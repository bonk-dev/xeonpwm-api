namespace XeonPwm.Driver.Settings;

public record SettingValues
{
    public required int Frequency { get; set; }
    public required int Resolution { get; set; }
    public required int Channel { get; set; }
    public required int Pin { get; set; }
    public required int MaxDutyCycle { get; set; }

    public static SettingValues Default => new SettingValues()
    {
        Frequency = 25000,
        Resolution = 8,
        Channel = 0,
        Pin = 4,
        MaxDutyCycle = 255
    };
}