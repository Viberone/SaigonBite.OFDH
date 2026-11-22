using SaigonBite.Web.DataAccess;
using SaigonBite.Web.Domain;

namespace SaigonBite.Web.Integration;

/// <summary>Adapter for the external map / distance service (actor "Map Service"). Risk R4: hidden behind an interface.</summary>
public interface IMapService
{
    /// <summary>Road distance in km from the hub to the district, or null if unknown.</summary>
    double? DistanceKm(int hubId, string district);
    List<string> Districts();
}

/// <summary>Mock map service: a fixed distance table (district_distance), as planned for testing.</summary>
public class MockMapService(Db db) : IMapService
{
    public double? DistanceKm(int hubId, string district)
    {
        using var c = db.Open();
        var v = Db.Cmd(c, "SELECT distance_km FROM district_distance WHERE hub_id = $h AND district = $d", null,
            ("$h", hubId), ("$d", district)).ExecuteScalar();
        return v is null or DBNull ? null : Convert.ToDouble(v);
    }

    public List<string> Districts()
    {
        using var c = db.Open();
        using var r = Db.Cmd(c, "SELECT DISTINCT district FROM district_distance ORDER BY district").ExecuteReader();
        var list = new List<string>();
        while (r.Read()) list.Add(r.GetString(0));
        return list;
    }
}

/// <summary>Adapter for the external payment gateway (actor "Payment Gateway").</summary>
public interface IPaymentGateway
{
    /// <summary>Authorises an online payment; returns the gateway reference, or null if declined.</summary>
    string? Authorise(PaymentMethod method, int amount, string customerRef);
    void Capture(string gatewayRef);
    void Void(string gatewayRef);
}

/// <summary>Sandbox stand-in: approves every amount below 5,000,000 VND (A08: the gateway is simulated).</summary>
public class MockPaymentGateway : IPaymentGateway
{
    private int _next = 50000;
    public string? Authorise(PaymentMethod method, int amount, string customerRef) =>
        amount is > 0 and < 5_000_000 ? $"MOCK-{Interlocked.Increment(ref _next)}" : null;
    public void Capture(string gatewayRef) { }
    public void Void(string gatewayRef) { }
}
