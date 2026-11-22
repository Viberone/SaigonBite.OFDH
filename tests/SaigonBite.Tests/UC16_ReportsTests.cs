using Microsoft.AspNetCore.Mvc;
using SaigonBite.Web.Controllers;
using SaigonBite.Web.Domain;

namespace SaigonBite.Tests;

/// <summary>
/// UC16 Produce Management Reports (author 523C0001). Post-conditions: the three reports show figures that match
/// the recorded orders and deliveries for the chosen period and hub; no data is changed (read-only use case).
/// </summary>
public class UC16_ReportsTests : IDisposable
{
    private readonly TestDb t = new();
    public void Dispose() => t.Dispose();

    private ReportController Manager() => t.WithContext(new ReportController(t.Reports, t.Hubs, t.Clock));
    private static readonly DateOnly From = new(2026, 11, 1), To = new(2026, 11, 7);

    private ManagementReport Show(DateOnly? from, DateOnly? to, int? hub = null) =>
        Assert.IsType<ReportVM>(Assert.IsType<ViewResult>(Manager().Index(from, to, hub)).Model).Report;

    [Fact(DisplayName = "TC-16-01 Sales: revenue and order count equal the delivered orders in the period")]
    public void SalesMatchOrders()
    {
        var r = Show(From, To);
        long expected = t.Scalar("SELECT SUM(total) FROM orders WHERE status = 'Delivered' AND substr(placed_at, 1, 10) BETWEEN '2026-11-01' AND '2026-11-07'");
        long count = t.Scalar("SELECT COUNT(*) FROM orders WHERE status = 'Delivered' AND substr(placed_at, 1, 10) BETWEEN '2026-11-01' AND '2026-11-07'");
        Assert.Equal(expected, r.SalesTotal.Revenue);
        Assert.Equal(count, r.SalesTotal.DeliveredOrders);
        Assert.All(r.Sales, s => Assert.Equal(s.FoodSales - s.Discounts + s.DeliveryFees + s.Surcharges, s.Revenue));
    }

    [Fact(DisplayName = "TC-16-02 Workload: orders placed per hub, including rejected and cancelled ones")]
    public void WorkloadCountsAllOrders()
    {
        var r = Show(From, To);
        long placed = t.Scalar("SELECT COUNT(*) FROM orders WHERE substr(placed_at, 1, 10) BETWEEN '2026-11-01' AND '2026-11-07'");
        Assert.Equal(placed, r.Workload.Sum(w => w.OrdersPlaced));
        Assert.All(r.Workload, w => Assert.InRange(w.PeakOrders, 0, w.OrdersPlaced));
    }

    [Fact(DisplayName = "TC-16-03 Hub filter: only the chosen hub is reported")]
    public void HubFilter()
    {
        var r = Show(From, To, 2);
        Assert.Single(r.Sales);
        Assert.Equal("SaigonBite Hub Binh Thanh", r.Sales[0].HubName);
        Assert.Single(r.Delivery);
    }

    [Fact(DisplayName = "TC-16-04 Delivery performance: a new on-time delivery is counted in the report")]
    public void NewDeliveryIsCounted()
    {
        var before = Show(null, null).Delivery.Single(d => d.HubName.EndsWith("District 1"));
        // deliver the seeded out-for-delivery order of hub 1 (placed at 10:20, delivered at 10:55 = 35 minutes)
        int orderId = (int)t.Scalar("SELECT order_id FROM orders WHERE hub_id = 1 AND status = 'OutForDelivery'");
        t.Execute($"UPDATE orders SET status = 'Delivered', closed_at = '2026-11-12 10:55:00' WHERE order_id = {orderId}");
        var after = Show(null, null).Delivery.Single(d => d.HubName.EndsWith("District 1"));
        Assert.Equal(before.Delivered + 1, after.Delivered);
        Assert.Equal(before.WithinTarget + 1, after.WithinTarget);
    }

    [Fact(DisplayName = "TC-16-05 An invalid period is corrected with a message")]
    public void InvalidPeriod()
    {
        var vm = Assert.IsType<ReportVM>(Assert.IsType<ViewResult>(Manager().Index(To, From, null)).Model);
        Assert.Contains(vm.Errors, e => e.Contains("start date"));
        Assert.True(vm.Report.From <= vm.Report.To);
    }

    [Fact(DisplayName = "TC-16-06 Producing reports changes no data")]
    public void ReadOnly()
    {
        string Snapshot() => string.Join("|", new[] { "orders", "delivery", "payment", "voucher" }
            .Select(tb => t.Scalar($"SELECT COUNT(*) FROM {tb}")))
            + "|" + t.Scalar("SELECT SUM(total) FROM orders");
        var before = Snapshot();
        Show(From, To);
        Show(null, null, 1);
        Assert.Equal(before, Snapshot());
    }
}
