using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZDLISPlus.Web.Data;
using ZDLISPlus.Web.Models;
using ZDLISPlus.Web.Models.ViewModels;
using ZDLISPlus.Web.Services;

namespace ZDLISPlus.Web.Controllers;

public class InstrumentsController : Controller
{
    private readonly LabDbContext _db;
    private readonly InstrumentService _svc;

    public InstrumentsController(LabDbContext db, InstrumentService svc)
    {
        _db = db;
        _svc = svc;
    }

    // ---------- LIST ----------
    public async Task<IActionResult> Index()
    {
        var instruments = await _db.Instruments
            .Include(i => i.TestMaps)
            .OrderBy(i => i.Discipline).ThenBy(i => i.Name)
            .ToListAsync();

        // Latest message per instrument
        var latest = await _db.InstrumentMessages
            .GroupBy(m => m.InstrumentId)
            .Select(g => new { InstrumentId = g.Key, At = g.Max(x => x.At) })
            .ToDictionaryAsync(x => x.InstrumentId, x => x.At);

        ViewData["LatestMessage"] = latest;
        return View(instruments);
    }

    // ---------- CREATE ----------
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new InstrumentEditViewModel
        {
            Disciplines = await DisciplinesAsync()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InstrumentEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Disciplines = await DisciplinesAsync();
            return View(vm);
        }

        var inst = new Instrument
        {
            Name = vm.Name.Trim(),
            Discipline = vm.Discipline.Trim(),
            Model = vm.Model?.Trim() ?? "",
            Manufacturer = vm.Manufacturer?.Trim() ?? "",
            SerialNumber = vm.SerialNumber?.Trim() ?? "",
            InterfaceKind = vm.InterfaceKind,
            ProtocolKind = vm.ProtocolKind,
            TcpHost = vm.TcpHost,
            TcpPort = vm.TcpPort,
            SerialPort = vm.SerialPort,
            SerialBaud = vm.SerialBaud,
            SerialDataBits = vm.SerialDataBits,
            SerialParity = vm.SerialParity,
            SerialStopBits = vm.SerialStopBits,
            WatchFolder = vm.WatchFolder,
            IsEnabled = vm.IsEnabled,
            AutoStart = vm.AutoStart,
            Notes = vm.Notes,
            State = InstrumentState.Offline,
            QueueNote = "Not connected"
        };

