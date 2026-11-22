using Microsoft.AspNetCore.Mvc;
using SaigonBite.Web.DataAccess;
using SaigonBite.Web.Domain;
using SaigonBite.Web.Infrastructure;
using SaigonBite.Web.Integration;

namespace SaigonBite.Web.Controllers;

/// <summary>
/// UC10 Deliver an Order (author 523C0001). One action per SSD message:
/// acceptOrder -> Accept, pickUp -> PickUp, confirmDelivered(cash) -> Complete, reportFailure(reason) -> Fail.
/// </summary>
[Role("Shipper")]
public class DeliveryController(OrderDAO orders, DeliveryDAO deliveries, HubDAO hubs, IPaymentGateway gateway,
                                ICurrentUser user, IClock clock) : Controller
{
    private Staff Shipper => user.Staff!;

    public IActionResult Index()
    {
        var mine = deliveries.GetOpenForShipper(Shipper.Id)
            .Select(d => new DeliveryCard(d, orders.GetById(d.OrderId)!)).ToList();
        return View(new ShipperBoardVM
        {
            Shipper = Shipper, Hub = hubs.GetById(Shipper.HubId!.Value)!, Ready = orders.GetReadyUnassigned(Shipper.HubId.Value),
            Mine = mine, Shift = deliveries.ShiftSummary(Shipper.Id, clock.Today),
        });
    }

    // SSD 1: acceptOrder(orderId) -> delivery Assigned
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Accept(int orderId)
    {
        var order = orders.GetById(orderId);
        if (order == null) return NotFound();
        try
        {
            var d = Delivery.Assign(order, Shipper, clock.Now);
            if (deliveries.TryInsert(d)) TempData["Message"] = $"You took order #{orderId}. Pick it up at the counter.";
            else TempData["Error"] = $"Order #{orderId} was taken by another shipper.";
        }
        catch (DomainRuleException e) { TempData["Error"] = e.Message; }
        return RedirectToAction(nameof(Index));
    }

    // SSD 2: pickUp(deliveryId) -> delivery PickedUp, order OutForDelivery
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult PickUp(int id) => Step(id, (d, o) =>
    {
        d.PickUp(clock.Now);
        o.PickUp();
        deliveries.SaveProgress(d, o, null, clock.Now);
        return $"Order #{o.Id} is out for delivery to {o.DeliveryStreet}, {o.DeliveryDistrict}.";
    });

    [HttpGet]
    public IActionResult Complete(int id)
    {
        var d = deliveries.GetById(id);
        if (d == null || d.ShipperId != Shipper.Id) return NotFound();
        ViewBag.Error = TempData["Error"];
        return View(new DeliveryCard(d, orders.GetById(d.OrderId)!));
    }

    // SSD 3a: confirmDelivered(deliveryId, cashCollected) -> Delivered, payment settled
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Complete(int id, int? cashCollected)
    {
        var (d, o) = Load(id);
        if (d == null || o == null) return NotFound();
        try
        {
            d.Complete(o.PaymentMethod, o.Total, cashCollected, clock.Now);
            o.Deliver(clock.Now);
            var p = orders.GetPayment(o.Id)!;
            if (p.GatewayRef != null) gateway.Capture(p.GatewayRef);
            p.Settle();
            deliveries.SaveProgress(d, o, p, clock.Now);
            TempData["Message"] = o.PaymentMethod == PaymentMethod.COD
                ? $"Order #{o.Id} delivered. {cashCollected:N0} VND cash recorded."
                : $"Order #{o.Id} delivered. Online payment captured.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainRuleException e)
        {
            TempData["Error"] = e.Message;
            return RedirectToAction(nameof(Complete), new { id });
        }
    }

    // SSD 3b: reportFailure(deliveryId, reason) -> Failed, order DeliveryFailed, payment voided
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Fail(int id, string reason) => Step(id, (d, o) =>
    {
        d.Fail(reason, clock.Now);
        o.FailDelivery(reason, clock.Now);
        var p = orders.GetPayment(o.Id)!;
        if (p.GatewayRef != null) gateway.Void(p.GatewayRef);
        p.Void();
        deliveries.SaveProgress(d, o, p, clock.Now);
        return $"Failed delivery of order #{o.Id} reported. Office staff will follow up.";
    });

    private (Delivery?, Order?) Load(int id)
    {
        var d = deliveries.GetById(id);
        if (d == null || d.ShipperId != Shipper.Id) return (null, null);
        return (d, orders.GetById(d.OrderId));
    }

    private IActionResult Step(int id, Func<Delivery, Order, string> action)
    {
        var (d, o) = Load(id);
        if (d == null || o == null) return NotFound();
        try { TempData["Message"] = action(d, o); }
        catch (DomainRuleException e) { TempData["Error"] = e.Message; }
        return RedirectToAction(nameof(Index));
    }
}
