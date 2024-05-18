using XeonPwm.Api.Services.Impl;

namespace XeonPwm.Api.Tests.Services.Impl;

public class LmSensorsReaderTests
{
    [Test]
    [TestCase("{\"coretemp-isa-0001\":{\"Adapter\":\"ISA adapter\",\"Core 0\":{\"temp2_input\":34.000,\"temp2_max\":80.000,\"temp2_crit\":96.000,\"temp2_crit_alarm\":0.000},\"Core 8\":{\"temp3_input\":32.000,\"temp3_max\":80.000,\"temp3_crit\":96.000,\"temp3_crit_alarm\":0.000},\"Core 2\":{\"temp4_input\":25.000,\"temp4_max\":80.000,\"temp4_crit\":96.000,\"temp4_crit_alarm\":0.000},\"Core 10\":{\"temp5_input\":27.000,\"temp5_max\":80.000,\"temp5_crit\":96.000,\"temp5_crit_alarm\":0.000},\"Core 1\":{\"temp6_input\":28.000,\"temp6_max\":80.000,\"temp6_crit\":96.000,\"temp6_crit_alarm\":0.000},\"Core 9\":{\"temp7_input\":30.000,\"temp7_max\":80.000,\"temp7_crit\":96.000,\"temp7_crit_alarm\":0.000}}}")]
    public void ParseLmSensorsOutputTest(string lmSensorsOutput)
    {
        _ = LmSensorsReader.ParseLmSensorsOutput(lmSensorsOutput);
    }
}