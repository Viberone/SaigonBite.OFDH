using Microsoft.Data.Sqlite;
using SaigonBite.Web.Domain;

namespace SaigonBite.Web.DataAccess;

/// <summary>Vouchers (table voucher): create, read, update, delete for UC15. Author 523C0007.</summary>
public class VoucherDAO(Db db)
{
    public List<Voucher> GetAll()
    {
        using var c = db.Open();
        using var r = Db.Cmd(c, "SELECT * FROM voucher ORDER BY start_date DESC, code").ExecuteReader();
        var list = new List<Voucher>();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public Voucher? GetById(int id) => QueryOne("voucher_id = $x", id);
    public Voucher? GetByCode(string code) => QueryOne("code = $x", code.Trim().ToUpperInvariant());

    public bool CodeExists(string code, int exceptId = 0)
    {
        using var c = db.Open();
        return Convert.ToInt32(Db.Cmd(c, "SELECT COUNT(*) FROM voucher WHERE code = $c AND voucher_id <> $id", null,
            ("$c", code), ("$id", exceptId)).ExecuteScalar()) > 0;
    }

    public int Insert(Voucher v)
    {
        using var c = db.Open();
        v.Id = Convert.ToInt32(Db.Cmd(c, """
            INSERT INTO voucher (code, description, discount_type, discount_value, max_discount, min_order_value,
                                 start_date, end_date, usage_limit, used_count, status, created_by, created_at)
            VALUES ($code, $desc, $type, $value, $max, $min, $start, $end, $limit, 0, $status, $by, $at);
            SELECT last_insert_rowid();
            """, null, Params(v).Append(("$by", v.CreatedBy)).Append(("$at", Db.Ts(v.CreatedAt))).ToArray()).ExecuteScalar());
        return v.Id;
    }

    public void Update(Voucher v)
    {
        using var c = db.Open();
        Db.Cmd(c, """
            UPDATE voucher SET code = $code, description = $desc, discount_type = $type, discount_value = $value,
                   max_discount = $max, min_order_value = $min, start_date = $start, end_date = $end,
                   usage_limit = $limit, status = $status
            WHERE voucher_id = $id
            """, null, Params(v).Append(("$id", v.Id)).ToArray()).ExecuteNonQuery();
    }

    /// <summary>Hard delete, allowed only for an unused Draft (checked again in SQL).</summary>
    public bool Delete(int id)
    {
        using var c = db.Open();
        return Db.Cmd(c, "DELETE FROM voucher WHERE voucher_id = $id AND status = 'Draft' AND used_count = 0", null, ("$id", id)).ExecuteNonQuery() == 1;
    }

    private static IEnumerable<(string, object?)> Params(Voucher v) =>
    [
        ("$code", v.Code), ("$desc", v.Description), ("$type", v.DiscountType.ToString()), ("$value", v.DiscountValue),
        ("$max", v.MaxDiscount), ("$min", v.MinOrderValue), ("$start", v.StartDate.ToString("yyyy-MM-dd")),
        ("$end", v.EndDate.ToString("yyyy-MM-dd")), ("$limit", v.UsageLimit), ("$status", v.StoredStatus.ToString()),
    ];

    private Voucher? QueryOne(string where, object arg)
    {
        using var c = db.Open();
        using var r = Db.Cmd(c, "SELECT * FROM voucher WHERE " + where, null, ("$x", arg)).ExecuteReader();
        return r.Read() ? Map(r) : null;
    }

    private static Voucher Map(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("voucher_id")), Code = r.GetString(r.GetOrdinal("code")),
        Description = r.GetString(r.GetOrdinal("description")),
        DiscountType = Enum.Parse<DiscountType>(r.GetString(r.GetOrdinal("discount_type"))),
        DiscountValue = r.GetInt32(r.GetOrdinal("discount_value")), MaxDiscount = Db.IntOrNull(r, "max_discount"),
        MinOrderValue = r.GetInt32(r.GetOrdinal("min_order_value")),
        StartDate = DateOnly.Parse(r.GetString(r.GetOrdinal("start_date"))), EndDate = DateOnly.Parse(r.GetString(r.GetOrdinal("end_date"))),
        UsageLimit = r.GetInt32(r.GetOrdinal("usage_limit")), UsedCount = r.GetInt32(r.GetOrdinal("used_count")),
        StoredStatus = Enum.Parse<VoucherStatus>(r.GetString(r.GetOrdinal("status"))), CreatedBy = Db.IntOrNull(r, "created_by"),
        CreatedAt = Db.ParseTs(r.GetString(r.GetOrdinal("created_at"))),
    };
}
