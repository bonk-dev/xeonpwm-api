using XeonPwm.Api.Models;

namespace XeonPwm.Api.Services;

/// <summary>
/// Responsible for reading the current CPU temperature
/// </summary>
public interface ITemperatureReader
{
    event EventHandler<TemperatureChangedEventArgs> OnTemperatureChanged;
    int GetCurrentTemperature();
}