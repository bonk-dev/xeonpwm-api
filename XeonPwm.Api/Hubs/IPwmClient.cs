using XeonPwm.Api.Models;

namespace XeonPwm.Api.Hubs;

public interface IPwmClient
{
    Task OnDutyCycleChanged(int dutyCycle);
    Task OnMaxDutyCycleChanged(int maxDutyCycle);
    Task OnTemperatureChanged(int temperature);
    Task OnAutoPointsChanged(IEnumerable<AutoDriverPoint> points);
    Task OnAutoModeStatusChanged(bool enabled);
}