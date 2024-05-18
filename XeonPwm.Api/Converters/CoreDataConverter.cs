using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using XeonPwm.Api.Models;

namespace XeonPwm.Api.Converters;

public class CoreDataConverter : JsonConverter<LmSensorsOutput.CoreTempChip.CoreTempData>
{
    private static Regex _propertyNameRegex = new Regex("^temp\\d+_(.*)", RegexOptions.Compiled);
    
    public override LmSensorsOutput.CoreTempChip.CoreTempData? Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var input = 0;
        var max = 0;
        var crit = 0;
        var critAlarm = 0;

        var propType = EPropertyType.None;
        while (reader.TokenType != JsonTokenType.EndObject)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    var match = _propertyNameRegex.Match(reader.GetString()!);
                    if (match.Success)
                    {
                        var propTypeStr = match.Groups[1].Value;
                        propType = propTypeStr switch
                        {
                            "input" => EPropertyType.Input,
                            "max" => EPropertyType.Max,
                            "crit" => EPropertyType.Critical,
                            "crit_alarm" => EPropertyType.CriticalAlarm,
                            _ => throw new Exception("Invalid property type: " + propTypeStr)
                        };
                    }
                    break;
                case JsonTokenType.Number:
                    switch (propType)
                    {
                        case EPropertyType.Input:
                            input = (int)reader.GetSingle();
                            break;
                        case EPropertyType.Max:
                            max = (int)reader.GetSingle();
                            break;
                        case EPropertyType.Critical:
                            crit = (int)reader.GetSingle();
                            break;
                        case EPropertyType.CriticalAlarm:
                            critAlarm = (int)reader.GetSingle();
                            break;
                    }
                    break;
            }
            
            reader.Read();
        }

        return new LmSensorsOutput.CoreTempChip.CoreTempData()
        {
            Current = input,
            Max = max,
            Critical = crit,
            CriticalAlarm = critAlarm
        };
    }

    public override void Write(
        Utf8JsonWriter writer, LmSensorsOutput.CoreTempChip.CoreTempData value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
    
    private enum EPropertyType
    {
        None,
        Input,
        Max,
        Critical,
        CriticalAlarm
    }
}