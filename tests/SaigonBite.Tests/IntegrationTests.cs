using Microsoft.AspNetCore.Mvc;
using SaigonBite.Web.Controllers;
using SaigonBite.Web.Domain;
using SaigonBite.Web.Infrastructure;

namespace SaigonBite.Tests;

/// <summary>
/// Integration of the four main use cases of both students in one business scenario (report section 4.3.6):
/// UC15 (523C0007) creates and activates a voucher, UC03 (523C0007) places an order with it,
/// the kitchen (supporting UC07, UC08) cooks it, UC10 (523C0001) delivers it, and UC16 (523C0001) reports it.
/// </summary>
public class IntegrationTests : IDisposable
{
    private readonly TestDb t = new();
    public void Dispose() => t.Dispose();

    [Fact(DisplayName = "TC-INT-01 Voucher → order → kitchen → delivery → report work together in one system")]
    public void FullScenario()
    {
        // UC15: admin creates and activates a voucher
        var admin = t.WithContext(new VoucherController(t.Vouchers, new FixedCurrentUser(staff: t.Staff.GetById(10)), t.Clock));
        var created = Assert.IsType<RedirectToActionResult>(admin.Create(new Voucher
        {
            Code = "INTEG15", Description = "Integration test", DiscountType = DiscountType.Fixed, DiscountValue = 15000,
            MinOrderValue = 100000, StartDate = t.Clock.Today, EndDate = t.Clock.Today.AddDays(10), UsageLimit = 5,
        }));
        int voucherId = (int)created.RouteValues!["id"]!;
        admin.Activate(voucherId);

        // UC03: customer Emma orders at hub 1 with the voucher, paying by card
        var customer = t.WithContext(new OrderController(t.Hubs, t.Menu, t.Orders, t.Vouchers, t.Rules, t.Map, t.Gateway,
            new FixedCurrentUser(t.Customers.GetById(3)), t.Clock));
        var placed = Assert.IsType<RedirectToActionResult>(customer.Place(new OrderForm
        {
            HubId = 1, AddressId = 4, PaymentMethod = PaymentMethod.Card, VoucherCode = "INTEG15", Quantities = new() { [3] = 1, [11] = 1 },
        }));
        int orderId = (int)placed.RouteValues!["id"]!;
        Assert.Equal(110000 - 15000 + 15000, t.Orders.GetById(orderId)!.Total);   // 80,000 + 30,000 food, 15,000 voucher, 15,000 fee

        // supporting UC07, UC08: kitchen of hub 1 confirms, prepares and marks ready
        var kitchen = t.WithContext(new KitchenController(t.Orders, t.Hubs, t.Menu, t.Rules, t.Gateway,
            new FixedCurrentUser(staff: t.Staff.GetById(1)), t.Clock));
        t.Clock.Now = t.Clock.Now.AddMinutes(2); kitchen.Confirm(orderId);
        kitchen.Start(orderId);
        t.Clock.Now = t.Clock.Now.AddMinutes(14); kitchen.Ready(orderId);

        // UC10: shipper takes it, picks it up and delivers it
        var shipper = t.WithContext(new DeliveryController(t.Orders, t.Deliveries, t.Hubs, t.Gateway,
            new FixedCurrentUser(staff: t.Staff.GetById(5)), t.Clock));
        var before = t.Reports.Build(t.Clock.Today, t.Clock.Today, 1);
        shipper.Accept(orderId);
        int deliveryId = t.Deliveries.GetByOrder(orderId)!.Id;
        t.Clock.Now = t.Clock.Now.AddMinutes(3); shipper.PickUp(deliveryId);
        t.Clock.Now = t.Clock.Now.AddMinutes(9); shipper.Complete(deliveryId, null);

        var order = t.Orders.GetById(orderId)!;
        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.Equal(PaymentStatus.Captured, t.Orders.GetPayment(orderId)!.Status);
        Assert.Equal(28, order.MinutesToDoor!.Value, 0.01);

        // UC16: the manager's report includes the order (revenue and an on-time delivery)
        var after = t.Reports.Build(t.Clock.Today, t.Clock.Today, 1);
        Assert.Equal(before.SalesTotal.Revenue + order.Total, after.SalesTotal.Revenue);
        Assert.Equal(before.Delivery[0].WithinTarget + 1, after.Delivery[0].WithinTarget);

        // UC15: the voucher use is counted
        Assert.Equal(1, t.Vouchers.GetById(voucherId)!.UsedCount);
    }
}
