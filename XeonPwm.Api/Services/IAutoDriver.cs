using XeonPwm.Api.Models;

namespace XeonPwm.Api.Services;

/// <summary>
/// Responsible for setting PWM duty cycle based on saved configuration
/// </summary>
public interface IAutoDriver
{
    bool Enable { get; set; }
    
    Task<IEnumerable<AutoDriverPoint>> GetCurrentConfigurationAsync();
    Task SaveConfigurationAsync(IEnumerable<AutoDriverPoint> points);

    Task<int> GetPwmPercentageAsync(int temperature);
}