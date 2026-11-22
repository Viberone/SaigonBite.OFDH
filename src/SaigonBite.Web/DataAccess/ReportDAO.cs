using SaigonBite.Web.Domain;

namespace SaigonBite.Web.DataAccess;

/// <summary>Read-only aggregate queries for UC16 Produce Management Reports. Author 523C0001.</summary>
public class ReportDAO(Db db)
{
    private const string InRange = "substr(o.placed_at, 1, 10) BETWEEN $from AND $to";
    private const string HubFilter = "($hub IS NULL OR h.hub_id = $hub)";
    /// <summary>Lunch 11:00-13:59 and dinner 17:00-20:59.</summary>
    private const string IsPeak = "(CAST(substr(o.placed_at, 12, 2) AS INTEGER) BETWEEN 11 AND 13 OR CAST(substr(o.placed_at, 12, 2) AS INTEGER) BETWEEN 17 AND 20)";

    public ManagementReport Build(DateOnly from, DateOnly to, int? hubId) => new()
    {
        From = from, To = to, HubId = hubId,
        Sales = Sales(from, to, hubId), Workload = Workload(from, to, hubId),
        Delivery = DeliveryPerformance(from, to, hubId), TopItems = TopItems(from, to, hubId),
    };

    public List<SalesRow> Sales(DateOnly from, DateOnly to, int? hubId) => Rows($"""
        SELECT h.name, COUNT(o.order_id), COALESCE(SUM(o.food_subtotal), 0), COALESCE(SUM(o.delivery_fee), 0),
               COALESCE(SUM(o.quiet_discount + o.voucher_discount), 0), COALESCE(SUM(o.surcharge), 0), COALESCE(SUM(o.total), 0)
        FROM hub h
        LEFT JOIN orders o ON o.hub_id = h.hub_id AND o.status = 'Delivered' AND {InRange}
        WHERE {HubFilter}
        GROUP BY h.hub_id ORDER BY h.hub_id
        """, from, to, hubId, r => new SalesRow
    {
        HubName = r.GetString(0), DeliveredOrders = r.GetInt32(1), FoodSales = r.GetInt64(2), DeliveryFees = r.GetInt64(3),
        Discounts = r.GetInt64(4), Surcharges = r.GetInt64(5), Revenue = r.GetInt64(6),
    });

    public List<WorkloadRow> Workload(DateOnly from, DateOnly to, int? hubId) => Rows($"""
        SELECT h.name, h.capacity, COUNT(o.order_id),
               COALESCE(SUM({IsPeak}), 0), COALESCE(SUM(o.status = 'Rejected'), 0), COALESCE(SUM(o.status = 'Cancelled'), 0),
               COALESCE((SELECT MAX(n) FROM (SELECT COUNT(*) AS n FROM orders o WHERE o.hub_id = h.hub_id AND {InRange}
                                             GROUP BY substr(o.placed_at, 1, 10))), 0)
        FROM hub h
        LEFT JOIN orders o ON o.hub_id = h.hub_id AND {InRange}
        WHERE {HubFilter}
        GROUP BY h.hub_id ORDER BY h.hub_id
        """, from, to, hubId, r => new WorkloadRow
    {
        HubName = r.GetString(0), Capacity = r.GetInt32(1), OrdersPlaced = r.GetInt32(2), PeakOrders = r.GetInt32(3),
        Rejected = r.GetInt32(4), Cancelled = r.GetInt32(5), BusiestDayOrders = r.GetInt32(6),
    });

    public List<DeliveryRow> DeliveryPerformance(DateOnly from, DateOnly to, int? hubId) => Rows($"""
        SELECT h.name,
               COALESCE(SUM(o.status = 'Delivered'), 0),
               COALESCE(SUM(o.status = 'DeliveryFailed'), 0),
               COALESCE(AVG(CASE WHEN o.status = 'Delivered' THEN (julianday(o.closed_at) - julianday(o.placed_at)) * 1440 END), 0),
               COALESCE(SUM(o.status = 'Delivered' AND (julianday(o.closed_at) - julianday(o.placed_at)) * 1440 <= 40), 0),
               COALESCE(AVG((julianday(d.picked_up_at) - julianday(o.ready_at)) * 1440), 0)
        FROM hub h
        LEFT JOIN orders o ON o.hub_id = h.hub_id AND o.status IN ('Delivered', 'DeliveryFailed') AND {InRange}
        LEFT JOIN delivery d ON d.order_id = o.order_id
        WHERE {HubFilter}
        GROUP BY h.hub_id ORDER BY h.hub_id
        """, from, to, hubId, r => new DeliveryRow
    {
        HubName = r.GetString(0), Delivered = r.GetInt32(1), Failed = r.GetInt32(2),
        AvgMinutesToDoor = r.GetDouble(3), WithinTarget = r.GetInt32(4), AvgPickupWait = r.GetDouble(5),
    });

    public List<TopItemRow> TopItems(DateOnly from, DateOnly to, int? hubId, int top = 5) => Rows($"""
        SELECT m.name_en, b.name, SUM(l.quantity), SUM(l.quantity * l.unit_price)
        FROM order_line l
        JOIN orders o ON o.order_id = l.order_id AND o.status = 'Delivered' AND {InRange}
        JOIN hub h ON h.hub_id = o.hub_id
        JOIN menu_item m ON m.item_id = l.item_id
        JOIN brand b ON b.brand_id = m.brand_id
        WHERE {HubFilter}
        GROUP BY m.item_id ORDER BY SUM(l.quantity) DESC, m.name_en LIMIT {top}
        """, from, to, hubId, r => new TopItemRow
    {
        ItemName = r.GetString(0), BrandName = r.GetString(1), Quantity = r.GetInt32(2), Sales = r.GetInt64(3),
    });

    private List<T> Rows<T>(string sql, DateOnly from, DateOnly to, int? hubId, Func<Microsoft.Data.Sqlite.SqliteDataReader, T> map)
    {
        using var c = db.Open();
        using var r = Db.Cmd(c, sql, null, ("$from", from.ToString("yyyy-MM-dd")), ("$to", to.ToString("yyyy-MM-dd")), ("$hub", hubId)).ExecuteReader();
        var list = new List<T>();
        while (r.Read()) list.Add(map(r));
        return list;
    }
}
