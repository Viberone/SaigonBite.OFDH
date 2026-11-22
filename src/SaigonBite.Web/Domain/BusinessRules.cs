namespace SaigonBite.Web.Domain;

/// <summary>
/// Business rule values confirmed at the Client Q&amp;A (Assumption Log A02-A05).
/// Loaded from the business_rule table so the client can change them without a code change (risk R1).
/// </summary>
public class BusinessRules
{
    public double BusyLoad { get; init; } = 1.0;
    public double QuietLoad { get; init; } = 0.3;
    public double QuietRate { get; init; } = 0.10;
    public int QuietMax { get; init; } = 20000;
    public int BaseFee { get; init; } = 15000;
    public int BaseKm { get; init; } = 2;
    public int PerKmFee { get; init; } = 5000;
    public double MaxKm { get; init; } = 8;
    public int RainSurcharge { get; init; } = 10000;

    public static BusinessRules FromDictionary(IReadOnlyDictionary<string, double> v) => new()
    {
        BusyLoad = v.GetValueOrDefault("BusyLoad", 1.0),
        QuietLoad = v.GetValueOrDefault("QuietLoad", 0.3),
        QuietRate = v.GetValueOrDefault("QuietRate", 0.10),
        QuietMax = (int)v.GetValueOrDefault("QuietMax", 20000),
        BaseFee = (int)v.GetValueOrDefault("BaseFee", 15000),
        BaseKm = (int)v.GetValueOrDefault("BaseKm", 2),
        PerKmFee = (int)v.GetValueOrDefault("PerKmFee", 5000),
        MaxKm = v.GetValueOrDefault("MaxKm", 8),
        RainSurcharge = (int)v.GetValueOrDefault("RainSurcharge", 10000),
    };
}
