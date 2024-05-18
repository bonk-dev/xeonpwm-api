using XeonPwm.Driver.Settings;

namespace XeonPwm.Driver.Tests.Settings;

public class SettingsParserTests
{
    public static IEnumerable<TestCaseData> FromStringCases
    {
        get
        {
            var strToBeParsed = "0|25000|8|4|255";
            var expectedValues = new SettingValues()
            {
                Channel = 0,
                Frequency = 25000,
                Resolution = 8,
                Pin = 4,
                MaxDutyCycle = 255
            };

            yield return new TestCaseData(strToBeParsed, expectedValues);
        }
    }
    
    [Test]
    [TestCaseSource(nameof(FromStringCases))]
    public void ParseFromStringTest(string value, SettingValues expectedValues)
    {
        var parser = new SettingsParser();
        var actualValues = parser.ParseFromString(value);
        
        Assert.That(actualValues, Is.EqualTo(expectedValues));
    }

    [Test]
    [TestCase("0|123|123|123")]
    [TestCase("")]
    public void ParseFromStringInvalidValueAmountTest(string value)
    {
        var splitLength = value.Split(value, StringSplitOptions.RemoveEmptyEntries).Length;
        Assert.That(splitLength, Is.Not.EqualTo(SettingsParser.ExpectedStringSplitLength));
        
        Assert.Catch<ArgumentException>(() =>
        {
            var parser = new SettingsParser();
            parser.ParseFromString(value);
        });
    }
}