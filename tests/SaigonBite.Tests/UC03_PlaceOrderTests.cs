using Microsoft.AspNetCore.Mvc;
using SaigonBite.Web.Controllers;
using SaigonBite.Web.Domain;
using SaigonBite.Web.Infrastructure;

namespace SaigonBite.Tests;

/// <summary>
/// UC03 Place an Order (author 523C0007). Test cases derived from the post-conditions:
/// an Order in state Placed with its OrderLines, a Payment (Pending for cash, Authorised online),
/// the price computed by the confirmed rules, the voucher use counted and the hub load increased.
/// </summary>
public class UC03_PlaceOrderTests : IDisposable
{
    private readonly TestDb t = new();
    public void Dispose() => t.Dispose();

    private OrderController As(int customerId) => t.WithContext(new OrderController(t.Hubs, t.Menu, t.Orders, t.Vouchers, t.Rules, t.Map, t.Gateway,
        new FixedCurrentUser(t.Customers.GetById(customerId)), t.Clock));

    private static OrderForm Form(int hub, int address, PaymentMethod pay, string? voucher = null, params (int item, int qty)[] items) => new()
    {
        HubId = hub, AddressId = address, PaymentMethod = pay, VoucherCode = voucher,
        Quantities = items.ToDictionary(i => i.item, i => i.qty),
    };

    private static int PlacedOrderId(IActionResult r) => (int)Assert.IsType<RedirectToActionResult>(r).RouteValues!["id"]!;

    [Fact(DisplayName = "TC-03-01 Cash order at a normal hub is Placed with lines, correct total and a pending payment")]
    public void CashOrderIsPlaced()
    {
        long loadBefore = t.Scalar("SELECT COUNT(*) FROM orders WHERE hub_id = 1 AND status IN ('Placed','Confirmed','Preparing')");
        // An (local), home in Quan 3 = 2.8 km from hub 1; 2 x rare beef pho (65,000) + 1 iced coffee (25,000)
        var id = PlacedOrderId(As(1).Place(Form(1, 1, PaymentMethod.COD, null, (1, 2), (7, 1))));

        var o = t.Orders.GetById(id)!;
        Assert.Equal(OrderStatus.Placed, o.Status);
        Assert.Equal(2, o.Lines.Count);
        Assert.Equal(155000, o.FoodSubtotal);
        Assert.Equal(20000, o.DeliveryFee);            // 15,000 for 2 km + 1 started km x 5,000
        Assert.Equal(175000, o.Total);
        var p = t.Orders.GetPayment(id)!;
        Assert.Equal(PaymentStatus.Pending, p.Status);
        Assert.Equal(175000, p.Amount);
        Assert.Equal(loadBefore + 1, t.Scalar("SELECT COUNT(*) FROM orders WHERE hub_id = 1 AND status IN ('Placed','Confirmed','Preparing')"));
    }

    [Fact(DisplayName = "TC-03-02 Tourist card order with a voucher: payment authorised, voucher discount applied and counted")]
    public void TouristCardOrderWithVoucher()
    {
        var v = t.NewActiveVoucher();
        // Emma (tourist), Rex Hotel in Quan 1 = 1.5 km from hub 1; 2 x special pho (80,000)
        var id = PlacedOrderId(As(3).Place(Form(1, 4, PaymentMethod.Card, "test10", (3, 2))));

        var o = t.Orders.GetById(id)!;
        Assert.Equal(160000, o.FoodSubtotal);
        Assert.Equal(16000, o.VoucherDiscount);       // 10% of the food subtotal
        Assert.Equal(15000, o.DeliveryFee);           // within the first 2 km
        Assert.Equal(159000, o.Total);
        Assert.Equal(v.Id, o.VoucherId);
        var p = t.Orders.GetPayment(id)!;
        Assert.Equal(PaymentStatus.Authorised, p.Status);
        Assert.StartsWith("MOCK-", p.GatewayRef);
        Assert.Equal(1, t.Vouchers.GetById(v.Id)!.UsedCount);
    }

    [Fact(DisplayName = "TC-03-03 A quiet hub gives 10% off the food subtotal")]
    public void QuietHubDiscount()
    {
        // Kenji (tourist), Crescent Mall in Quan 7 = 1.8 km from hub 3, which has no active orders (quiet)
        var id = PlacedOrderId(As(4).Place(Form(3, 6, PaymentMethod.COD, null, (12, 2))));
        var o = t.Orders.GetById(id)!;
        Assert.Equal(110000, o.FoodSubtotal);
        Assert.Equal(11000, o.QuietDiscount);
        Assert.Equal(114000, o.Total);                // 110,000 - 11,000 + 15,000
    }

    [Fact(DisplayName = "TC-03-04 A busy hub refuses the order and suggests quieter hubs")]
    public void BusyHubRefuses()
    {
        long before = t.Scalar("SELECT COUNT(*) FROM orders");
        var r = Assert.IsType<ViewResult>(As(2).Place(Form(2, 3, PaymentMethod.COD, null, (1, 1))));
        Assert.Equal("HubBusy", r.ViewName);
        var vm = Assert.IsType<HubBusyVM>(r.Model);
        Assert.All(vm.Alternatives, a => Assert.NotEqual(2, a.Hub.Id));
        Assert.Equal(before, t.Scalar("SELECT COUNT(*) FROM orders"));
    }

