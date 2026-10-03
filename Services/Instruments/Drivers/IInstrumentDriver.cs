namespace ZDLISPlus.Web.Services.Instruments.Drivers;

public interface IInstrumentDriver : IAsyncDisposable
{
    int InstrumentId { get; }
    Task StartAsync(CancellationToken ct);
    Task StopAsync();
    bool IsRunning { get; }
    event Action<string, string>? Log;   // (level, message)
    event Action<string>? RawReceived;   // full message text
}