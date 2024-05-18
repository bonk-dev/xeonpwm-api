namespace XeonPwm.Api.Models;

public class TemperatureChangedEventArgs : EventArgs
{
    public required int PreviousTemperature { get; set; }
    public required int NewTemperature { get; set; }
}