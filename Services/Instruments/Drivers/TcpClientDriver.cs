using System.Net.Sockets;
using System.Text;
using ZDLISPlus.Web.Models;

namespace ZDLISPlus.Web.Services.Instruments.Drivers;

public class TcpClientDriver : IInstrumentDriver
{
    private readonly Instrument _instrument;
    private readonly IServiceProvider _sp;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public TcpClientDriver(Instrument instrument, IServiceProvider sp)
    {
        _instrument = instrument;
        _sp = sp;
    }

    public int InstrumentId => _instrument.Id;
    public bool IsRunning => _loop is { IsCompleted: false };

    public event Action<string, string>? Log;
    public event Action<string>? RawReceived;

    public Task StartAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_instrument.TcpHost) || _instrument.TcpPort is null)
            throw new InvalidOperationException("TCP host/port not configured.");

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _loop = Task.Run(() => LoopAsync(_cts.Token), _cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        if (_loop is not null) await Task.WhenAll(_loop).ConfigureAwait(false);
    }

    private async Task LoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(_instrument.TcpHost!, _instrument.TcpPort!.Value, ct);
                Log?.Invoke("Info", $"Connected to {_instrument.TcpHost}:{_instrument.TcpPort}");

                using var stream = client.GetStream();
                var buffer = new byte[8192];
                var sb = new StringBuilder();

                while (!ct.IsCancellationRequested)
                {
                    var read = await stream.ReadAsync(buffer, ct);
                    if (read == 0) break;

                    sb.Append(Encoding.ASCII.GetString(buffer, 0, read));
                    var text = sb.ToString();

                    if (text.EndsWith("\x1C\r") ||
                        text.EndsWith("\r\r") ||
                        text.EndsWith("\x04"))
                    {
                        RawReceived?.Invoke(text.TrimEnd('\0'));
                        sb.Clear();
                    }
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Log?.Invoke("Warn", "Connection lost: " + ex.Message);
                try { await Task.Delay(5000, ct); } catch { break; }
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        _cts?.Dispose();
        return ValueTask.CompletedTask;
    }
}