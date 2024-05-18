using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using XeonPwm.Api.Converters;
using XeonPwm.Api.Hubs;
using XeonPwm.Api.Models;

namespace XeonPwm.Api.Services.Impl;

public class LmSensorsReader : ITemperatureReader
{
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        Converters =
        {
            new CoreDataConverter()
        }
    };

    private static readonly ProcessStartInfo StartInfo = new ProcessStartInfo(
        "/usr/bin/sensors", "coretemp-isa-0001 -j")
    {
        RedirectStandardOutput = true
    };
    
    private readonly ILogger<LmSensorsReader> _logger;
    private readonly IHubContext<PwmHub> _hubContext;
    private readonly IConfiguration _configuration;
    private int _lastReadTemperature = 0;
    private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

    public LmSensorsReader(
        ILogger<LmSensorsReader> logger, 
        IHubContext<PwmHub> hubContext, 
        IConfiguration configuration)
    {
        _logger = logger;
        _hubContext = hubContext;
        _configuration = configuration;
    }

    public void StartReading() => Task.Factory.StartNew(ReadLoop, TaskCreationOptions.LongRunning);
    public async Task CancelReadingAsync() => await _tokenSource.CancelAsync();

    public event EventHandler<TemperatureChangedEventArgs>? OnTemperatureChanged;

    public int GetCurrentTemperature()
    {
        return _lastReadTemperature;
    }

    private async Task ReadLoop()
    {
        while (!_tokenSource.IsCancellationRequested)
        {
            var path = _configuration.GetRequiredSection("Sensors").GetValue<string>("OutputLogPath");
            if (string.IsNullOrEmpty(path))
            {
                throw new Exception("Sensors:OutputLogPath was null");
            }
            if (!File.Exists(path))
            {
                throw new Exception($"File {path} does not exist");
            }
            
            var output = await File.ReadAllTextAsync(path);
            var temps = ParseLmSensorsOutput(output);
            var currentTemperatureAvg = (int)temps.Average();

            if (currentTemperatureAvg != _lastReadTemperature)
            {
                OnTemperatureChanged?.Invoke(this, new TemperatureChangedEventArgs()
                {
                    PreviousTemperature = _lastReadTemperature,
                    NewTemperature = currentTemperatureAvg
                });

                _lastReadTemperature = currentTemperatureAvg;

                await _hubContext.Clients.All.SendAsync(
                    nameof(IPwmClient.OnTemperatureChanged), _lastReadTemperature);
            }

            await Task.Delay(5000);
        }
    }

    internal static IEnumerable<int> ParseLmSensorsOutput(string jsonOutput)
    {
        var output = JsonSerializer.Deserialize<LmSensorsOutput>(jsonOutput, SerializerOptions);
        return [output.ChipData.Core0.Current, output.ChipData.Core1.Current, output.ChipData.Core2.Current,
                output.ChipData.Core8.Current, output.ChipData.Core9.Current, output.ChipData.Core10.Current];
    }
}