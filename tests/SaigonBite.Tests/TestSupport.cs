using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using SaigonBite.Web.DataAccess;
using SaigonBite.Web.Domain;
using SaigonBite.Web.Infrastructure;
using SaigonBite.Web.Integration;

namespace SaigonBite.Tests;

/// <summary>
/// A fresh database built from schema.sql and seed.sql for every test, plus the DAOs and helpers the tests use.
/// The clock is fixed at 12 Nov 2026 12:00, the "today" of the seed data.
/// </summary>
public sealed class TestDb : IDisposable
{
    public static readonly DateTime Now = new(2026, 11, 12, 12, 0, 0);
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"saigonbite-test-{Guid.NewGuid():N}.db");

    public Db Db { get; }
    public FixedClock Clock { get; } = new(Now);
    public MockPaymentGateway Gateway { get; } = new();
    public OrderDAO Orders { get; }
    public DeliveryDAO Deliveries { get; }
    public VoucherDAO Vouchers { get; }
    public HubDAO Hubs { get; }
    public MenuItemDAO Menu { get; }
    public RuleDAO Rules { get; }
    public CustomerDAO Customers { get; }
    public StaffDAO Staff { get; }
    public ReportDAO Reports { get; }
    public MockMapService Map { get; }

    public TestDb()
    {
        Db = Db.Create(_path, Path.Combine(AppContext.BaseDirectory, "Database"), reset: true);
        Orders = new(Db); Deliveries = new(Db); Vouchers = new(Db); Hubs = new(Db); Menu = new(Db); Rules = new(Db);
        Customers = new(Db); Staff = new(Db); Reports = new(Db); Map = new(Db);
    }

    public long Scalar(string sql)
    {
        using var c = Db.Open();
        return Convert.ToInt64(Db.Cmd(c, sql).ExecuteScalar());
    }

    public int Execute(string sql)
    {
        using var c = Db.Open();
        return Db.Cmd(c, sql).ExecuteNonQuery();
    }

    /// <summary>A ready, unassigned order at hub 1 paid by the given method (seeded "today" data).</summary>
    public int ReadyOrderAtHub1(PaymentMethod method) => (int)Scalar($"""
        SELECT o.order_id FROM orders o WHERE o.hub_id = 1 AND o.status = 'ReadyForPickup' AND o.payment_method = '{method}'
          AND NOT EXISTS (SELECT 1 FROM delivery d WHERE d.order_id = o.order_id) LIMIT 1
        """);

    /// <summary>An Active voucher no customer has used yet, so tests do not depend on the random history.</summary>
    public Voucher NewActiveVoucher(string code = "TEST10", DiscountType type = DiscountType.Percent, int value = 10, int? max = 30000, int min = 100000)
    {
        var v = new Voucher
        {
            Code = code, Description = "Test voucher", DiscountType = type, DiscountValue = value, MaxDiscount = max, MinOrderValue = min,
            StartDate = new DateOnly(2026, 11, 1), EndDate = new DateOnly(2026, 12, 31), UsageLimit = 10,
            StoredStatus = VoucherStatus.Active, CreatedAt = Now,
        };
        Vouchers.Insert(v);
        return v;
    }

    public T WithContext<T>(T controller) where T : Controller
    {
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.TempData = new TempDataDictionary(controller.HttpContext, new MemoryTempData());
        return controller;
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { File.Delete(_path); } catch (IOException) { }
    }

    private sealed class MemoryTempData : ITempDataProvider
    {
        private IDictionary<string, object> _data = new Dictionary<string, object>();
        public IDictionary<string, object> LoadTempData(HttpContext context) => _data;
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) => _data = values;
    }
}