        _db.Instruments.Add(inst);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Instrument {inst.Name} created. Configure test mapping next.";
        return RedirectToAction(nameof(TestMap), new { id = inst.Id });
    }

    // ---------- EDIT ----------
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var i = await _db.Instruments.FindAsync(id);
        if (i is null) return NotFound();

        var vm = new InstrumentEditViewModel
        {
            Id = i.Id,
            Name = i.Name,
            Discipline = i.Discipline,
            Model = i.Model,
            Manufacturer = i.Manufacturer,
            SerialNumber = i.SerialNumber,
            InterfaceKind = i.InterfaceKind,
            ProtocolKind = i.ProtocolKind,
            TcpHost = i.TcpHost,
            TcpPort = i.TcpPort,
            SerialPort = i.SerialPort,
            SerialBaud = i.SerialBaud,
            SerialDataBits = i.SerialDataBits,
            SerialParity = i.SerialParity,
            SerialStopBits = i.SerialStopBits,
            WatchFolder = i.WatchFolder,
            IsEnabled = i.IsEnabled,
            AutoStart = i.AutoStart,
            Notes = i.Notes,
            Disciplines = await DisciplinesAsync()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(InstrumentEditViewModel vm)
    {
        if (vm.Id is null) return BadRequest();

        var i = await _db.Instruments.FindAsync(vm.Id.Value);
        if (i is null) return NotFound();

        if (!ModelState.IsValid)
        {
            vm.Disciplines = await DisciplinesAsync();
            return View(vm);
        }

        i.Name = vm.Name.Trim();
        i.Discipline = vm.Discipline.Trim();
        i.Model = vm.Model?.Trim() ?? "";
        i.Manufacturer = vm.Manufacturer?.Trim() ?? "";
        i.SerialNumber = vm.SerialNumber?.Trim() ?? "";
        i.InterfaceKind = vm.InterfaceKind;
        i.ProtocolKind = vm.ProtocolKind;
        i.TcpHost = vm.TcpHost;
        i.TcpPort = vm.TcpPort;
        i.SerialPort = vm.SerialPort;
        i.SerialBaud = vm.SerialBaud;
        i.SerialDataBits = vm.SerialDataBits;
        i.SerialParity = vm.SerialParity;
        i.SerialStopBits = vm.SerialStopBits;
        i.WatchFolder = vm.WatchFolder;
        i.IsEnabled = vm.IsEnabled;
        i.AutoStart = vm.AutoStart;
        i.Notes = vm.Notes;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Instrument updated. Restart app for driver changes to take effect.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- DELETE ----------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var i = await _db.Instruments.FindAsync(id);
        if (i is null) return NotFound();
        _db.Instruments.Remove(i);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Instrument deleted.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- TEST MAP ----------
    [HttpGet]
    public async Task<IActionResult> TestMap(int id)
    {
        var i = await _db.Instruments
            .Include(x => x.TestMaps).ThenInclude(m => m.ComponentTest).ThenInclude(c => c.ParentTest)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (i is null) return NotFound();

        var vm = new TestMapEditViewModel
        {
            InstrumentId = i.Id,
            InstrumentName = i.Name,
            Rows = i.TestMaps.OrderBy(m => m.InstrumentCode).Select(m => new TestMapRow
            {
                Id = m.Id,
                InstrumentCode = m.InstrumentCode,
                ComponentTestId = m.ComponentTestId,
                ComponentLabel = m.ComponentTest.ParentTest is null
                    ? $"{m.ComponentTest.Code} — {m.ComponentTest.Name}"
                    : $"{m.ComponentTest.ParentTest.Code} › {m.ComponentTest.Code} — {m.ComponentTest.Name}",
                Factor = m.Factor,
                UnitOverride = m.UnitOverride,
                IsActive = m.IsActive
            }).ToList(),
            AllTests = await _db.TestCatalog
                .Include(t => t.ParentTest)
                .Where(t => t.IsActive)
                .OrderBy(t => t.ParentTestId == null ? t.Code : t.ParentTest!.Code)
                .ThenBy(t => t.SortOrder)
                .ToListAsync()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMap(int instrumentId, string instrumentCode, int componentTestId,
        decimal? factor, string? unitOverride)
    {
        if (string.IsNullOrWhiteSpace(instrumentCode) || componentTestId <= 0)
        {
            TempData["Error"] = "Instrument code and component are required.";
            return RedirectToAction(nameof(TestMap), new { id = instrumentId });
        }

        var exists = await _db.InstrumentTestMaps.AnyAsync(m =>
            m.InstrumentId == instrumentId && m.InstrumentCode == instrumentCode);
        if (exists)
        {
            TempData["Error"] = $"Map for '{instrumentCode}' already exists.";
            return RedirectToAction(nameof(TestMap), new { id = instrumentId });
        }

        _db.InstrumentTestMaps.Add(new InstrumentTestMap
        {
            InstrumentId = instrumentId,
            InstrumentCode = instrumentCode.Trim(),
            ComponentTestId = componentTestId,
            Factor = factor,
            UnitOverride = string.IsNullOrWhiteSpace(unitOverride) ? null : unitOverride.Trim(),
            IsActive = true
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Mapped {instrumentCode}.";
        return RedirectToAction(nameof(TestMap), new { id = instrumentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMap(int id, int instrumentId)
    {
        var map = await _db.InstrumentTestMaps.FindAsync(id);
        if (map is not null)
        {
            _db.InstrumentTestMaps.Remove(map);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(TestMap), new { id = instrumentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleMap(int id, int instrumentId)
    {
        var map = await _db.InstrumentTestMaps.FindAsync(id);
        if (map is not null)
        {
            map.IsActive = !map.IsActive;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(TestMap), new { id = instrumentId });
    }

    // ---------- MESSAGES ----------
    public async Task<IActionResult> Messages(int id, int page = 1)
    {
        var instrument = await _db.Instruments.FindAsync(id);
        if (instrument is null) return NotFound();

        var pageSize = 25;
        var query = _db.InstrumentMessages.Where(m => m.InstrumentId == id)
            .OrderByDescending(m => m.At);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        ViewData["Instrument"] = instrument;
        ViewData["Page"] = page;
        ViewData["TotalPages"] = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        return View(items);
    }

    // ---------- STATE TOGGLE (existing) ----------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CycleState(int id)
    {
        await _svc.CycleStateAsync(id);
        TempData["Success"] = "State updated.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<string>> DisciplinesAsync() =>
        await _db.TestCatalog.Where(t => t.ParentTestId == null)
            .Select(t => t.Discipline).Distinct().OrderBy(d => d).ToListAsync();
}