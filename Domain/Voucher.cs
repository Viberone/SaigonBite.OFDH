namespace SaigonBite.OFDH.Domain;

public enum VoucherType { Fixed, Percentage }
public enum VoucherStatus { Draft, Active, Expired, Deactivated }

public class Voucher
{
    public int VoucherId { get; set; }
    public string Code { get; set; } = "";
    public VoucherType Type { get; set; } = VoucherType.Fixed;
    public decimal Value { get; set; }            // VND if Fixed, 1-100 if Percentage
    public decimal? MaxDiscount { get; set; }     // cap, only for Percentage
    public decimal MinOrderValue { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public int UsageLimit { get; set; }
    public int UsedCount { get; set; }            // increased later by UC03
    public VoucherStatus Status { get; set; } = VoucherStatus.Draft;
}