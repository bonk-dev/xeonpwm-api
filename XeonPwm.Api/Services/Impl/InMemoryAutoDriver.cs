using XeonPwm.Api.Models;

namespace XeonPwm.Api.Services.Impl;

public class InMemoryAutoDriver : IAutoDriver
{
    private readonly IPwmDriver _driver;
    private readonly ITemperatureReader _temperatureReader;
    private readonly ILogger<InMemoryAutoDriver> _logger;
    private readonly List<AutoDriverPoint> _points = new([
        new AutoDriverPoint(15, 10),
        new AutoDriverPoint(50, 20),
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

    public InMemoryAutoDriver(IPwmDriver driver, ITemperatureReader temperatureReader, ILogger<InMemoryAutoDriver> logger)
    {
        _driver = driver;
        _temperatureReader = temperatureReader;
        _logger = logger;
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
        Task.FromResult(_points.AsEnumerable());

    public async Task SaveConfigurationAsync(IEnumerable<AutoDriverPoint> points)
    {
        _points.Clear();
        _points.AddRange(points.Where(p => p.Temperature is > 0 and < 100));

        if (!Enable) return;
        
        await UpdateDutyCycleAsync(_temperatureReader.GetCurrentTemperature());
    }

    public Task<int> GetPwmPercentageAsync(int temperature)
    {
        for (var i = 0; i < _points.Count; ++i)
        {
            var currentPoint = _points[i];
            var nextPoint = i + 1 < _points.Count
                ? _points[i + 1]
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