namespace SaigonBite.Web.Domain;

/// <summary>
/// A shipper's delivery of one order (UC10 Deliver an Order, author 523C0001).
/// State machine: Assigned -> PickedUp -> (Delivered | Failed).
/// </summary>
public class Delivery
{
    public int Id { get; set; }
    public int OrderId { get; init; }
    public int ShipperId { get; init; }
    public DeliveryStatus Status { get; private set; } = DeliveryStatus.Assigned;
    public DateTime AssignedAt { get; init; }
    public DateTime? PickedUpAt { get; private set; }
    public DateTime? FinishedAt { get; private set; }
    public int? CashCollected { get; private set; }
    public string? FailureReason { get; private set; }

    public static Delivery Assign(Order order, Staff shipper, DateTime now)
    {
        if (shipper.Role != StaffRole.Shipper) throw new DomainRuleException("Only a shipper can take a delivery.");
        if (shipper.HubId != order.HubId) throw new DomainRuleException("Shippers deliver only orders of their own hub (A10).");
        if (order.Status != OrderStatus.ReadyForPickup) throw new DomainRuleException($"Order #{order.Id} is not ready for pickup.");
        return new Delivery { OrderId = order.Id, ShipperId = shipper.Id, AssignedAt = now };
    }

    public static Delivery Restore(Delivery d, DeliveryStatus status, DateTime? pickedUpAt, DateTime? finishedAt, int? cash, string? reason)
    {
        d.Status = status; d.PickedUpAt = pickedUpAt; d.FinishedAt = finishedAt; d.CashCollected = cash; d.FailureReason = reason;
        return d;
    }

    public void PickUp(DateTime now)
    {
        if (Status != DeliveryStatus.Assigned) throw new DomainRuleException($"Delivery #{Id} is {Status}, not Assigned.");
        Status = DeliveryStatus.PickedUp; PickedUpAt = now;
    }

    /// <summary>For cash on delivery the shipper must record exactly the order total (B3, zero cash differences).</summary>
    public void Complete(PaymentMethod method, int orderTotal, int? cashCollected, DateTime now)
    {
        if (Status != DeliveryStatus.PickedUp) throw new DomainRuleException($"Delivery #{Id} is {Status}, not PickedUp.");
        if (method == PaymentMethod.COD && cashCollected != orderTotal)
            throw new DomainRuleException($"Cash collected must equal the order total of {orderTotal:N0} VND.");
        Status = DeliveryStatus.Delivered; FinishedAt = now;
        CashCollected = method == PaymentMethod.COD ? cashCollected : null;
    }

    public void Fail(string reason, DateTime now)
    {
        if (Status != DeliveryStatus.PickedUp) throw new DomainRuleException($"Delivery #{Id} is {Status}, not PickedUp.");
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainRuleException("A reason is required for a failed delivery.");
        Status = DeliveryStatus.Failed; FinishedAt = now; FailureReason = reason;
    }
}

/// <summary>Payment for one order. Online payments are authorised at confirmation and captured on delivery (A08).</summary>
public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public PaymentMethod Method { get; init; }
    public int Amount { get; init; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public string? GatewayRef { get; private set; }

    public static Payment Restore(Payment p, PaymentStatus status, string? gatewayRef) { p.Status = status; p.GatewayRef = gatewayRef; return p; }

    public void Authorised(string gatewayRef) { Status = PaymentStatus.Authorised; GatewayRef = gatewayRef; }
    /// <summary>Delivered: online money is captured, cash is recorded as collected.</summary>
    public void Settle() => Status = Method == PaymentMethod.COD ? PaymentStatus.Collected : PaymentStatus.Captured;
    public void Void() => Status = PaymentStatus.Voided;
}
