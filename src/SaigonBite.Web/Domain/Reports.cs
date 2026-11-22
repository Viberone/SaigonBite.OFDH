namespace SaigonBite.Web.Domain;

/// <summary>Management report (UC16 Produce Management Reports, author 523C0001). Read-only, so no state machine.</summary>
public class ManagementReport
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public int? HubId { get; init; }
    public List<SalesRow> Sales { get; init; } = [];
    public List<WorkloadRow> Workload { get; init; } = [];
    public List<DeliveryRow> Delivery { get; init; } = [];
    public List<TopItemRow> TopItems { get; init; } = [];

    public SalesRow SalesTotal => new()
    {
        HubName = "All hubs",
        DeliveredOrders = Sales.Sum(s => s.DeliveredOrders),
        FoodSales = Sales.Sum(s => s.FoodSales),
        DeliveryFees = Sales.Sum(s => s.DeliveryFees),
        Discounts = Sales.Sum(s => s.Discounts),
        Surcharges = Sales.Sum(s => s.Surcharges),
        Revenue = Sales.Sum(s => s.Revenue),
    };
}

public class SalesRow
{
    public string HubName { get; init; } = "";
    public int DeliveredOrders { get; init; }
    public long FoodSales { get; init; }
    public long DeliveryFees { get; init; }
    public long Discounts { get; init; }
    public long Surcharges { get; init; }
    public long Revenue { get; init; }
    public long AverageOrder => DeliveredOrders == 0 ? 0 : Revenue / DeliveredOrders;
}

public class WorkloadRow
{
    public string HubName { get; init; } = "";
    public int Capacity { get; init; }
    public int OrdersPlaced { get; init; }
    public int PeakOrders { get; init; }        // placed 11:00-13:59 or 17:00-20:59
    public int Rejected { get; init; }
    public int Cancelled { get; init; }
    public int BusiestDayOrders { get; init; }
    public double PeakShare => OrdersPlaced == 0 ? 0 : (double)PeakOrders / OrdersPlaced;
    public double RejectRate => OrdersPlaced == 0 ? 0 : (double)Rejected / OrdersPlaced;
}

public class DeliveryRow
{
    public string HubName { get; init; } = "";
    public int Delivered { get; init; }
    public int Failed { get; init; }
    public double AvgMinutesToDoor { get; init; }
    public int WithinTarget { get; init; }      // delivered within the 40-minute target (B1)
    public double AvgPickupWait { get; init; }  // ready -> picked up, minutes
    public double OnTimeRate => Delivered == 0 ? 0 : (double)WithinTarget / Delivered;
    public double FailRate => Delivered + Failed == 0 ? 0 : (double)Failed / (Delivered + Failed);
}

public class TopItemRow
{
    public string ItemName { get; init; } = "";
    public string BrandName { get; init; } = "";
    public int Quantity { get; init; }
    public long Sales { get; init; }
}
