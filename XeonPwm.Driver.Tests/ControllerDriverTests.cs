using System.Buffers.Binary;
using System.Text;
using XeonPwm.Driver.Commands;
using XeonPwm.Driver.Settings;
using XeonPwm.Driver.Tests.Streams;

namespace XeonPwm.Driver.Tests;

public class ControllerDriverTests
{
    #region Constructor

    [Test]
    public void ConstructorNullStreamExceptionTest()
    {
        Assert.Catch<ArgumentNullException>(() =>
        {
            _ = new ControllerDriver(null!);
        });
    }
    
    [Test]
    public void ConstructorNonReadableStreamExceptionTest()
    {
        Assert.Catch<ArgumentException>(() =>
        {
            var stream = new InvalidStream(false, false);
            _ = new ControllerDriver(stream);
        });
    }
    
    [Test]
    public void ConstructorNonWriteableStreamExceptionTest()
    {
        Assert.Catch<ArgumentException>(() =>
        {
            var stream = new InvalidStream(true, false);
            _ = new ControllerDriver(stream);
        });
    }
    
    #endregion

    [Test]
    public void RestartAsyncTest()
    {
        var driver = PrepDriver();
        var task = new Task(() =>
        {
            driver.RestartAsync().Wait();
        });
        
        var expectedString = CommandNames.Restart + "|";
        AssertSentCommand(driver, expectedString, task);
    }
    
    [Test]
    [TestCase(240)]
    public void SetDutyCycleAsyncTest(int dutyCycle)
    {
        var driver = PrepDriver();
        var task = new Task(() => driver.SetDutyCycleAsync(dutyCycle).Wait());
        var expectedString = $"{CommandNames.SetDutyCycle}|{dutyCycle}|";
        AssertSentCommand(driver, expectedString, task);
    }
    
    [Test]
    [TestCase(240)]
    public void GetDutyCycleAsyncTest(int simulatedDutyCycle)
    {
        var driver = PrepDriver(Encoding.ASCII.GetBytes(simulatedDutyCycle + ControllerDriver.ControllerNewLine));
        var task = new Task<int>(() => driver.GetDutyCycleAsync().Result);
        var expectedString = CommandNames.GetDutyCycle + "|";
        AssertSentCommandWithGet(driver, expectedString, task, simulatedDutyCycle.ToString());

        var actualResult = task.Result;
        Assert.That(actualResult, Is.EqualTo(simulatedDutyCycle));
    }
    
    [Test]
    [TestCase(EPwmSetting.Frequency, 25000)]
    [TestCase(EPwmSetting.Channel, 0)]
    [TestCase(EPwmSetting.Pin, 4)]
    [TestCase(EPwmSetting.Resolution, 8)]
    public void SetPwmSettingAsyncTest(EPwmSetting setting, int value)
    {
        var driver = PrepDriver();
        var task = new Task(() => driver.SetPwmSettingAsync(setting, value).Wait());
        var expectedString = $"{CommandNames.SetPwmSetting}|{setting.AsString()}|{value}|";
        AssertSentCommand(driver, expectedString, task);
    }
    
    [Test]
    public void GetPwmSettingsAsyncTest()
    {
        var driver = PrepDriver(Encoding.ASCII.GetBytes($"0|25000|8|4|255{ControllerDriver.ControllerNewLine}"));
        var task = new Task<SettingValues>(() => driver.GetPwmSettingsAsync().Result);
        var expectedString = CommandNames.ShowPwmSettings + "|";
        AssertSentCommandWithGet(driver, expectedString, task, "0|25000|8|4|255");
    }
    
    [Test]
    public void ResetPwmSettingsAsyncTest()
    {
        var driver = PrepDriver();
        var task = new Task(() => driver.ResetPwmSettingsAsync().Wait());
        var expectedString = CommandNames.ResetPwmSetting + "|";
        AssertSentCommand(driver, expectedString, task);
    }

    private static ControllerDriver PrepDriver(byte[]? fakeResponse = null)
    {
        Stream stream = fakeResponse != null 
            ? new FakeReadStream(fakeResponse) 
            : new MemoryStream(new byte[64]);
        
        return new ControllerDriver(stream);
    }

    private static void AssertSentCommand(ControllerDriver driver, string expectedString, Task task)
    {
        task.RunSynchronously();
        task.Wait();
        
        var stream = (MemoryStream)driver.Stream;
        var actualString = Encoding.ASCII.GetString(
            stream
                .ToArray()
                .AsSpan(0, (int)stream.Position - ControllerDriver.ReturnCodeLength));
        
        Assert.That(actualString, Is.EqualTo(expectedString));
    }
    
    private static void AssertSentCommandWithGet(ControllerDriver driver, string expectedString, Task task, string response)
    {
        var stream = (MemoryStream)driver.Stream;
        driver.Stream.Write(Encoding.ASCII.GetBytes(response + ControllerDriver.ControllerNewLine));
        stream.Position = 0L;

        task.RunSynchronously();
        task.Wait();
        
        var actualString = Encoding.ASCII.GetString(stream.ToArray().AsSpan(0, (int)stream.Position));
        
        Assert.That(actualString, Is.EqualTo(expectedString));
        
        driver.Stream.Write(Encoding.ASCII.GetBytes("0" + ControllerDriver.ControllerNewLine));
    }
}