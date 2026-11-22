using SaigonBite.Web.Domain;

namespace SaigonBite.Web.DataAccess;

/// <summary>Reads business rules (table business_rule).</summary>
public class RuleDAO(Db db)
{
    public BusinessRules Load()
    {
        using var c = db.Open();
        using var r = Db.Cmd(c, "SELECT rule_key, rule_value FROM business_rule").ExecuteReader();
        var values = new Dictionary<string, double>();
        while (r.Read()) values[r.GetString(0)] = r.GetDouble(1);
        return BusinessRules.FromDictionary(values);
    }
}

/// <summary>Hubs with their current load (tables hub, orders).</summary>
public class HubDAO(Db db)
{
    private const string Select = """
        SELECT h.hub_id, h.name, h.district, h.street, h.capacity, h.rain_surcharge_on,
               (SELECT COUNT(*) FROM orders o WHERE o.hub_id = h.hub_id
                  AND o.status IN ('Placed', 'Confirmed', 'Preparing')) AS active_orders
        FROM hub h WHERE h.is_active = 1
        """;

    public List<Hub> GetAll()
    {
        using var c = db.Open();
        using var r = Db.Cmd(c, Select + " ORDER BY h.hub_id").ExecuteReader();
        var list = new List<Hub>();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public Hub? GetById(int hubId)
    {
        using var c = db.Open();
        using var r = Db.Cmd(c, Select + " AND h.hub_id = $id", null, ("$id", hubId)).ExecuteReader();
        return r.Read() ? Map(r) : null;
    }

    public void SetRainSurcharge(int hubId, bool on)
    {
        using var c = db.Open();
        Db.Cmd(c, "UPDATE hub SET rain_surcharge_on = $on WHERE hub_id = $id", null, ("$on", on ? 1 : 0), ("$id", hubId)).ExecuteNonQuery();
    }

    private static Hub Map(Microsoft.Data.Sqlite.SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0), Name = r.GetString(1), District = r.GetString(2), Street = r.GetString(3),
        Capacity = r.GetInt32(4), RainSurchargeOn = r.GetInt32(5) == 1, ActiveOrders = r.GetInt32(6),
    };
}

/// <summary>Menu items offered by a hub (tables menu_item, brand, hub_menu_item).</summary>
public class MenuItemDAO(Db db)
{
    public List<MenuItem> GetMenu(int hubId)
    {
        using var c = db.Open();
        using var r = Db.Cmd(c, """
            SELECT m.item_id, m.brand_id, b.name, m.name_vi, m.name_en, m.price, hm.is_available
            FROM hub_menu_item hm
            JOIN menu_item m ON m.item_id = hm.item_id
            JOIN brand b ON b.brand_id = m.brand_id
            WHERE hm.hub_id = $hub AND m.is_active = 1
            ORDER BY b.name, m.price
            """, null, ("$hub", hubId)).ExecuteReader();
        var list = new List<MenuItem>();
        while (r.Read())
            list.Add(new MenuItem
            {
                Id = r.GetInt32(0), BrandId = r.GetInt32(1), BrandName = r.GetString(2), NameVi = r.GetString(3),
                NameEn = r.GetString(4), Price = r.GetInt32(5), IsAvailable = r.GetInt32(6) == 1,
            });
        return list;
    }

    /// <summary>Supporting UC09: kitchen staff mark an item available or sold out at their hub.</summary>
    public void SetAvailability(int hubId, int itemId, bool available)
    {
        using var c = db.Open();
        Db.Cmd(c, "UPDATE hub_menu_item SET is_available = $a WHERE hub_id = $h AND item_id = $i", null,
            ("$a", available ? 1 : 0), ("$h", hubId), ("$i", itemId)).ExecuteNonQuery();
    }
}
