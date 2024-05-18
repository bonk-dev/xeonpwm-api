using System.Text;
using XeonPwm.Driver.Commands;

namespace XeonPwm.Driver.Tests.Commands;

public class CommandSerializerTests
{
    [Test]
    [TestCase("RESTART", "RESTART|")]
    [TestCase("GET_DT_CYCLE", "GET_DT_CYCLE|")]
    [TestCase("SHOW_PWM_SETTINGS", "SHOW_PWM_SETTINGS|")]
    [TestCase("RESET_PWM_SETTINGS", "RESET_PWM_SETTINGS|")]
    public void SerializeNoArgsTest(string commandName, string expected)
    {
        var serializer = new CommandSerializer();

        Span<byte> buffer = stackalloc byte[64];
        var length = serializer.Serialize(buffer, commandName);

        var actualString = Encoding.ASCII.GetString(buffer[..length]);
        Assert.That(actualString, Is.EqualTo(expected));
    }

    [Test]
    [TestCase("SET_DT_CYCLE", new[] { "255" }, "SET_DT_CYCLE|255|")]
    [TestCase("SET_PWM_SETTING", new [] { "FREQUENCY", "25000" }, "SET_PWM_SETTING|FREQUENCY|25000|")]
    public void SerializeArgsParamsTest(string commandName, string[] args, string expected)
    {
        var serializer = new CommandSerializer();

        Span<byte> buffer = stackalloc byte[64];
        var length = serializer.Serialize(buffer, commandName, args);
        
        var actualString = Encoding.ASCII.GetString(buffer[..length]);
        Assert.That(actualString, Is.EqualTo(expected));
    }
    
    [Test]
    [TestCase("SET_DT_CYCLE", new[] { "255" }, "SET_DT_CYCLE|255|")]
    [TestCase("SET_PWM_SETTING", new [] { "FREQUENCY", "25000" }, "SET_PWM_SETTING|FREQUENCY|25000|")]
    public void SerializeArgsSpanTest(string commandName, string[] args, string expected)
    {
        var serializer = new CommandSerializer();

        Span<byte> buffer = stackalloc byte[64];
        var length = serializer.Serialize(buffer, commandName, args.AsSpan());
        
        var actualString = Encoding.ASCII.GetString(buffer[..length]);
        Assert.That(actualString, Is.EqualTo(expected));
    }
}