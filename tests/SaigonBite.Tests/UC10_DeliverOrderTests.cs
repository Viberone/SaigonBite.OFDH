using SaigonBite.Web.Controllers;
using SaigonBite.Web.Domain;
using SaigonBite.Web.Infrastructure;

namespace SaigonBite.Tests;

/// <summary>
/// UC10 Deliver an Order (author 523C0001). Test cases derived from the post-conditions and the Delivery state machine:
/// Assigned -> PickedUp -> Delivered (order Delivered, payment Collected or Captured, cash recorded)
///                       -> Failed (order DeliveryFailed, payment Voided, reason recorded).
/// </summary>
public class UC10_DeliverOrderTests : IDisposable
{
    private readonly TestDb t = new();
    public void Dispose() => t.Dispose();

    private DeliveryController As(int shipperId) => t.WithContext(new DeliveryController(t.Orders, t.Deliveries, t.Hubs, t.Gateway,
        new FixedCurrentUser(staff: t.Staff.GetById(shipperId)), t.Clock));

    /// <summary>Shipper 5 (hub 1) takes and picks up the order; returns the delivery.</summary>
    private Delivery TakeAndPickUp(int orderId, int shipper = 5)
    {
        var c = As(shipper);
        c.Accept(orderId);
        var d = t.Deliveries.GetByOrder(orderId)!;
        t.Clock.Now = t.Clock.Now.AddMinutes(4);
        c.PickUp(d.Id);
        return t.Deliveries.GetById(d.Id)!;
    }

    [Fact(DisplayName = "TC-10-01 Accept: a ready order gets an Assigned delivery for this shipper")]
    public void AcceptCreatesAssignedDelivery()
    {
        int orderId = t.ReadyOrderAtHub1(PaymentMethod.COD);
        As(5).Accept(orderId);
        var d = t.Deliveries.GetByOrder(orderId)!;
        Assert.Equal(DeliveryStatus.Assigned, d.Status);
        Assert.Equal(5, d.ShipperId);
        Assert.Equal(OrderStatus.ReadyForPickup, t.Orders.GetById(orderId)!.Status);
        Assert.DoesNotContain(t.Orders.GetReadyUnassigned(1), o => o.Id == orderId);
    }

    [Fact(DisplayName = "TC-10-02 Accept: an order already taken cannot be taken by a second shipper")]
    public void SecondShipperCannotTake()
    {
        int orderId = t.ReadyOrderAtHub1(PaymentMethod.COD);
        As(5).Accept(orderId);
        var second = As(4);
        second.Accept(orderId);
        Assert.Equal(5, t.Deliveries.GetByOrder(orderId)!.ShipperId);
        Assert.Contains("taken by another shipper", (string)second.TempData["Error"]!);
    }

    [Fact(DisplayName = "TC-10-03 Accept: a shipper of another hub cannot take the order")]
    public void OtherHubShipperRefused()
    {
        int orderId = t.ReadyOrderAtHub1(PaymentMethod.COD);
        var c = As(6); // shipper of hub 2
        c.Accept(orderId);
        Assert.Null(t.Deliveries.GetByOrder(orderId));
        Assert.Contains("own hub", (string)c.TempData["Error"]!);
    }

    [Fact(DisplayName = "TC-10-04 Pick up: delivery PickedUp and order OutForDelivery")]
    public void PickUpMovesOrderOut()
    {
        int orderId = t.ReadyOrderAtHub1(PaymentMethod.COD);
        var d = TakeAndPickUp(orderId);
        Assert.Equal(DeliveryStatus.PickedUp, d.Status);
        Assert.NotNull(d.PickedUpAt);
        Assert.Equal(OrderStatus.OutForDelivery, t.Orders.GetById(orderId)!.Status);
    }

    [Fact(DisplayName = "TC-10-05 Deliver cash order: exact cash recorded, payment Collected, order Delivered")]
    public void DeliverCashOrder()
    {
        int orderId = t.ReadyOrderAtHub1(PaymentMethod.COD);
        int total = t.Orders.GetById(orderId)!.Total;
        var d = TakeAndPickUp(orderId);
        t.Clock.Now = t.Clock.Now.AddMinutes(12);
        As(5).Complete(d.Id, total);

        var after = t.Deliveries.GetById(d.Id)!;
        Assert.Equal(DeliveryStatus.Delivered, after.Status);
        Assert.Equal(total, after.CashCollected);
        Assert.Equal(OrderStatus.Delivered, t.Orders.GetById(orderId)!.Status);
        Assert.Equal(PaymentStatus.Collected, t.Orders.GetPayment(orderId)!.Status);
        Assert.Equal(total, t.Deliveries.ShiftSummary(5, t.Clock.Today).Cash);
    }

    [Fact(DisplayName = "TC-10-06 Deliver cash order: a wrong cash amount is refused and nothing changes")]
    public void WrongCashRefused()
    {
        int orderId = t.ReadyOrderAtHub1(PaymentMethod.COD);
        int total = t.Orders.GetById(orderId)!.Total;
        var d = TakeAndPickUp(orderId);
        var c = As(5);
        c.Complete(d.Id, total - 5000);
        Assert.Equal(DeliveryStatus.PickedUp, t.Deliveries.GetById(d.Id)!.Status);
        Assert.Equal(OrderStatus.OutForDelivery, t.Orders.GetById(orderId)!.Status);
        Assert.Equal(PaymentStatus.Pending, t.Orders.GetPayment(orderId)!.Status);
        Assert.Contains("must equal the order total", (string)c.TempData["Error"]!);
    }

    [Fact(DisplayName = "TC-10-07 Deliver card order: online payment Captured, no cash recorded")]
    public void DeliverCardOrder()
    {
        int orderId = t.ReadyOrderAtHub1(PaymentMethod.Card);
        var d = TakeAndPickUp(orderId);
        As(5).Complete(d.Id, null);
        Assert.Null(t.Deliveries.GetById(d.Id)!.CashCollected);
        Assert.Equal(OrderStatus.Delivered, t.Orders.GetById(orderId)!.Status);
        Assert.Equal(PaymentStatus.Captured, t.Orders.GetPayment(orderId)!.Status);
    }

    [Fact(DisplayName = "TC-10-08 Report failure: delivery Failed with reason, order DeliveryFailed, payment Voided")]
    public void ReportFailure()
    {
        int orderId = t.ReadyOrderAtHub1(PaymentMethod.Card);
        var d = TakeAndPickUp(orderId);
        As(5).Fail(d.Id, "Customer did not answer for 10 minutes");
        var after = t.Deliveries.GetById(d.Id)!;
        Assert.Equal(DeliveryStatus.Failed, after.Status);
        Assert.Equal("Customer did not answer for 10 minutes", after.FailureReason);
        var o = t.Orders.GetById(orderId)!;
        Assert.Equal(OrderStatus.DeliveryFailed, o.Status);
        Assert.NotNull(o.ClosedAt);
        Assert.Equal(PaymentStatus.Voided, t.Orders.GetPayment(orderId)!.Status);
    }

    [Fact(DisplayName = "TC-10-09 A delivery cannot be completed before it is picked up")]
    public void CannotCompleteBeforePickUp()
    {
        int orderId = t.ReadyOrderAtHub1(PaymentMethod.Card);
        var c = As(5);
        c.Accept(orderId);
        var d = t.Deliveries.GetByOrder(orderId)!;
        c.Complete(d.Id, null);
        Assert.Equal(DeliveryStatus.Assigned, t.Deliveries.GetById(d.Id)!.Status);
        Assert.Contains("not PickedUp", (string)c.TempData["Error"]!);
    }
}
