using Microsoft.Data.Sqlite;
using SaigonBite.Web.Domain;

namespace SaigonBite.Web.DataAccess;

/// <summary>Raised when a hub became busy between the quote and the confirmation (the peak-hour race).</summary>
public class HubBusyException(string hubName) : Exception($"{hubName} has just become busy and cannot take new orders.");

/// <summary>Orders, order lines and payments (tables orders, order_line, payment).</summary>
public class OrderDAO(Db db)
{
    private const string Select = """
        SELECT o.*, h.name AS hub_name, c.full_name AS customer_name, v.code AS voucher_code
        FROM orders o
        JOIN hub h ON h.hub_id = o.hub_id
        JOIN customer c ON c.customer_id = o.customer_id
        LEFT JOIN voucher v ON v.voucher_id = o.voucher_id
        """;

    /// <summary>
    /// Saves a new Placed order with its lines and payment, and counts the voucher use, in one transaction.
    /// The hub load is checked again inside the transaction so two customers cannot overfill a hub.
    /// </summary>
    public int InsertPlaced(Order o, Payment p, double busyLoad)
    {
        using var c = db.Open();
        using var tx = c.BeginTransaction(System.Data.IsolationLevel.Serializable);
        var load = Db.Cmd(c, """
            SELECT h.name, h.capacity,
                   (SELECT COUNT(*) FROM orders WHERE hub_id = h.hub_id AND status IN ('Placed','Confirmed','Preparing'))
            FROM hub h WHERE h.hub_id = $hub
            """, tx, ("$hub", o.HubId)).ExecuteReader();
        load.Read();
        string hubName = load.GetString(0);
        double ratio = (double)load.GetInt32(2) / load.GetInt32(1);
        load.Close();
        if (ratio >= busyLoad) throw new HubBusyException(hubName);

        o.Id = Convert.ToInt32(Db.Cmd(c, """
            INSERT INTO orders (customer_id, hub_id, delivery_street, delivery_district, distance_km, status, payment_method,
                                food_subtotal, delivery_fee, quiet_discount, voucher_id, voucher_discount, surcharge, total, placed_at)
            VALUES ($cust, $hub, $street, $district, $km, 'Placed', $method,
                    $food, $fee, $quiet, $voucher, $vdisc, $sur, $total, $placed);
            SELECT last_insert_rowid();
            """, tx,
            ("$cust", o.CustomerId), ("$hub", o.HubId), ("$street", o.DeliveryStreet), ("$district", o.DeliveryDistrict),
            ("$km", o.DistanceKm), ("$method", o.PaymentMethod.ToString()), ("$food", o.FoodSubtotal), ("$fee", o.DeliveryFee),
            ("$quiet", o.QuietDiscount), ("$voucher", o.VoucherId), ("$vdisc", o.VoucherDiscount), ("$sur", o.Surcharge),
            ("$total", o.Total), ("$placed", Db.Ts(o.PlacedAt))).ExecuteScalar());

        foreach (var l in o.Lines)
            Db.Cmd(c, "INSERT INTO order_line (order_id, line_no, item_id, quantity, unit_price) VALUES ($o, $n, $i, $q, $p)", tx,
                ("$o", o.Id), ("$n", l.LineNo), ("$i", l.ItemId), ("$q", l.Quantity), ("$p", l.UnitPrice)).ExecuteNonQuery();

        p.OrderId = o.Id;
        p.Id = Convert.ToInt32(Db.Cmd(c, """
            INSERT INTO payment (order_id, method, amount, status, gateway_ref, updated_at)
            VALUES ($o, $m, $a, $s, $ref, $t); SELECT last_insert_rowid();
            """, tx, ("$o", o.Id), ("$m", p.Method.ToString()), ("$a", p.Amount), ("$s", p.Status.ToString()),
            ("$ref", p.GatewayRef), ("$t", Db.Ts(o.PlacedAt))).ExecuteScalar());

        if (o.VoucherId is int vid)
            Db.Cmd(c, "UPDATE voucher SET used_count = used_count + 1 WHERE voucher_id = $v", tx, ("$v", vid)).ExecuteNonQuery();
        tx.Commit();
        return o.Id;
    }

