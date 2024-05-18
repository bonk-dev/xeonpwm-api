namespace XeonPwm.Driver.Settings;

public class SettingsParser
{
    internal const int ExpectedStringSplitLength = 5; 
    
    public SettingValues ParseFromString(string value)
    {
        var split = value.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (split.Length != ExpectedStringSplitLength)
        {
            throw new ArgumentException(
                $"The string does not have the expected amount of values ({ExpectedStringSplitLength})");
        }
        
        var channel = int.Parse(split[0]);
        var frequency = int.Parse(split[1]);
        var resolution = int.Parse(split[2]);
        var pin = int.Parse(split[3]);
        var maxDutyCycle = int.Parse(split[4]);

        return new SettingValues()
        {
            Channel = channel,
            Frequency = frequency,
            Resolution = resolution,
            Pin = pin,
            MaxDutyCycle = maxDutyCycle
        };
    }
}