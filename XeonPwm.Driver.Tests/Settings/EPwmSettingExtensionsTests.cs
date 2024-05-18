using XeonPwm.Driver.Settings;

namespace XeonPwm.Driver.Tests.Settings;

public class EPwmSettingExtensionsTests
{
    [Test]
    [TestCase(EPwmSetting.Frequency, "FREQUENCY")]
    [TestCase(EPwmSetting.Channel, "CHANNEL")]
    [TestCase(EPwmSetting.Resolution, "RESOLUTION")]
    [TestCase(EPwmSetting.Pin, "PIN")]
    public void AsStringTest(EPwmSetting setting, string expected)
    {
        var str = setting.AsString();
        Assert.That(str, Is.EqualTo(expected));
    }

    [Test]
    public void AsStringInvalidTest()
    {
        const EPwmSetting invalidSetting = (EPwmSetting)423423235;
        Assert.That(Enum.IsDefined(invalidSetting), Is.False);
        Assert.Catch(() =>
        {
            _ = invalidSetting.AsString();
        });
    }
}