using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZDLISPlus.Web.Data;
using ZDLISPlus.Web.Services;
using ZDLISPlus.Web.Services.Instruments;
using ZDLISPlus.Web.Services.Instruments.Drivers;
using ZDLISPlus.Web.Services.Instruments.Parsing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<PermissionService>();
builder.Services.AddScoped<AuditService>();

// ---------- Database ----------
builder.Services.AddDbContext<LabDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default")
                ?? "Data Source=zdlisplus.db"));

// ---------- Identity ----------
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<LabDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.LogoutPath = "/Account/Logout";
    o.AccessDeniedPath = "/Account/Denied";
});

// ---------- MVC ----------
builder.Services.AddControllersWithViews();

// ---------- Application services ----------
builder.Services.AddScoped<PatientService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<InstrumentService>();
builder.Services.AddScoped<LabDataService>();
builder.Services.AddScoped<DbSeeder>();

// ---------- Instrument module ----------
builder.Services.AddSingleton<ProtocolParserFactory>();
builder.Services.AddSingleton<DriverFactory>();
builder.Services.AddScoped<ResultDispatcher>();
builder.Services.AddHostedService<InstrumentHostedService>();

var app = builder.Build();

// ---------- Seed ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LabDbContext>();
    db.Database.EnsureCreated();
    await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync();
}

// ---------- Pipeline ----------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();