    [Fact(DisplayName = "TC-03-05 An address beyond 8 km is refused; no order is created")]
    public void OutsideRadius()
    {
        long before = t.Scalar("SELECT COUNT(*) FROM orders");
        // An's home in Quan 3 is 8.6 km from hub 3
        var r = Assert.IsType<ViewResult>(As(1).Place(Form(3, 1, PaymentMethod.COD, null, (1, 1))));
        Assert.Contains(Assert.IsType<OrderPageVM>(r.Model).Errors, e => e.Contains("outside the 8 km"));
        Assert.Equal(before, t.Scalar("SELECT COUNT(*) FROM orders"));
    }

    [Fact(DisplayName = "TC-03-06 A customer cannot use the same voucher twice")]
    public void VoucherOncePerCustomer()
    {
        t.NewActiveVoucher();
        As(3).Place(Form(1, 4, PaymentMethod.COD, "TEST10", (3, 2)));
        var r = Assert.IsType<ViewResult>(As(3).Quote(Form(1, 4, PaymentMethod.COD, "TEST10", (3, 2))));
        Assert.Contains(Assert.IsType<OrderPageVM>(r.Model).Errors, e => e.Contains("already used"));
    }

    [Fact(DisplayName = "TC-03-07 A local customer cannot pay by international card")]
    public void LocalCannotPayByCard()
    {
        var r = Assert.IsType<ViewResult>(As(1).Quote(Form(1, 1, PaymentMethod.Card, null, (1, 1))));
        Assert.Contains(Assert.IsType<OrderPageVM>(r.Model).Errors, e => e.Contains("Local customers can pay by"));
    }

    [Fact(DisplayName = "TC-03-08 A sold-out item cannot be ordered")]
    public void SoldOutItem()
    {
        // Shrimp salad (item 8) is sold out at hub 1 in the seed data
        var r = Assert.IsType<ViewResult>(As(1).Quote(Form(1, 1, PaymentMethod.COD, null, (8, 1))));
        Assert.Contains(Assert.IsType<OrderPageVM>(r.Model).Errors, e => e.Contains("sold out"));
    }

    [Fact(DisplayName = "TC-03-09 Cancel while Placed: order Cancelled, payment voided, voucher use returned; later cancel refused")]
    public void CancelOnlyWhilePlaced()
    {
        var v = t.NewActiveVoucher();
        var c = As(3);
        int id = PlacedOrderId(c.Place(Form(1, 4, PaymentMethod.Card, "TEST10", (3, 2))));
        c.Cancel(id);
        Assert.Equal(OrderStatus.Cancelled, t.Orders.GetById(id)!.Status);
        Assert.Equal(PaymentStatus.Voided, t.Orders.GetPayment(id)!.Status);
        Assert.Equal(0, t.Vouchers.GetById(v.Id)!.UsedCount);

        int id2 = PlacedOrderId(c.Place(Form(1, 4, PaymentMethod.COD, null, (3, 1))));
        var o = t.Orders.GetById(id2)!;
        o.Confirm(TestDb.Now);
        t.Orders.SaveStatus(o);
        c.Cancel(id2);
        Assert.Equal(OrderStatus.Confirmed, t.Orders.GetById(id2)!.Status);
        Assert.Contains("can no longer be cancelled", (string)c.TempData["Error"]!);
    }

    [Fact(DisplayName = "TC-03-10 If the hub fills up between the quote and the confirmation, the order is refused")]
    public void HubFillsUpBeforeConfirm()
    {
        // hub 3 has capacity 6: fill it after the quote was shown
        var c = As(4);
        Assert.Equal("Quote", Assert.IsType<ViewResult>(c.Quote(Form(3, 6, PaymentMethod.COD, null, (12, 1)))).ViewName);
        for (int i = 0; i < 6; i++)
            t.Execute($"INSERT INTO orders (customer_id, hub_id, delivery_street, delivery_district, distance_km, status, payment_method, food_subtotal, delivery_fee, total, placed_at) VALUES (6, 3, 'x', 'Quan 7', 1.8, 'Placed', 'COD', 55000, 15000, 70000, '2026-11-12 11:59:00')");
        var r = Assert.IsType<ViewResult>(c.Place(Form(3, 6, PaymentMethod.COD, null, (12, 1))));
        Assert.Equal("HubBusy", r.ViewName);

        // the data access layer checks the load again inside the insert transaction (the peak-hour race)
        var kenji = t.Customers.GetById(4)!;
        var hub = new Hub { Id = 3, Capacity = 6 };
        var lines = new List<OrderLine> { new() { LineNo = 1, ItemId = 12, Quantity = 1, UnitPrice = 55000 } };
        var quote = new PriceCalculator(new BusinessRules()).Quote(hub, lines, 1.8, null);
        var order = Order.Place(kenji, hub, kenji.Addresses.First(a => a.Id == 6), lines, quote, PaymentMethod.COD, TestDb.Now);
        Assert.Throws<SaigonBite.Web.DataAccess.HubBusyException>(() =>
            t.Orders.InsertPlaced(order, new Payment { Method = PaymentMethod.COD, Amount = order.Total }, 1.0));
    }

    [Theory(DisplayName = "TC-03-11 Delivery fee: 15,000 VND up to 2 km, then 5,000 VND per started km")]
    [InlineData(1.5, 15000)]
    [InlineData(2.0, 15000)]
    [InlineData(2.1, 20000)]
    [InlineData(4.2, 30000)]
    [InlineData(8.0, 45000)]
    public void DeliveryFeeTable(double km, int fee) => Assert.Equal(fee, new PriceCalculator(new BusinessRules()).DeliveryFee(km));
}
