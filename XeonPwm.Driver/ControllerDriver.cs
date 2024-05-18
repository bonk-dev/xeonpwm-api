using System.Text;
using XeonPwm.Driver.Commands;
using XeonPwm.Driver.Settings;

namespace XeonPwm.Driver;

public class ControllerDriver : IDisposable, IAsyncDisposable
{
    private readonly IDriverLogger? _logger;

    // Max ESP32 PWM resolution is 16-bit
    // Actual max can be different since resolution can be changed with SetPwmSetting
    public static readonly int AbsoluteMaxDutyCycle = (int)(Math.Pow(2, 16) - 1);

    // Arduino Serial.println uses LF+CR
    public const string ControllerNewLine = "\r\n";
    private const int DefaultBufferSize = 64;
    
    // All return codes are 1-character long
    internal static int ReturnCodeLength { get; } = Encoding.ASCII.GetByteCount("0" + ControllerNewLine);
    private static readonly Memory<byte> NewLineBytes = Encoding.ASCII.GetBytes(ControllerNewLine);

    private readonly SettingsParser _settingsParser = new();
    private readonly CommandSerializer _commandSerializer = new();
    private readonly Memory<byte> _writeBuffer = new byte[DefaultBufferSize];
    private readonly Memory<byte> _readBuffer = new byte[DefaultBufferSize];
    private readonly SemaphoreSlim _semaphore = new(1);

    internal Stream Stream { get; }
    public int TimeoutMs { get; }
    public SettingValues LastReadSettingValues { get; private set; } = SettingValues.Default;

    public ControllerDriver(Stream stream, int timeoutMs = 5000, IDriverLogger? logger = null)
    {
        _logger = logger;
        Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        TimeoutMs = timeoutMs;

        if (!stream.CanRead)
        {
            throw new ArgumentException("Stream must be readable", nameof(stream));
        }
        if (!stream.CanWrite)
        {
            throw new ArgumentException("Stream must be writeable", nameof(stream));
        }
    }

    /// <summary>
    /// Soft-restart the microcontroller
    /// </summary>
    public async Task RestartAsync() => await SerializeAndWriteAsync(CommandNames.Restart);

    /// <summary>
    /// Sets the PWM duty cycle.
    /// <para>The higher the duty cycle, the lower FAN RPM</para>
    /// </summary>
    /// <param name="dutyCycle">PWM duty cycle (min. 0, max. <see cref="AbsoluteMaxDutyCycle"/>)</param>
    /// <exception cref="ArgumentException"></exception>
    public async Task SetDutyCycleAsync(int dutyCycle)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(dutyCycle);

        if (dutyCycle > LastReadSettingValues.MaxDutyCycle)
        {
            throw new ArgumentOutOfRangeException(nameof(dutyCycle),
                $"The duty cycle must be smaller than {LastReadSettingValues.MaxDutyCycle}");
        }

        await SerializeAndWriteAsync(CommandNames.SetDutyCycle, dutyCycle.ToString());
    }

    /// <summary>
    /// Reads the current PWM duty cycle.
    /// </summary>
    /// <returns>An integer representing current PWM duty cycle (min. 0, max. <see cref="AbsoluteMaxDutyCycle"/>)</returns>
    public async Task<int> GetDutyCycleAsync()
    {
        var cycleString = await SerializeAndWriteReadStringAsync(CommandNames.GetDutyCycle);
        return int.Parse(cycleString);
    }

    /// <summary>
    /// Set one of the four PWM settings
    /// </summary>
    /// <param name="setting">Setting type</param>
    /// <param name="value">New setting value</param>
    public async Task SetPwmSettingAsync(EPwmSetting setting, int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);

        await SerializeAndWriteAsync(
            CommandNames.SetPwmSetting,
            setting.AsString(),
            value.ToString());
    }

    /// <summary>
    /// Reads values of all of the PWM settings
    /// You should probably use <see cref="LastReadSettingValues"/> instead.
    /// </summary>
    /// <returns>An instance of <see cref="SettingValues"/></returns>
    public async Task<SettingValues> GetPwmSettingsAsync()
    {
        var str = await SerializeAndWriteReadStringAsync(CommandNames.ShowPwmSettings);
        var settings = _settingsParser.ParseFromString(str);
        LastReadSettingValues = settings;

        return settings;
    }

    /// <summary>
    /// Resets all the PWM settings to their default values 
    /// </summary>
    public async Task ResetPwmSettingsAsync() => await SerializeAndWriteAsync(CommandNames.ResetPwmSetting);

    public void Dispose() => Stream.Dispose();
    public async ValueTask DisposeAsync() => await Stream.DisposeAsync();

    private async Task SerializeAndWriteAsync(string commandName, params string[] args) =>
        await SerializeAndWriteAsync(commandName, CancellationToken.None, args)
            .WaitAsync(TimeSpan.FromMilliseconds(TimeoutMs));
    
    private async Task SerializeAndWriteAsync(string commandName, CancellationToken token, params string[] args)
    {
        try
        {
            await _semaphore.WaitAsync(token);

            var length = args.Length <= 0
                ? _commandSerializer.Serialize(_writeBuffer.Span, commandName, ReadOnlySpan<string>.Empty)
                : _commandSerializer.Serialize(_writeBuffer.Span, commandName, args.AsSpan());

            await Stream.WriteAsync(_writeBuffer[..length], token);
            await DiscardReturnCode(token);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<string> SerializeAndWriteReadStringAsync(string commandName, params string[] args) =>
        await SerializeAndWriteReadStringAsync(commandName, CancellationToken.None, args)
            .WaitAsync(TimeSpan.FromMilliseconds(TimeoutMs));
    
    private async Task<string> SerializeAndWriteReadStringAsync(string commandName, CancellationToken token, params string[] args)
    {
        var length = args.Length <= 0
            ? _commandSerializer.Serialize(_writeBuffer.Span, commandName, ReadOnlySpan<string>.Empty)
            : _commandSerializer.Serialize(_writeBuffer.Span, commandName, args.AsSpan());

        await Stream.WriteAsync(_writeBuffer[..length], token);
        
        // value<new line>return code<new line>
        var readLength = await Stream.ReadAsync(_readBuffer, token);
        var newLineIndex = _readBuffer.Span[..readLength].IndexOf(NewLineBytes.Span);
        
        _logger?.LogDebug("Buffer " + DebugBuffer(_readBuffer.Span));
        _logger?.LogDebug("New line index: " + newLineIndex);
        
        return Encoding.ASCII.GetString(_readBuffer[..newLineIndex].Span);
    }

    private static string DebugBuffer(ReadOnlySpan<byte> buffer)
    {
        var builder = new StringBuilder();
        
        foreach (var b in buffer)
        {
            builder.Append("0x");
            builder.Append(b.ToString("X2"));
            builder.Append(' ');
        }

        return builder.ToString();
    } 
    
    private async Task DiscardReturnCode(CancellationToken token = default) => 
        _ = await Stream.ReadAsync(_readBuffer[..(ReturnCodeLength * 2)], token);
}