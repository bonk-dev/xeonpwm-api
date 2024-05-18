namespace XeonPwm.Driver.Settings;

public static class EPwmSettingExtensions
{
    public static string AsString(this EPwmSetting setting) =>
        setting switch
        {
            EPwmSetting.Channel => "CHANNEL",
            EPwmSetting.Frequency => "FREQUENCY",
            EPwmSetting.Pin => "PIN",
            EPwmSetting.Resolution => "RESOLUTION",
            _ => throw new Exception("Invalid enum value")
        };
}