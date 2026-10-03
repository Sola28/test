using Microsoft.EntityFrameworkCore;
using ZDLISPlus.Web.Data;
using ZDLISPlus.Web.Models;

namespace ZDLISPlus.Web.Services;

public class InstrumentService
{
    private readonly LabDbContext _db;
    public InstrumentService(LabDbContext db) => _db = db;

    public Task<List<Instrument>> AllAsync() =>
        _db.Instruments.OrderBy(i => i.Discipline).ThenBy(i => i.Name).ToListAsync();

    public Task<Instrument?> GetAsync(int id) => _db.Instruments.FirstOrDefaultAsync(i => i.Id == id);

    public async Task<Instrument> CreateAsync(Instrument i)
    {
        _db.Instruments.Add(i);
        await _db.SaveChangesAsync();
        return i;
    }

    public async Task UpdateAsync(Instrument i)
    {
        _db.Instruments.Update(i);
        await _db.SaveChangesAsync();
    }

    public async Task CycleStateAsync(int id)
    {
        var inst = await _db.Instruments.FindAsync(id);
        if (inst is null) return;
        inst.State = inst.State switch
        {
            InstrumentState.Online => InstrumentState.Idle,
            InstrumentState.Idle => InstrumentState.Offline,
            _ => InstrumentState.Online
        };
        inst.LastSeenAt = DateTime.Now;
        inst.QueueNote = inst.State == InstrumentState.Offline
            ? "Disconnected"
            : (inst.State == InstrumentState.Idle ? "Ready" : "0 queued");
        await _db.SaveChangesAsync();
    }
}