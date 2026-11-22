namespace SaigonBite.Web.Domain;

/// <summary>
/// Customer order (UC03 Place an Order, author 523C0007). Each method is one transition of the
/// Order state machine; a method called in the wrong state throws <see cref="DomainRuleException"/>.
/// </summary>
public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; init; }
    public int HubId { get; init; }
    public string DeliveryStreet { get; init; } = "";
    public string DeliveryDistrict { get; init; } = "";
    public double DistanceKm { get; init; }
    public OrderStatus Status { get; private set; } = OrderStatus.Placed;
    public PaymentMethod PaymentMethod { get; init; }
    public int FoodSubtotal { get; init; }
    public int DeliveryFee { get; init; }
    public int QuietDiscount { get; init; }
    public int? VoucherId { get; init; }
    public int VoucherDiscount { get; init; }
    public int Surcharge { get; init; }
    public int Total { get; init; }
    public DateTime PlacedAt { get; init; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? ReadyAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public string? Note { get; private set; }
    public List<OrderLine> Lines { get; init; } = [];

    // display data filled by the DAO
    public string HubName { get; init; } = "";
    public string CustomerName { get; init; } = "";
    public string? VoucherCode { get; init; }

    public bool IsActive => Status is OrderStatus.Placed or OrderStatus.Confirmed or OrderStatus.Preparing;
    public bool IsFinal => Status is OrderStatus.Delivered or OrderStatus.Cancelled or OrderStatus.Rejected or OrderStatus.DeliveryFailed;
    public bool CanCancel => Status == OrderStatus.Placed;

    /// <summary>Creates a Placed order from a confirmed price quote (post-condition of UC03).</summary>
    public static Order Place(Customer customer, Hub hub, Address address, List<OrderLine> lines, PriceQuote quote,
                              PaymentMethod method, DateTime now)
    {
        if (lines.Count == 0) throw new DomainRuleException("The order has no items.");
        if (!customer.CanPayWith(method)) throw new DomainRuleException($"{customer.CustomerType} customers cannot pay by {method}.");
        return new Order
        {
            CustomerId = customer.Id, HubId = hub.Id, DeliveryStreet = address.Street, DeliveryDistrict = address.District,
            DistanceKm = quote.DistanceKm, PaymentMethod = method, FoodSubtotal = quote.FoodSubtotal,
            DeliveryFee = quote.DeliveryFee, QuietDiscount = quote.QuietDiscount, VoucherId = quote.VoucherId,
            VoucherDiscount = quote.VoucherDiscount, Surcharge = quote.Surcharge, Total = quote.Total,
            PlacedAt = now, Lines = lines,
        };
    }

    /// <summary>Rebuilds an order read from the database (used by the DAO only).</summary>
    public static Order Restore(Order data, OrderStatus status, DateTime? confirmedAt, DateTime? readyAt, DateTime? closedAt, string? note)
    {
        data.Status = status; data.ConfirmedAt = confirmedAt; data.ReadyAt = readyAt; data.ClosedAt = closedAt; data.Note = note;
        return data;
    }

    private void Require(OrderStatus expected, string action)
    {
        if (Status != expected) throw new DomainRuleException($"Cannot {action}: order #{Id} is {Status}, not {expected}.");
    }

    // ---------- customer ----------
    public void Cancel(DateTime now)
    {
        if (!CanCancel) throw new DomainRuleException($"Order #{Id} can no longer be cancelled (it is {Status}).");
        Status = OrderStatus.Cancelled; ClosedAt = now; Note = "Cancelled by the customer";
    }

    // ---------- kitchen staff (supporting UC07, UC08) ----------
    public void Confirm(DateTime now) { Require(OrderStatus.Placed, "confirm"); Status = OrderStatus.Confirmed; ConfirmedAt = now; }
    public void Reject(string reason, DateTime now)
    {
        Require(OrderStatus.Placed, "reject");
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainRuleException("A reason is required to reject an order.");
        Status = OrderStatus.Rejected; ClosedAt = now; Note = reason;
    }
    public void StartPreparing() { Require(OrderStatus.Confirmed, "start preparing"); Status = OrderStatus.Preparing; }
    public void MarkReady(DateTime now) { Require(OrderStatus.Preparing, "mark ready"); Status = OrderStatus.ReadyForPickup; ReadyAt = now; }

    // ---------- shipper (UC10) ----------
    public void PickUp() { Require(OrderStatus.ReadyForPickup, "pick up"); Status = OrderStatus.OutForDelivery; }
    public void Deliver(DateTime now) { Require(OrderStatus.OutForDelivery, "deliver"); Status = OrderStatus.Delivered; ClosedAt = now; }
    public void FailDelivery(string reason, DateTime now)
    {
        Require(OrderStatus.OutForDelivery, "report a failed delivery");
        Status = OrderStatus.DeliveryFailed; ClosedAt = now; Note = reason;
    }

    /// <summary>Order-to-door time in minutes, for delivered orders.</summary>
    public double? MinutesToDoor => Status == OrderStatus.Delivered && ClosedAt is DateTime c ? (c - PlacedAt).TotalMinutes : null;
}

/// <summary>Part of an order (composition). Unit price is copied at ordering time.</summary>
public class OrderLine
{
    public int LineNo { get; init; }
    public int ItemId { get; init; }
    public string ItemName { get; init; } = "";
    public int Quantity { get; init; }
    public int UnitPrice { get; init; }
    public int LineTotal => Quantity * UnitPrice;
}
