using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZDLISPlus.Web.Models;

namespace ZDLISPlus.Web.Data;

public class DbSeeder
{
    private readonly LabDbContext _db;
    private readonly UserManager<IdentityUser> _users;
    private readonly RoleManager<IdentityRole> _roles;

    public DbSeeder(LabDbContext db, UserManager<IdentityUser> users, RoleManager<IdentityRole> roles)
        => (_db, _users, _roles) = (db, users, roles);

    public async Task SeedAsync()
    {
        if (await _db.Patients.AnyAsync()) return;

        // ---------- Roles ----------
        foreach (var role in new[] { "Administrator", "Technologist", "Pathologist", "Reception" })
            if (!await _roles.RoleExistsAsync(role))
                await _roles.CreateAsync(new IdentityRole(role));

        // ---------- Admin user ----------
        var admin = new IdentityUser
        {
            UserName = "pedro@zdlis.local",
            Email = "pedro@zdlis.local",
            EmailConfirmed = true
        };
        if (await _users.FindByNameAsync(admin.UserName!) is null)
        {
            await _users.CreateAsync(admin, "Admin123");
            await _users.AddToRoleAsync(admin, "Administrator");
        }

        // ---------- Demo users (one per role) ----------
        var demos = new (string Email, string Role)[]
        {
            ("tech@zdlis.local",      "Technologist"),
            ("path@zdlis.local",      "Pathologist"),
            ("reception@zdlis.local", "Reception"),
        };

        foreach (var (email, role) in demos)
        {
            if (await _users.FindByNameAsync(email) is null)
            {
                var u = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                await _users.CreateAsync(u, "Passw0rd!");
                await _users.AddToRoleAsync(u, role);
            }
        }

        // ---------- Default role permissions ----------
        // Only run if the table is empty (idempotent).
        if (!await _db.RolePermissions.AnyAsync())
        {
            var adminRoleId = (await _roles.FindByNameAsync("Administrator"))!.Id;
            var techRoleId = (await _roles.FindByNameAsync("Technologist"))!.Id;
            var pathRoleId = (await _roles.FindByNameAsync("Pathologist"))!.Id;
            var receptRoleId = (await _roles.FindByNameAsync("Reception"))!.Id;

            // Administrator — every module (kept in sync even though the
            // check short-circuits for admins, so the Roles grid shows it
            // as fully granted).
            foreach (var m in AppModules.All)
                _db.RolePermissions.Add(new RolePermission
                {
                    RoleId = adminRoleId,
                    ModuleKey = m.Key
                });

            // Technologist — bench work
            foreach (var key in new[] { "Home", "Patients", "Orders", "Results", "Validation", "Instruments" })
                _db.RolePermissions.Add(new RolePermission
                {
                    RoleId = techRoleId,
                    ModuleKey = key
                });

            // Pathologist — review, sign out, look at analytics and audit
            foreach (var key in new[] { "Home", "Patients", "Orders", "Results", "Validation", "Reports", "Audit" })
                _db.RolePermissions.Add(new RolePermission
                {
                    RoleId = pathRoleId,
                    ModuleKey = key
                });

            // Reception — front desk: register patients, place orders
            foreach (var key in new[] { "Home", "Patients", "Orders" })
                _db.RolePermissions.Add(new RolePermission
                {
                    RoleId = receptRoleId,
                    ModuleKey = key
                });

            await _db.SaveChangesAsync();
        }

        // ---------- Instruments ----------
        _db.Instruments.AddRange(
           new Instrument
           {
               Name = "FUJI DRI-CHEM NX600",
               Discipline = "Chemistry",
               Manufacturer = "Fuji",
               Model = "NX600",
               InterfaceKind = InterfaceKind.Serial,
               ProtocolKind = ProtocolKind.HL7,
               SerialPort = "COM3",
               SerialBaud = 9600,
               SerialDataBits = 8,
               SerialParity = "None",
               SerialStopBits = "One",
               State = InstrumentState.Offline,
               QueueNote = "Not connected",
               IsEnabled = true,
               AutoStart = false
           },
           new Instrument
           {
               Name = "Sysmex XN-1000",
               Discipline = "Hematology",
               Manufacturer = "Sysmex",
               Model = "XN-1000",
               InterfaceKind = InterfaceKind.TcpServer,
               ProtocolKind = ProtocolKind.HL7,
               TcpPort = 5555,
               State = InstrumentState.Offline,
               QueueNote = "Not connected",
               IsEnabled = true,
               AutoStart = false
           },
           new Instrument
           {
               Name = "Urisys 1100",
               Discipline = "Urinalysis",
               Manufacturer = "Roche",
               Model = "Urisys 1100",
               InterfaceKind = InterfaceKind.Serial,
               ProtocolKind = ProtocolKind.ASTM,
               SerialPort = "COM5",
               SerialBaud = 9600,
               SerialDataBits = 8,
               SerialParity = "None",
               SerialStopBits = "One",
               State = InstrumentState.Offline,
               QueueNote = "Not connected",
               IsEnabled = true,
               AutoStart = false
           },
           new Instrument
           {
               Name = "Cobas e 411",
               Discipline = "Immunology",
               Manufacturer = "Roche",
               Model = "Cobas e 411",
               InterfaceKind = InterfaceKind.TcpClient,
               ProtocolKind = ProtocolKind.HL7,
               TcpHost = "192.168.1.100",
               TcpPort = 5000,
               State = InstrumentState.Offline,
               QueueNote = "Not connected",
               IsEnabled = true,
               AutoStart = false
           }
       );

        // ---------- Test catalog: profiles (parents) ----------
        var profiles = new List<TestCatalog>
        {
            new() { Code = "CBC",     Name = "Complete Blood Count", Discipline = "Hematology", SampleType = "EDTA Whole Blood", TurnaroundMinutes = 30, Price = 350m,  IsProfile = true, IsOrderable = true, SortOrder = 1 },
            new() { Code = "LIPID",   Name = "Lipid Profile",        Discipline = "Chemistry",  SampleType = "Serum",            TurnaroundMinutes = 60, Price = 850m,  IsProfile = true, IsOrderable = true, SortOrder = 2 },
            new() { Code = "UA",      Name = "Urinalysis",           Discipline = "Urinalysis", SampleType = "Random Urine",     TurnaroundMinutes = 30, Price = 150m,  IsProfile = true, IsOrderable = true, SortOrder = 3 },
            new() { Code = "THYROID", Name = "Thyroid Panel",        Discipline = "Immunology", SampleType = "Serum",            TurnaroundMinutes = 90, Price = 1400m, IsProfile = true, IsOrderable = true, SortOrder = 4 },
            new() { Code = "CHEM",    Name = "Chemistry Panel",      Discipline = "Chemistry",  SampleType = "Serum",            TurnaroundMinutes = 60, Price = 1200m, IsProfile = true, IsOrderable = true, SortOrder = 5 },

            // Standalone orderable tests (no children)
            new() { Code = "HBA1C", Name = "Hemoglobin A1c",       Discipline = "Chemistry",  SampleType = "EDTA Whole Blood", TurnaroundMinutes = 60, Price = 950m,  IsProfile = false, IsOrderable = true, Unit = "%",      ReferenceRange = "4.0 – 5.6", SortOrder = 6 },
            new() { Code = "FBS",   Name = "Fasting Blood Sugar",  Discipline = "Chemistry",  SampleType = "Fluoride Oxalate", TurnaroundMinutes = 45, Price = 180m,  IsProfile = false, IsOrderable = true, Unit = "mg/dL",  ReferenceRange = "70 – 100",  SortOrder = 7 },
            new() { Code = "CREA",  Name = "Creatinine",           Discipline = "Chemistry",  SampleType = "Serum",            TurnaroundMinutes = 45, Price = 250m,  IsProfile = false, IsOrderable = true, Unit = "mg/dL",  ReferenceRange = "0.6 – 1.2", SortOrder = 8 },
            new() { Code = "TROP",  Name = "Troponin I",           Discipline = "Immunology", SampleType = "Serum",            TurnaroundMinutes = 30, Price = 1200m, IsProfile = false, IsOrderable = true, Unit = "ng/mL",  ReferenceRange = "< 0.04",    SortOrder = 9 },
            new() { Code = "CKMB",  Name = "CK-MB",                Discipline = "Immunology", SampleType = "Serum",            TurnaroundMinutes = 30, Price = 900m,  IsProfile = false, IsOrderable = true, Unit = "ng/mL",  ReferenceRange = "< 5.0",     SortOrder = 10 },
            new() { Code = "PTINR", Name = "PT / INR",             Discipline = "Hematology", SampleType = "Citrated Plasma",  TurnaroundMinutes = 45, Price = 500m,  IsProfile = false, IsOrderable = true, Unit = "ratio",  ReferenceRange = "0.8 – 1.2", SortOrder = 11 },
        };

        _db.TestCatalog.AddRange(profiles);
        await _db.SaveChangesAsync();

        // ---------- Test catalog: children ----------
        var byCode = await _db.TestCatalog
            .Where(t => t.ParentTestId == null)
            .ToDictionaryAsync(t => t.Code);

        void AddChild(string parentCode, string code, string name, string unit, string refRange, int sort)
        {
            _db.TestCatalog.Add(new TestCatalog
            {
                Code = code,
                Name = name,
                Discipline = byCode[parentCode].Discipline,
                SampleType = byCode[parentCode].SampleType,
                ParentTestId = byCode[parentCode].Id,
                IsProfile = false,
                IsOrderable = false,
                Unit = unit,
                ReferenceRange = refRange,
                SortOrder = sort
            });
        }

        // CBC components
        AddChild("CBC", "WBC", "White Blood Cell Count", "×10⁹/L", "4.0 – 11.0", 1);
        AddChild("CBC", "RBC", "Red Blood Cell Count", "×10¹²/L", "4.5 – 5.9", 2);
        AddChild("CBC", "HGB", "Hemoglobin", "g/dL", "13.0 – 17.0", 3);
        AddChild("CBC", "HCT", "Hematocrit", "%", "40 – 52", 4);
        AddChild("CBC", "MCV", "Mean Corpuscular Volume", "fL", "80 – 100", 5);
        AddChild("CBC", "MCH", "Mean Corpuscular Hgb", "pg", "27 – 33", 6);
        AddChild("CBC", "MCHC", "Mean Corpuscular Hgb Conc", "g/dL", "32 – 36", 7);
        AddChild("CBC", "PLT", "Platelet Count", "×10⁹/L", "150 – 450", 8);

        // Lipid profile
        AddChild("LIPID", "CHOL", "Total Cholesterol", "mg/dL", "< 200", 1);
        AddChild("LIPID", "TRIG", "Triglycerides", "mg/dL", "< 150", 2);
        AddChild("LIPID", "HDL", "HDL Cholesterol", "mg/dL", "> 40", 3);
        AddChild("LIPID", "LDL", "LDL Cholesterol", "mg/dL", "< 100", 4);

        // Urinalysis
        AddChild("UA", "UCOL", "Color", "", "Yellow", 1);
        AddChild("UA", "UAPP", "Appearance", "", "Clear", 2);
        AddChild("UA", "UPH", "pH", "", "4.6 – 8.0", 3);
        AddChild("UA", "USG", "Specific Gravity", "", "1.005 – 1.030", 4);
        AddChild("UA", "UPRO", "Protein", "", "Negative", 5);
        AddChild("UA", "UGLU", "Glucose", "", "Negative", 6);
        AddChild("UA", "UKET", "Ketones", "", "Negative", 7);
        AddChild("UA", "UBLD", "Blood", "", "Negative", 8);
        AddChild("UA", "UNI", "Nitrite", "", "Negative", 9);
        AddChild("UA", "ULEU", "Leukocytes", "", "Negative", 10);
        AddChild("UA", "URBC", "RBC (microscopy)", "/HPF", "0 – 2", 11);
        AddChild("UA", "UWBC", "WBC (microscopy)", "/HPF", "0 – 5", 12);
        AddChild("UA", "UEPI", "Epithelial Cells", "/HPF", "Few", 13);

        // Thyroid panel
        AddChild("THYROID", "TSH", "Thyroid Stimulating Hormone", "mIU/L", "0.4 – 4.0", 1);
        AddChild("THYROID", "FT4", "Free T4", "pmol/L", "9 – 19", 2);
        AddChild("THYROID", "FT3", "Free T3", "pmol/L", "3.5 – 6.5", 3);

        // Chemistry panel
        AddChild("CHEM", "BUN", "Blood Urea Nitrogen", "mg/dL", "7 – 20", 1);
        AddChild("CHEM", "URIC", "Uric Acid", "mg/dL", "3.4 – 7.0", 2);
        AddChild("CHEM", "ALT", "ALT (SGPT)", "U/L", "7 – 56", 3);
        AddChild("CHEM", "AST", "AST (SGOT)", "U/L", "10 – 40", 4);

        await _db.SaveChangesAsync();

        // ---------- Patients ----------
        var patients = new[]
        {
            new Patient { MedicalRecordNumber = "MRN-0001", FullName = "Maria Santos",    Gender = "F", Age = 42, BirthDate = new DateTime(1983, 5, 12),  Phone = "0917-111-2222", Address = "Quezon City" },
            new Patient { MedicalRecordNumber = "MRN-0002", FullName = "Roberto Cruz",    Gender = "M", Age = 57, BirthDate = new DateTime(1968, 2, 3),   Phone = "0917-222-3333", Address = "Makati" },
            new Patient { MedicalRecordNumber = "MRN-0003", FullName = "Ana Reyes",       Gender = "F", Age = 29, BirthDate = new DateTime(1996, 8, 22),  Phone = "0917-333-4444", Address = "Pasig" },
            new Patient { MedicalRecordNumber = "MRN-0004", FullName = "Daniel Lim",      Gender = "M", Age = 66, BirthDate = new DateTime(1959, 1, 14),  Phone = "0917-444-5555", Address = "Taguig" },
            new Patient { MedicalRecordNumber = "MRN-0005", FullName = "Lorna Garcia",    Gender = "F", Age = 34, BirthDate = new DateTime(1991, 11, 8),  Phone = "0917-555-6666", Address = "Mandaluyong" },
            new Patient { MedicalRecordNumber = "MRN-0006", FullName = "Jose Mendoza",    Gender = "M", Age = 48, BirthDate = new DateTime(1977, 6, 30),  Phone = "0917-666-7777", Address = "Manila" },
            new Patient { MedicalRecordNumber = "MRN-0007", FullName = "Carla Villanueva", Gender = "F", Age = 25, BirthDate = new DateTime(2000, 3, 17), Phone = "0917-777-8888", Address = "Pasay" },
        };
        _db.Patients.AddRange(patients);
        await _db.SaveChangesAsync();

        // ---------- Sample orders ----------
        var orderableTests = await _db.TestCatalog
            .Where(t => t.IsOrderable && t.ParentTestId == null)
            .ToListAsync();

        var rnd = new Random(42);
        var orderTypes = new[] { "Outpatient", "Inpatient", "Emergency" };
        var statuses = new[]
        {
            OrderStatus.Collected, OrderStatus.Processing,
            OrderStatus.ForValidation, OrderStatus.Released, OrderStatus.Critical
        };

        for (int i = 0; i < 20; i++)
        {
            var p = patients[rnd.Next(patients.Length)];
            var hourOffset = rnd.Next(0, 5);

            var order = new LabOrder
            {
                Accession = $"ZL-{DateTime.Today:yyMMdd}-{(42 - i):D3}",
                PatientId = p.Id,
                OrderType = orderTypes[rnd.Next(orderTypes.Length)],
                OrderedAt = DateTime.Today.AddHours(8.5 - hourOffset).AddMinutes(rnd.Next(0, 60)),
                Status = statuses[rnd.Next(statuses.Length)],
                Priority = rnd.NextDouble() > 0.75 ? OrderPriority.Urgent : OrderPriority.Normal,
                RequestingPhysician = "Dr. Reyes"
            };

            var picked = orderableTests.OrderBy(_ => rnd.Next()).Take(rnd.Next(1, 4)).ToList();
            foreach (var t in picked)
            {
                order.Tests.Add(new OrderTest
                {
                    CatalogTestId = t.Id,
                    Result = rnd.NextDouble() > 0.5 ? rnd.Next(60, 200).ToString() : null,
                    Flag = rnd.NextDouble() > 0.85 ? "H" : null
                });
            }

            _db.LabOrders.Add(order);
        }
        await _db.SaveChangesAsync();

        // ---------- Audit trail ----------
        _db.AuditTrail.AddRange(
            new AuditEntry { User = "pedro@zdlis.local", Action = "Created", Entity = "LabOrder", EntityKey = "ZL-260918-042", Details = "Order created for Maria Santos" },
            new AuditEntry { User = "pedro@zdlis.local", Action = "Validated", Entity = "LabOrder", EntityKey = "ZL-260918-040", Details = "Result released by technologist" },
            new AuditEntry { User = "admin@zdlis.local", Action = "Login", Entity = "User", EntityKey = "pedro@zdlis.local", Details = "Successful sign-in" }
        );
        await _db.SaveChangesAsync();
    }
}