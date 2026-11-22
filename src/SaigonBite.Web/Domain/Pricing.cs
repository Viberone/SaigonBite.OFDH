namespace SaigonBite.Web.Domain;

/// <summary>The full price shown to the customer before confirming (UC03 step "show full price").</summary>
public class PriceQuote
{
    public int FoodSubtotal { get; init; }
    public double DistanceKm { get; init; }
    public int BilledKm { get; init; }
    public int DeliveryFee { get; init; }
    public int QuietDiscount { get; init; }
    public int? VoucherId { get; init; }
    public string? VoucherCode { get; init; }
    public int VoucherDiscount { get; init; }
    public int Surcharge { get; init; }
    public int Total => FoodSubtotal - QuietDiscount - VoucherDiscount + DeliveryFee + Surcharge;
}

/// <summary>Pure pricing rules (Information Expert for the price). Values come from <see cref="BusinessRules"/>.</summary>
public class PriceCalculator(BusinessRules rules)
{
    public BusinessRules Rules => rules;

    /// <summary>Base fee covers BaseKm; every started kilometre after that costs PerKmFee (Q2).</summary>
    public int DeliveryFee(double distanceKm)
    {
        int started = (int)Math.Ceiling(distanceKm - 1e-9);
        return rules.BaseFee + Math.Max(0, started - rules.BaseKm) * rules.PerKmFee;
    }

    public bool WithinRadius(double distanceKm) => distanceKm <= rules.MaxKm;

    public int QuietDiscount(Hub hub, int foodSubtotal) =>
        hub.IsQuiet(rules) ? Math.Min((int)(foodSubtotal * rules.QuietRate), rules.QuietMax) : 0;

    public PriceQuote Quote(Hub hub, IEnumerable<OrderLine> lines, double distanceKm, Voucher? voucher)
    {
        int food = lines.Sum(l => l.LineTotal);
        int quiet = QuietDiscount(hub, food);
        return new PriceQuote
        {
            FoodSubtotal = food,
            DistanceKm = distanceKm,
            BilledKm = (int)Math.Ceiling(distanceKm - 1e-9),
            DeliveryFee = DeliveryFee(distanceKm),
            QuietDiscount = quiet,
            VoucherId = voucher?.Id,
            VoucherCode = voucher?.Code,
            // both discounts apply to food only, so together they never exceed the food subtotal
            VoucherDiscount = Math.Min(voucher?.DiscountFor(food) ?? 0, food - quiet),
            Surcharge = hub.RainSurchargeOn ? rules.RainSurcharge : 0,
        };
    }
}
