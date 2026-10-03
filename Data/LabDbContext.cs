using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ZDLISPlus.Web.Models;

namespace ZDLISPlus.Web.Data;

public class LabDbContext : IdentityDbContext
{
    public LabDbContext(DbContextOptions<LabDbContext> options) : base(options) { }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<LabOrder> LabOrders => Set<LabOrder>();
    public DbSet<OrderTest> OrderTests => Set<OrderTest>();
    public DbSet<TestCatalog> TestCatalog => Set<TestCatalog>();
    public DbSet<Instrument> Instruments => Set<Instrument>();
    public DbSet<AuditEntry> AuditTrail => Set<AuditEntry>();
    public DbSet<OrderTestResult> OrderTestResults => Set<OrderTestResult>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<InstrumentTestMap> InstrumentTestMaps => Set<InstrumentTestMap>();
    public DbSet<InstrumentMessage> InstrumentMessages => Set<InstrumentMessage>();
    public DbSet<InstrumentLog> InstrumentLogs => Set<InstrumentLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Patient>()
            .HasIndex(p => p.MedicalRecordNumber).IsUnique();

        b.Entity<LabOrder>()
            .HasIndex(o => o.Accession).IsUnique();

        b.Entity<LabOrder>()
            .HasOne(o => o.Patient)
            .WithMany(p => p.Orders)
            .HasForeignKey(o => o.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<OrderTest>()
            .HasOne(t => t.Order)
            .WithMany(o => o.Tests)
            .HasForeignKey(t => t.OrderId);

        b.Entity<OrderTest>()
            .HasOne(t => t.CatalogTest)
            .WithMany()
            .HasForeignKey(t => t.CatalogTestId);

        b.Entity<TestCatalog>()
            .HasIndex(t => t.Code).IsUnique();

        b.Entity<TestCatalog>()
    .HasOne(t => t.ParentTest)
    .WithMany(t => t.Children)
    .HasForeignKey(t => t.ParentTestId)
    .OnDelete(DeleteBehavior.Restrict);

        b.Entity<TestCatalog>()
            .HasIndex(t => new { t.Code, t.ParentTestId }).IsUnique();

        b.Entity<OrderTestResult>()
    .HasOne(r => r.Order)
    .WithMany()
    .HasForeignKey(r => r.OrderId)
    .OnDelete(DeleteBehavior.Cascade);

        b.Entity<OrderTestResult>()
            .HasOne(r => r.OrderedTest)
            .WithMany()
            .HasForeignKey(r => r.OrderedTestId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<OrderTestResult>()
            .HasOne(r => r.ComponentTest)
            .WithMany()
            .HasForeignKey(r => r.ComponentTestId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<OrderTestResult>()
            .HasIndex(r => new { r.OrderId, r.OrderedTestId, r.ComponentTestId })
            .IsUnique();

        b.Entity<RolePermission>()
    .HasIndex(p => new { p.RoleId, p.ModuleKey })
    .IsUnique();

        b.Entity<InstrumentTestMap>()
    .HasIndex(m => new { m.InstrumentId, m.InstrumentCode }).IsUnique();

        b.Entity<InstrumentMessage>()
            .HasIndex(m => new { m.InstrumentId, m.At });

        b.Entity<InstrumentMessage>()
            .HasOne(m => m.Instrument).WithMany(i => i.Messages)
            .HasForeignKey(m => m.InstrumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }

}