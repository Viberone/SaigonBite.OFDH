using SaigonBite.Web.Domain;

namespace SaigonBite.Web.Controllers;

/// <summary>Data the customer posts in UC03 (one SSD message per step: items, address, voucher, payment method).</summary>
public class OrderForm
{
    public int HubId { get; set; }
    public Dictionary<int, int> Quantities { get; set; } = [];
    public int AddressId { get; set; }
    public string? VoucherCode { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;
}

public class OrderPageVM
{
    public required OrderForm Form { get; init; }
    public required Hub Hub { get; init; }
    public required BusinessRules Rules { get; init; }
    public List<MenuItem> Menu { get; init; } = [];
    public List<Address> Addresses { get; init; } = [];
    public IReadOnlyList<PaymentMethod> PaymentMethods { get; init; } = [];
    public List<OrderLine> Lines { get; init; } = [];
    public Address? Address { get; set; }
    public PriceQuote? Quote { get; set; }
    public List<string> Errors { get; init; } = [];
}

public record HubChoice(Hub Hub, string LoadLabel, double? DistanceKm, string? FromAddress);

public class HubBusyVM
{
    public required Hub Hub { get; init; }
    public List<HubChoice> Alternatives { get; init; } = [];
}

public record DeliveryCard(Delivery Delivery, Order Order);

public class ShipperBoardVM
{
    public required Staff Shipper { get; init; }
    public required Hub Hub { get; init; }
    public List<Order> Ready { get; init; } = [];
    public List<DeliveryCard> Mine { get; init; } = [];
    public (int Delivered, int Failed, int Cash) Shift { get; init; }
}

public class KitchenBoardVM
{
    public required Hub Hub { get; init; }
    public required BusinessRules Rules { get; init; }
    public List<Order> Incoming { get; init; } = [];
    public List<Order> Cooking { get; init; } = [];
    public List<Order> Ready { get; init; } = [];
    public List<MenuItem> Menu { get; init; } = [];
}

public class VoucherListVM
{
    public List<Voucher> Vouchers { get; init; } = [];
    public DateOnly Today { get; init; }
    public string? Filter { get; init; }
}

public class VoucherEditVM
{
    public required Voucher Voucher { get; init; }
    public bool IsNew { get; init; }
    public DateOnly Today { get; init; }
    public List<string> Errors { get; init; } = [];
}

public class ReportVM
{
    public required ManagementReport Report { get; init; }
    public List<Hub> Hubs { get; init; } = [];
    public List<string> Errors { get; init; } = [];
}
