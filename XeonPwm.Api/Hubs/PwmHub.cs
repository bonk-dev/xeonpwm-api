using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using XeonPwm.Api.Models;
using XeonPwm.Api.Services;

namespace XeonPwm.Api.Hubs;

[Authorize]
public class PwmHub : Hub<IPwmClient>
{
    private readonly ILogger<PwmHub> _logger;
    private readonly IPwmDriver _driver;
    private readonly ITemperatureReader _temperatureReader;
    private readonly IAutoDriver _autoDriver;

    public PwmHub(ILogger<PwmHub> logger, IPwmDriver driver, ITemperatureReader temperatureReader, IAutoDriver autoDriver)
    {
        _logger = logger;
        _driver = driver;
        _temperatureReader = temperatureReader;
        _autoDriver = autoDriver;
    }

    public override async Task OnConnectedAsync()
    {
        var dt = await _driver.Driver.GetDutyCycleAsync();
        await Clients.Caller.OnDutyCycleChanged(dt);
        await Clients.Caller.OnMaxDutyCycleChanged(_driver.Driver.LastReadSettingValues.MaxDutyCycle);
        await Clients.Caller.OnTemperatureChanged(_temperatureReader.GetCurrentTemperature());
        await Clients.Caller.OnAutoPointsChanged(await _autoDriver.GetCurrentConfigurationAsync());
        await Clients.Caller.OnAutoModeStatusChanged(_autoDriver.Enable);
        
        _logger.LogDebug("OnConnectedAsync end");
    }

    public async Task<int> GetDutyCycle()
    {
        return await _driver.Driver.GetDutyCycleAsync();
    }
    
    public async Task<string?> SetDutyCycle(int dutyCycle)
    {
        if (_autoDriver.Enable)
        {
            _logger.LogError("Cannot manually set duty cycle when auto mode is enabled");
            return "Cannot manually set duty cycle when auto mode is enabled";
        }
        
        _logger.LogDebug("Setting duty cycle to {DutyCycle}", dutyCycle);
        if (dutyCycle > _driver.Driver.LastReadSettingValues.MaxDutyCycle)
        {
            return $"Duty cycle must be smaller than {_driver.Driver.LastReadSettingValues.MaxDutyCycle}";
        }

        await _driver.Driver.SetDutyCycleAsync(dutyCycle);
        await Clients.Others.OnDutyCycleChanged(dutyCycle);
        return null;
    }

    public int GetCurrentTemperature() => _temperatureReader.GetCurrentTemperature();

    public async Task ChangeAutoModeStatus(bool enable)
    {
        _autoDriver.Enable = enable;
        await Clients.Others.OnAutoModeStatusChanged(enable);
    }
    
    public async Task SaveAutoConfiguration(IEnumerable<AutoDriverPoint> points)
    {
        var autoDriverPoints = points as AutoDriverPoint[] ?? points.ToArray();
        
        await _autoDriver.SaveConfigurationAsync(autoDriverPoints);
        await Clients.Others.OnAutoPointsChanged(autoDriverPoints);
    }
}