namespace SaigonBite.Web.Domain;

/// <summary>A cloud kitchen. Its load decides the busy-kitchen rule and the quiet-kitchen incentive.</summary>
public class Hub
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string District { get; init; } = "";
    public string Street { get; init; } = "";
    public int Capacity { get; init; }
    public bool RainSurchargeOn { get; init; }
    /// <summary>Orders that are Placed, Confirmed or Preparing at this hub.</summary>
    public int ActiveOrders { get; init; }

    public double Load => Capacity == 0 ? 1 : (double)ActiveOrders / Capacity;
    public bool IsBusy(BusinessRules r) => Load >= r.BusyLoad;
    public bool IsQuiet(BusinessRules r) => Load < r.QuietLoad;

    public string LoadLabel(BusinessRules r) => IsBusy(r) ? "Busy" : IsQuiet(r) ? "Quiet" : "Normal";
}

public class Brand
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Cuisine { get; init; } = "";
}

/// <summary>A menu item as offered by one hub (MenuItem joined with the HubMenuItem association class).</summary>
public class MenuItem
{
    public int Id { get; init; }
    public int BrandId { get; init; }
    public string BrandName { get; init; } = "";
    public string NameVi { get; init; } = "";
    public string NameEn { get; init; } = "";
    public int Price { get; init; }
    public bool IsAvailable { get; init; }
}
