using SaigonBite.Web.DataAccess;
using SaigonBite.Web.Infrastructure;
using SaigonBite.Web.Integration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// Data access layer: one SQLite file, created from Database/schema.sql + seed.sql on first run.
// Set "Database:Reset": true (or pass --Database:Reset=true) to rebuild it from the seed.
string dbPath = Path.Combine(builder.Environment.ContentRootPath, builder.Configuration["Database:Path"] ?? "App_Data/saigonbite.db");
string sqlFolder = Path.Combine(AppContext.BaseDirectory, "Database");
var db = Db.Create(dbPath, sqlFolder, reset: builder.Configuration.GetValue<bool>("Database:Reset"));
builder.Services.AddSingleton(db);
builder.Services.AddScoped<RuleDAO>();
builder.Services.AddScoped<HubDAO>();
builder.Services.AddScoped<MenuItemDAO>();
builder.Services.AddScoped<CustomerDAO>();
builder.Services.AddScoped<StaffDAO>();
builder.Services.AddScoped<OrderDAO>();
builder.Services.AddScoped<DeliveryDAO>();
builder.Services.AddScoped<VoucherDAO>();
builder.Services.AddScoped<ReportDAO>();

// External systems behind adapters (mocked in this release)
builder.Services.AddScoped<IMapService, MockMapService>();
builder.Services.AddSingleton<IPaymentGateway, MockPaymentGateway>();

builder.Services.AddScoped<ICurrentUser, CookieCurrentUser>();
builder.Services.AddSingleton<IClock, SystemClock>();

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();
app.MapStaticAssets();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();
app.Run();

public partial class Program;
