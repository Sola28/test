using Microsoft.EntityFrameworkCore;
using ZDLISPlus.Web.Data;
using ZDLISPlus.Web.Models;
using ZDLISPlus.Web.Services.Instruments.Drivers;
using ZDLISPlus.Web.Services.Instruments.Parsing;

namespace ZDLISPlus.Web.Services.Instruments;

public class InstrumentHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly DriverFactory _drivers;
    private readonly ProtocolParserFactory _parsers;
    private readonly ILogger<InstrumentHostedService> _log;

    private readonly Dictionary<int, IInstrumentDriver> _running = new();

    public InstrumentHostedService(
        IServiceScopeFactory scopes,
        DriverFactory drivers,
        ProtocolParserFactory parsers,
        ILogger<InstrumentHostedService> log)
    {
        _scopes = scopes;
        _drivers = drivers;
        _parsers = parsers;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Wait a beat for the app to fully start
        await Task.Delay(1500, ct);

        using (var scope = _scopes.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LabDbContext>();
            var instruments = await db.Instruments
                .Where(i => i.IsEnabled && i.AutoStart)
                .ToListAsync(ct);

            foreach (var inst in instruments)
                TryStart(inst);
        }

        // Keep alive until cancellation
        try { await Task.Delay(Timeout.Infinite, ct); }
        catch (TaskCanceledException) { }

        foreach (var drv in _running.Values)
        {
            try { await drv.StopAsync(); } catch { }
            try { await drv.DisposeAsync(); } catch { }
        }
    }

    private void TryStart(Instrument inst)
    {
        try
        {
            var driver = _drivers.Create(inst);
            driver.Log += (level, msg) => LogAsync(inst.Id, level, msg).GetAwaiter().GetResult();
            driver.RawReceived += raw => HandleRawAsync(inst, raw).GetAwaiter().GetResult();

            _ = driver.StartAsync(CancellationToken.None);
            _running[inst.Id] = driver;

            _log.LogInformation("Instrument {Name} driver started ({Kind}/{Proto})",
                inst.Name, inst.InterfaceKind, inst.ProtocolKind);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to start driver for {Name}", inst.Name);
            _ = LogAsync(inst.Id, "Error", ex.Message);
        }
    }

    private async Task HandleRawAsync(Instrument inst, string raw)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LabDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ResultDispatcher>();

        var parser = _parsers.Create(inst.ProtocolKind);
        var outcome = parser.Parse(raw);

        var msg = new InstrumentMessage
        {
            InstrumentId = inst.Id,
            Direction = MessageDirection.Inbound,
            Raw = raw,
            SampleId = outcome.SampleId,
            Summary = outcome.Success
                ? $"{outcome.Results.Count} results parsed"
                : outcome.Error,
            Status = outcome.Success ? MessageStatus.Parsed : MessageStatus.Error,
            ResultCount = outcome.Results.Count
        };
        db.InstrumentMessages.Add(msg);

        // Update instrument heartbeat
        var dbInst = await db.Instruments.FindAsync(inst.Id);
        if (dbInst is not null)
        {
            dbInst.LastSeenAt = DateTime.Now;
            dbInst.State = InstrumentState.Online;
        }

        await db.SaveChangesAsync();

        if (outcome.Success)
        {
            var saved = await dispatcher.DispatchAsync(inst.Id, outcome);
            msg.Status = saved > 0 ? MessageStatus.Dispatched : MessageStatus.Parsed;
            msg.Summary = $"{outcome.Results.Count} parsed, {saved} saved";
            await db.SaveChangesAsync();
        }
    }

    private async Task LogAsync(int instrumentId, string level, string message)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LabDbContext>();
        db.InstrumentLogs.Add(new InstrumentLog
        {
            InstrumentId = instrumentId,
            Level = level,
            Message = message
        });
        await db.SaveChangesAsync();
    }
}