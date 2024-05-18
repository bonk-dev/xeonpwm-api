using Microsoft.EntityFrameworkCore;
using XeonPwm.Api.Contexts;
using XeonPwm.Api.Models;
using XeonPwm.Api.Models.Db;

namespace XeonPwm.Api.Services.Impl;

public class EfCoreAutoDriver : IAutoDriver
{
    private readonly IPwmDriver _driver;
    private readonly ITemperatureReader _temperatureReader;
    private readonly ILogger<InMemoryAutoDriver> _logger;
    private readonly IDbContextFactory<XeonPwmContext> _contextFactory;

    private readonly List<AutoDriverPoint> _cachedPoints = new([

    ]);

    private bool _enable;
    private int _lastSetForTemperature = -1;

    public bool Enable
    {
        get => _enable;
        set
        {
            if (_enable != value && value)
            {
                _temperatureReader.OnTemperatureChanged += TemperatureReaderOnOnTemperatureChanged;
                if (_lastSetForTemperature != _temperatureReader.GetCurrentTemperature())
                {
                    _ = UpdateDutyCycleAsync(_temperatureReader.GetCurrentTemperature());
                }
            }
            else if (_enable != value)
            {
                _temperatureReader.OnTemperatureChanged -= TemperatureReaderOnOnTemperatureChanged;
            }
            _enable = value;
        }
    }

    public EfCoreAutoDriver(IPwmDriver driver, ITemperatureReader temperatureReader, ILogger<InMemoryAutoDriver> logger, 
        IDbContextFactory<XeonPwmContext> contextFactory)
    {
        _driver = driver;
        _temperatureReader = temperatureReader;
        _logger = logger;
        _contextFactory = contextFactory;
    }

    public async Task ReloadFromDatabaseAsync()
    {
        _cachedPoints.Clear();

        await using var context = await _contextFactory.CreateDbContextAsync();
        await foreach (var point in context.DriverPoints.AsAsyncEnumerable())
        {
            _cachedPoints.Add(new AutoDriverPoint(point.Temperature, point.PwmPercentage));
        }
    } 

    private async void TemperatureReaderOnOnTemperatureChanged(object? sender, TemperatureChangedEventArgs e) => 
        await UpdateDutyCycleAsync(e.NewTemperature);

    private async Task UpdateDutyCycleAsync(int temperature)
    {
        var pwmPerc = await GetPwmPercentageAsync(temperature);
        var settingsAuto = _driver.Driver.LastReadSettingValues;
        var newPwmDutyCycle = (int)((100 - pwmPerc) / 100.00 * settingsAuto.MaxDutyCycle);
        
        _logger.LogDebug("Perc: {PwmPerc}, temp: {Temperature}", pwmPerc, temperature);
        _logger.LogInformation("Auto driver setting PWM duty cycle to {DutyCycle}", newPwmDutyCycle);
        
        await _driver.Driver.SetDutyCycleAsync(newPwmDutyCycle);
    }

    public Task<IEnumerable<AutoDriverPoint>> GetCurrentConfigurationAsync() => 
        Task.FromResult(_cachedPoints.AsEnumerable());

    public async Task SaveConfigurationAsync(IEnumerable<AutoDriverPoint> points)
    {
        var autoDriverPoints = points as AutoDriverPoint[] ?? points.ToArray();
        
        _cachedPoints.Clear();
        _cachedPoints.AddRange(autoDriverPoints.Where(p => p.Temperature is > 0 and < 100));
        
        await using var context = await _contextFactory.CreateDbContextAsync();
        await context.DriverPoints.ExecuteDeleteAsync();
        await context.DriverPoints.AddRangeAsync(autoDriverPoints.Select(p => new RegisteredAutoDriverPoint()
        {
            PwmPercentage = p.PwmPercentage,
            Temperature = p.Temperature
        }));
        await context.SaveChangesAsync();

        if (!Enable) return;
        
        await UpdateDutyCycleAsync(_temperatureReader.GetCurrentTemperature());
    }

    public Task<int> GetPwmPercentageAsync(int temperature)
    {
        for (var i = 0; i < _cachedPoints.Count; ++i)
        {
            var currentPoint = _cachedPoints[i];
            var nextPoint = i + 1 < _cachedPoints.Count
                ? _cachedPoints[i + 1]
                : null;
            if (nextPoint == null || (i == 0 && currentPoint.Temperature > temperature))
            {
                return Task.FromResult(currentPoint.PwmPercentage);
            }

            if (currentPoint.Temperature < temperature && nextPoint.Temperature > temperature)
            {
                return Task.FromResult(GetPwmPercentageFromRange(currentPoint, nextPoint, temperature));
            }
        }

        return Task.FromResult(100);
    }

    private static int GetPwmPercentageFromRange(AutoDriverPoint startPoint, AutoDriverPoint endPoint, int inputTemperature)
    {
        if (startPoint.Temperature > endPoint.Temperature)
        {
            throw new ArgumentException("Start point must have a smaller temperature than the end point",
                nameof(startPoint));
        }

        // Linear function
        var a = (endPoint.PwmPercentage - startPoint.PwmPercentage) * 1.00 / (endPoint.Temperature - startPoint.Temperature) * 1.00;
        return (int)(a * (inputTemperature - startPoint.Temperature) + startPoint.PwmPercentage);
    }
}