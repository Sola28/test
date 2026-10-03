using System.Net;
using System.Net.Sockets;
using System.Text;
using ZDLISPlus.Web.Models;

namespace ZDLISPlus.Web.Services.Instruments.Drivers;

public class TcpServerDriver : IInstrumentDriver
{
    private readonly Instrument _instrument;
    private readonly IServiceProvider _sp;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public TcpServerDriver(Instrument instrument, IServiceProvider sp)
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
        if (_instrument.TcpPort is null)
            throw new InvalidOperationException("TCP port not configured.");

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _listener = new TcpListener(IPAddress.Any, _instrument.TcpPort.Value);
        _listener.Start();
        _loop = Task.Run(() => AcceptLoopAsync(_cts.Token), _cts.Token);
        Log?.Invoke("Info", $"Listening on port {_instrument.TcpPort}");
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        try { _listener?.Stop(); } catch { }
        if (_loop is not null) await Task.WhenAll(_loop).ConfigureAwait(false);
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var client = await _listener!.AcceptTcpClientAsync(ct);
                _ = HandleClientAsync(client, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { Log?.Invoke("Error", ex.Message); }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        try
        {
            using (client)
            using (var stream = client.GetStream())
            {
                var buffer = new byte[8192];
                var sb = new StringBuilder();
                var idle = DateTime.UtcNow;

                while (!ct.IsCancellationRequested)
                {
                    var read = await stream.ReadAsync(buffer, ct);
                    if (read == 0) break;

                    sb.Append(Encoding.ASCII.GetString(buffer, 0, read));
                    var text = sb.ToString();

                    // Message boundary heuristics for HL7 / ASTM
                    if (text.EndsWith("\x1C\r") ||
                        text.EndsWith("\r\r") ||
                        text.EndsWith("\x04") ||
                        (DateTime.UtcNow - idle).TotalMilliseconds > 800)
                    {
                        var cleaned = text.TrimEnd('\0');
                        if (!string.IsNullOrWhiteSpace(cleaned))
                            RawReceived?.Invoke(cleaned);
                        sb.Clear();
                    }
                    idle = DateTime.UtcNow;
                }
            }
        }
        catch (Exception ex)
        {
            Log?.Invoke("Error", ex.Message);
        }
    }

    public ValueTask DisposeAsync()
    {
        _cts?.Dispose();
        try { _listener?.Stop(); } catch { }
        return ValueTask.CompletedTask;
    }
}