    /// <summary>Saves a state change of the order (and its payment); gives the voucher use back if the order never went ahead.</summary>
    public void SaveStatus(Order o, Payment? p = null, DateTime? now = null)
    {
        using var c = db.Open();
        using var tx = c.BeginTransaction();
        SaveStatus(c, tx, o, p, now);
        tx.Commit();
    }

    internal static void SaveStatus(SqliteConnection c, SqliteTransaction tx, Order o, Payment? p, DateTime? now)
    {
        int rows = Db.Cmd(c, """
            UPDATE orders SET status = $s, confirmed_at = $conf, ready_at = $ready, closed_at = $closed, note = $note
            WHERE order_id = $id
            """, tx, ("$s", o.Status.ToString()), ("$conf", Db.Ts(o.ConfirmedAt)), ("$ready", Db.Ts(o.ReadyAt)),
            ("$closed", Db.Ts(o.ClosedAt)), ("$note", o.Note), ("$id", o.Id)).ExecuteNonQuery();
        if (rows != 1) throw new InvalidOperationException($"Order #{o.Id} was not found.");
        if (p != null)
            Db.Cmd(c, "UPDATE payment SET status = $s, gateway_ref = $ref, updated_at = $t WHERE payment_id = $id", tx,
                ("$s", p.Status.ToString()), ("$ref", p.GatewayRef), ("$t", Db.Ts(now ?? DateTime.Now)), ("$id", p.Id)).ExecuteNonQuery();
        if (o.VoucherId is int vid && o.Status is OrderStatus.Cancelled or OrderStatus.Rejected)
            Db.Cmd(c, "UPDATE voucher SET used_count = MAX(used_count - 1, 0) WHERE voucher_id = $v", tx, ("$v", vid)).ExecuteNonQuery();
    }

    public Order? GetById(int orderId)
    {
        using var c = db.Open();
        Order? o;
        using (var r = Db.Cmd(c, Select + " WHERE o.order_id = $id", null, ("$id", orderId)).ExecuteReader())
            o = r.Read() ? Map(r) : null;
        if (o == null) return null;
        using var lr = Db.Cmd(c, """
            SELECT l.line_no, l.item_id, m.name_en, l.quantity, l.unit_price
            FROM order_line l JOIN menu_item m ON m.item_id = l.item_id
            WHERE l.order_id = $id ORDER BY l.line_no
            """, null, ("$id", orderId)).ExecuteReader();
        while (lr.Read())
            o.Lines.Add(new OrderLine { LineNo = lr.GetInt32(0), ItemId = lr.GetInt32(1), ItemName = lr.GetString(2), Quantity = lr.GetInt32(3), UnitPrice = lr.GetInt32(4) });
        return o;
    }

    public List<Order> GetByCustomer(int customerId, int limit = 20) =>
        Query(Select + " WHERE o.customer_id = $x ORDER BY o.placed_at DESC LIMIT " + limit, customerId);

    public List<Order> GetForHub(int hubId, params OrderStatus[] statuses)
    {
        string inList = string.Join(",", statuses.Select(s => $"'{s}'"));
        return Query(Select + $" WHERE o.hub_id = $x AND o.status IN ({inList}) ORDER BY o.placed_at", hubId);
    }

    /// <summary>Ready orders of a hub that no shipper has taken yet (UC10 step 1).</summary>
    public List<Order> GetReadyUnassigned(int hubId) =>
        Query(Select + """
             WHERE o.hub_id = $x AND o.status = 'ReadyForPickup'
               AND NOT EXISTS (SELECT 1 FROM delivery d WHERE d.order_id = o.order_id)
             ORDER BY o.ready_at
            """, hubId);

    /// <summary>Each customer may use a voucher once (Client Q&amp;A, Q4); cancelled or rejected orders do not count.</summary>
    public bool CustomerUsedVoucher(int customerId, int voucherId)
    {
        using var c = db.Open();
        return Convert.ToInt32(Db.Cmd(c, """
            SELECT COUNT(*) FROM orders WHERE customer_id = $c AND voucher_id = $v AND status NOT IN ('Cancelled', 'Rejected')
            """, null, ("$c", customerId), ("$v", voucherId)).ExecuteScalar()) > 0;
    }

