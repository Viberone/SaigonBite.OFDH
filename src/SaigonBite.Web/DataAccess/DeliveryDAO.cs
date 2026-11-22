using Microsoft.Data.Sqlite;
using SaigonBite.Web.Domain;

namespace SaigonBite.Web.DataAccess;

/// <summary>Deliveries (table delivery). Author 523C0001.</summary>
public class DeliveryDAO(Db db)
{
    /// <summary>Saves a new Assigned delivery. Returns false if another shipper took the order first (UNIQUE order_id).</summary>
    public bool TryInsert(Delivery d)
    {
        using var c = db.Open();
        try
        {
            d.Id = Convert.ToInt32(Db.Cmd(c, """
                INSERT INTO delivery (order_id, shipper_id, status, assigned_at) VALUES ($o, $s, 'Assigned', $t);
                SELECT last_insert_rowid();
                """, null, ("$o", d.OrderId), ("$s", d.ShipperId), ("$t", Db.Ts(d.AssignedAt))).ExecuteScalar());
            return true;
        }
        catch (SqliteException e) when (e.SqliteErrorCode == 19) // constraint: order already has a delivery
        {
            return false;
        }
    }

    /// <summary>Saves the delivery, its order and the payment together, so the three never disagree.</summary>
    public void SaveProgress(Delivery d, Order o, Payment? p, DateTime now)
    {
        using var c = db.Open();
        using var tx = c.BeginTransaction();
        Db.Cmd(c, """
            UPDATE delivery SET status = $s, picked_up_at = $p, finished_at = $f, cash_collected = $cash, failure_reason = $why
            WHERE delivery_id = $id
            """, tx, ("$s", d.Status.ToString()), ("$p", Db.Ts(d.PickedUpAt)), ("$f", Db.Ts(d.FinishedAt)),
            ("$cash", d.CashCollected), ("$why", d.FailureReason), ("$id", d.Id)).ExecuteNonQuery();
        OrderDAO.SaveStatus(c, tx, o, p, now);
        tx.Commit();
    }

    public Delivery? GetById(int deliveryId) => QueryOne("WHERE delivery_id = $x", deliveryId);
    public Delivery? GetByOrder(int orderId) => QueryOne("WHERE order_id = $x", orderId);

    /// <summary>The shipper's deliveries that are not finished yet (Assigned or PickedUp).</summary>
    public List<Delivery> GetOpenForShipper(int shipperId)
    {
        using var c = db.Open();
        using var r = Db.Cmd(c, "SELECT * FROM delivery WHERE shipper_id = $x AND status IN ('Assigned','PickedUp') ORDER BY assigned_at", null, ("$x", shipperId)).ExecuteReader();
        var list = new List<Delivery>();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    /// <summary>Today's finished deliveries of a shipper, with the cash to hand in at the end of the shift.</summary>
    public (int Delivered, int Failed, int Cash) ShiftSummary(int shipperId, DateOnly day)
    {
        using var c = db.Open();
        using var r = Db.Cmd(c, """
            SELECT SUM(status = 'Delivered'), SUM(status = 'Failed'), COALESCE(SUM(cash_collected), 0)
            FROM delivery WHERE shipper_id = $s AND substr(finished_at, 1, 10) = $d
            """, null, ("$s", shipperId), ("$d", day.ToString("yyyy-MM-dd"))).ExecuteReader();
        r.Read();
        return (r.IsDBNull(0) ? 0 : r.GetInt32(0), r.IsDBNull(1) ? 0 : r.GetInt32(1), r.GetInt32(2));
    }

    private Delivery? QueryOne(string where, int arg)
    {
        using var c = db.Open();
        using var r = Db.Cmd(c, "SELECT * FROM delivery " + where, null, ("$x", arg)).ExecuteReader();
        return r.Read() ? Map(r) : null;
    }

    private static Delivery Map(SqliteDataReader r)
    {
        var d = new Delivery
        {
            Id = r.GetInt32(r.GetOrdinal("delivery_id")), OrderId = r.GetInt32(r.GetOrdinal("order_id")),
            ShipperId = r.GetInt32(r.GetOrdinal("shipper_id")), AssignedAt = Db.ParseTs(r.GetString(r.GetOrdinal("assigned_at"))),
        };
        return Delivery.Restore(d, Enum.Parse<DeliveryStatus>(r.GetString(r.GetOrdinal("status"))),
            Db.ParseTsOrNull(r, "picked_up_at"), Db.ParseTsOrNull(r, "finished_at"), Db.IntOrNull(r, "cash_collected"), Db.StrOrNull(r, "failure_reason"));
    }
}
