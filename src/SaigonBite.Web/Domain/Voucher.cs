using System.Text.RegularExpressions;

namespace SaigonBite.Web.Domain;

/// <summary>
/// Discount code managed by the admin (UC15 Manage Vouchers, author 523C0007).
/// State machine: Draft -> Active -> (Expired | Deactivated); a Draft may also be deleted.
/// Expired is derived: an Active voucher past its end date or at its usage limit.
/// </summary>
public partial class Voucher
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public DiscountType DiscountType { get; set; } = DiscountType.Percent;
    public int DiscountValue { get; set; }
    public int? MaxDiscount { get; set; }
    public int MinOrderValue { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int UsageLimit { get; set; }
    public int UsedCount { get; set; }
    /// <summary>Stored status: Draft, Active or Deactivated.</summary>
    public VoucherStatus StoredStatus { get; set; } = VoucherStatus.Draft;
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public VoucherStatus StatusOn(DateOnly today) =>
        StoredStatus == VoucherStatus.Active && (today > EndDate || UsedCount >= UsageLimit)
            ? VoucherStatus.Expired
            : StoredStatus;

    // ---------- state transitions (guards) ----------
    public void Activate(DateOnly today)
    {
        if (StoredStatus != VoucherStatus.Draft) throw new DomainRuleException("Only a Draft voucher can be activated.");
        if (EndDate < today) throw new DomainRuleException("The end date has already passed.");
        StoredStatus = VoucherStatus.Active;
    }

    public void Deactivate(DateOnly today)
    {
        var s = StatusOn(today);
        if (s is not (VoucherStatus.Active or VoucherStatus.Expired))
            throw new DomainRuleException("Only an Active or Expired voucher can be deactivated.");
        StoredStatus = VoucherStatus.Deactivated;
    }

    /// <summary>Hard delete is allowed only before anyone could use the voucher.</summary>
    public bool CanDelete => StoredStatus == VoucherStatus.Draft && UsedCount == 0;
    /// <summary>A Draft can be edited fully; an Active voucher only its end date, usage limit and description.</summary>
    public bool CanEditTerms => StoredStatus == VoucherStatus.Draft;
    public bool CanEdit(DateOnly today) => StatusOn(today) is VoucherStatus.Draft or VoucherStatus.Active;

    // ---------- validation of the data the admin enters ----------
    [GeneratedRegex("^[A-Z0-9]{4,15}$")]
    private static partial Regex CodePattern();

    public List<string> Validate()
    {
        var errors = new List<string>();
        if (!CodePattern().IsMatch(Code ?? "")) errors.Add("Code must be 4-15 capital letters or digits.");
        if (string.IsNullOrWhiteSpace(Description)) errors.Add("Description is required.");
        if (DiscountType == DiscountType.Percent && (DiscountValue < 1 || DiscountValue > 100)) errors.Add("A percentage must be between 1 and 100.");
        if (DiscountType == DiscountType.Fixed && DiscountValue < 1000) errors.Add("A fixed discount must be at least 1,000 VND.");
        if (DiscountType == DiscountType.Percent && MaxDiscount is null) errors.Add("A percentage voucher needs a maximum discount.");
        if (MaxDiscount is < 0) errors.Add("Maximum discount cannot be negative.");
        if (MinOrderValue < 0) errors.Add("Minimum order value cannot be negative.");
        if (EndDate < StartDate) errors.Add("End date must be on or after the start date.");
        if (UsageLimit < 1) errors.Add("Usage limit must be at least 1.");
        if (UsageLimit < UsedCount) errors.Add($"Usage limit cannot be below the {UsedCount} uses so far.");
        return errors;
    }

    // ---------- use on an order (UC03) ----------
    /// <summary>Returns null when the voucher can be used, otherwise the reason it cannot.</summary>
    public string? CheckUsable(int foodSubtotal, DateOnly today, bool usedBefore)
    {
        var s = StatusOn(today);
        if (s != VoucherStatus.Active) return $"Voucher {Code} is {s.ToString().ToLowerInvariant()}.";
        if (today < StartDate) return $"Voucher {Code} starts on {StartDate:dd/MM/yyyy}.";
        if (usedBefore) return $"You have already used voucher {Code}.";
        if (foodSubtotal < MinOrderValue) return $"Voucher {Code} needs a food subtotal of at least {MinOrderValue:N0} VND.";
        return null;
    }

    /// <summary>Discount on the food subtotal only (A06), never more than the subtotal.</summary>
    public int DiscountFor(int foodSubtotal)
    {
        int d = DiscountType == DiscountType.Percent ? foodSubtotal * DiscountValue / 100 : DiscountValue;
        if (MaxDiscount is int cap) d = Math.Min(d, cap);
        return Math.Min(d, foodSubtotal);
    }

    public string DiscountText => DiscountType == DiscountType.Percent
        ? $"{DiscountValue}% (max {MaxDiscount:N0} VND)"
        : $"{DiscountValue:N0} VND";
}
