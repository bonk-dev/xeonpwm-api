using XeonPwm.Driver;

namespace XeonPwm.Api.Services;

public interface IPwmDriver
{
    ControllerDriver Driver { get; }
}