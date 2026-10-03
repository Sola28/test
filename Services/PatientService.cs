using Microsoft.EntityFrameworkCore;
using ZDLISPlus.Web.Data;
using ZDLISPlus.Web.Models;
using ZDLISPlus.Web.Models.ViewModels;

namespace ZDLISPlus.Web.Services;

public class PatientService
{
    private readonly LabDbContext _db;
    public PatientService(LabDbContext db) => _db = db;

    public async Task<PatientListViewModel> ListAsync(string? q, int page, int pageSize)
    {
        var query = _db.Patients.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p =>
                p.FullName.Contains(q) ||
                p.MedicalRecordNumber.Contains(q) ||
                (p.Phone != null && p.Phone.Contains(q)));

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(p => p.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PatientListViewModel
        {
            Q = q,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Patients = items
        };
    }

    public Task<Patient?> GetAsync(int id) =>
        _db.Patients.Include(p => p.Orders).ThenInclude(o => o.Tests).ThenInclude(t => t.CatalogTest)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Patient> CreateAsync(Patient p)
    {
        p.MedicalRecordNumber = await NextMrnAsync();
        _db.Patients.Add(p);
        await _db.SaveChangesAsync();
        return p;
    }

    public async Task UpdateAsync(Patient p)
    {
        _db.Patients.Update(p);
        await _db.SaveChangesAsync();
    }

    private async Task<string> NextMrnAsync()
    {
        var last = await _db.Patients.OrderByDescending(p => p.Id).Select(p => p.MedicalRecordNumber).FirstOrDefaultAsync();
        var n = 1;
        if (last is not null && last.StartsWith("MRN-") && int.TryParse(last[4..], out var parsed)) n = parsed + 1;
        return $"MRN-{n:D4}";
    }
}