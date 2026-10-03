using System.IO.Ports;
using System.Text;
using ZDLISPlus.Web.Models;

namespace ZDLISPlus.Web.Services.Instruments.Drivers;

public class SerialDriver : IInstrumentDriver
{
    private readonly Instrument _instrument;
    private readonly IServiceProvider _sp;
    private SerialPort? _port;
    private readonly StringBuilder _buffer = new();

    public SerialDriver(Instrument instrument, IServiceProvider sp)
    {
        _instrument = instrument;
        _sp = sp;
    }

    public int InstrumentId => _instrument.Id;
    public bool IsRunning => _port?.IsOpen == true;

    public event Action<string, string>? Log;
    public event Action<string>? RawReceived;

    public Task StartAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_instrument.SerialPort))
            throw new InvalidOperationException("Serial port not configured.");

        _port = new SerialPort(
            _instrument.SerialPort,
            _instrument.SerialBaud ?? 9600,
            ParseParity(_instrument.SerialParity),
            _instrument.SerialDataBits ?? 8,
            ParseStopBits(_instrument.SerialStopBits))
        {
            ReadTimeout = 1000,
            WriteTimeout = 1000,
            Handshake = Handshake.None
        };

        _port.DataReceived += OnData;
        _port.Open();
        Log?.Invoke("Info", $"Opened {_instrument.SerialPort} @ {_instrument.SerialBaud}");
        return Task.CompletedTask;
    }

    private void OnData(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            var available = _port!.BytesToRead;
            if (available <= 0) return;
            var buf = new byte[available];
            var read = _port.Read(buf, 0, available);
            if (read <= 0) return;

            _buffer.Append(Encoding.ASCII.GetString(buf, 0, read));
            var text = _buffer.ToString();
            if (text.EndsWith("\x1C\r") || text.EndsWith("\r\r") || text.EndsWith("\x04"))
            {
                _buffer.Clear();
                RawReceived?.Invoke(text.TrimEnd('\0'));
            }
        }
        catch (Exception ex)
        {
            Log?.Invoke("Error", ex.Message);
        }
    }

    public Task StopAsync()
    {
        try { _port?.Close(); } catch { }
        return Task.CompletedTask;
    }

    private static Parity ParseParity(string? s) => s?.ToLowerInvariant() switch
    {
        "even" => Parity.Even,
        "odd" => Parity.Odd,
        "mark" => Parity.Mark,
        "space" => Parity.Space,
        _ => Parity.None
    };

    private static StopBits ParseStopBits(string? s) => s?.ToLowerInvariant() switch
    {
        "two" => StopBits.Two,
        "onepointfive" => StopBits.OnePointFive,
        _ => StopBits.One
    };

    public ValueTask DisposeAsync()
    {
        try { _port?.Dispose(); } catch { }
        return ValueTask.CompletedTask;
    }
}