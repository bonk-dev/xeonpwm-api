using System.Text.Json.Serialization;

namespace XeonPwm.Api.Models;

public class LmSensorsOutput
{
    [JsonPropertyName("coretemp-isa-0001")]
    public required CoreTempChip ChipData { get; set; }
    
    public class CoreTempChip
    {
        public required string Adapter { get; set; }
        
        [JsonPropertyName("Core 0")]
        public required CoreTempData Core0 { get; set; }
        
        [JsonPropertyName("Core 8")]
        public required CoreTempData Core8 { get; set; }
        
        [JsonPropertyName("Core 2")]
        public required CoreTempData Core2 { get; set; }
        
        [JsonPropertyName("Core 10")]
        public required CoreTempData Core10 { get; set; }
        
        [JsonPropertyName("Core 1")]
        public required CoreTempData Core1 { get; set; }
        
        [JsonPropertyName("Core 9")]
        public required CoreTempData Core9 { get; set; }

        public class CoreTempData
        {
            public int Current { get; set; }
            public int Max { get; set; }
            public int Critical { get; set; }
            public int CriticalAlarm { get; set; }
        }
    }
}