    public Payment? GetPayment(int orderId)
    {
        using var c = db.Open();
        using var r = Db.Cmd(c, "SELECT payment_id, order_id, method, amount, status, gateway_ref FROM payment WHERE order_id = $o", null, ("$o", orderId)).ExecuteReader();
        if (!r.Read()) return null;
        var p = new Payment { Id = r.GetInt32(0), OrderId = r.GetInt32(1), Method = Enum.Parse<PaymentMethod>(r.GetString(2)), Amount = r.GetInt32(3) };
        return Payment.Restore(p, Enum.Parse<PaymentStatus>(r.GetString(4)), r.IsDBNull(5) ? null : r.GetString(5));
    }

    private List<Order> Query(string sql, int arg)
    {
        using var c = db.Open();
        using var r = Db.Cmd(c, sql, null, ("$x", arg)).ExecuteReader();
        var list = new List<Order>();
        while (r.Read()) list.Add(Map(r));
        r.Close();
        AttachLines(c, list);
        return list;
    }

    /// <summary>Loads the lines of several orders with one query.</summary>
    private static void AttachLines(SqliteConnection c, List<Order> list)
    {
        if (list.Count == 0) return;
        var byId = list.ToDictionary(o => o.Id);
        using var r = Db.Cmd(c, $"""
            SELECT l.order_id, l.line_no, l.item_id, m.name_en, l.quantity, l.unit_price
            FROM order_line l JOIN menu_item m ON m.item_id = l.item_id
            WHERE l.order_id IN ({string.Join(",", byId.Keys)}) ORDER BY l.order_id, l.line_no
            """).ExecuteReader();
        while (r.Read())
            byId[r.GetInt32(0)].Lines.Add(new OrderLine { LineNo = r.GetInt32(1), ItemId = r.GetInt32(2), ItemName = r.GetString(3), Quantity = r.GetInt32(4), UnitPrice = r.GetInt32(5) });
    }

    private static Order Map(SqliteDataReader r)
    {
        var o = new Order
        {
            Id = r.GetInt32(r.GetOrdinal("order_id")), CustomerId = r.GetInt32(r.GetOrdinal("customer_id")),
            HubId = r.GetInt32(r.GetOrdinal("hub_id")), DeliveryStreet = r.GetString(r.GetOrdinal("delivery_street")),
            DeliveryDistrict = r.GetString(r.GetOrdinal("delivery_district")), DistanceKm = r.GetDouble(r.GetOrdinal("distance_km")),
            PaymentMethod = Enum.Parse<PaymentMethod>(r.GetString(r.GetOrdinal("payment_method"))),
            FoodSubtotal = r.GetInt32(r.GetOrdinal("food_subtotal")), DeliveryFee = r.GetInt32(r.GetOrdinal("delivery_fee")),
            QuietDiscount = r.GetInt32(r.GetOrdinal("quiet_discount")), VoucherId = Db.IntOrNull(r, "voucher_id"),
            VoucherDiscount = r.GetInt32(r.GetOrdinal("voucher_discount")), Surcharge = r.GetInt32(r.GetOrdinal("surcharge")),
            Total = r.GetInt32(r.GetOrdinal("total")), PlacedAt = Db.ParseTs(r.GetString(r.GetOrdinal("placed_at"))),
            HubName = r.GetString(r.GetOrdinal("hub_name")), CustomerName = r.GetString(r.GetOrdinal("customer_name")),
            VoucherCode = Db.StrOrNull(r, "voucher_code"),
        };
        return Order.Restore(o, Enum.Parse<OrderStatus>(r.GetString(r.GetOrdinal("status"))),
            Db.ParseTsOrNull(r, "confirmed_at"), Db.ParseTsOrNull(r, "ready_at"), Db.ParseTsOrNull(r, "closed_at"), Db.StrOrNull(r, "note"));
    }
}
