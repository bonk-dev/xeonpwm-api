using System.IO.Ports;
using XeonPwm.Driver;

namespace XeonPwm.Api.Services.Impl;

public class PwmDriver : IPwmDriver, IDisposable
{
    private const int DefaultMaxDutyCycle = 255;
    
    private readonly SerialPort _port;
    private readonly ILogger<PwmDriver> _logger;
    
    public ControllerDriver Driver { get; }

    public PwmDriver(ILogger<PwmDriver> logger, IConfiguration configuration)
    {
        _logger = logger;

        var serial = configuration["Driver:SerialPort"];
        if (string.IsNullOrEmpty(serial))
        {
            throw new Exception("Driver:SerialPort is null");
        }
        
        var baudRateStr = configuration["Driver:BaudRate"];
        if (string.IsNullOrEmpty(serial))
        {
            throw new Exception("Driver:BaudRate is null");
        }

        if (!int.TryParse(baudRateStr, out var baudRate))
        {
            throw new Exception("Driver:BaudRate must be a valid integer");
        }
        
        _port = new SerialPort(serial, baudRate)
        {
            DtrEnable = true
        };
        
        _port.Open();
        _port.DiscardInBuffer();
        _port.DiscardOutBuffer();
        
        _logger.LogDebug("Opened the {SerialPortName} serial port", serial);
        
        Driver = new ControllerDriver(_port.BaseStream, logger: new DriverLogger(_logger));
    }

    public void Dispose()
    {
        _logger.LogDebug("Disposing the PWM driver");
        _port.Close();
        Driver.Dispose();
    }

    private class DriverLogger : IDriverLogger
    {
        private readonly ILogger _logger;

        public DriverLogger(ILogger logger)
        {
            _logger = logger;
        }
        
        public void LogDebug(string message)
        {
            _logger.LogDebug(message);
        }
    }
}