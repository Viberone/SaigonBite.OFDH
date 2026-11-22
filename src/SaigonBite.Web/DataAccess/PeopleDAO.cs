using SaigonBite.Web.Domain;

namespace SaigonBite.Web.DataAccess;

/// <summary>Customers and their addresses (tables customer, address). Maps the generalization by customer_type.</summary>
public class CustomerDAO(Db db)
{
    public List<Customer> GetAll()
    {
        using var c = db.Open();
        using var r = Db.Cmd(c, "SELECT * FROM customer ORDER BY customer_id").ExecuteReader();
        var list = new List<Customer>();
        while (r.Read()) list.Add(Map(r, []));
        return list;
    }

    public Customer? GetById(int customerId)
    {
        using var c = db.Open();
        var addresses = new List<Address>();
        using (var a = Db.Cmd(c, "SELECT * FROM address WHERE customer_id = $id ORDER BY address_id", null, ("$id", customerId)).ExecuteReader())
            while (a.Read()) addresses.Add(MapAddress(a));
        using var r = Db.Cmd(c, "SELECT * FROM customer WHERE customer_id = $id", null, ("$id", customerId)).ExecuteReader();
        return r.Read() ? Map(r, addresses) : null;
    }

    private static Customer Map(Microsoft.Data.Sqlite.SqliteDataReader r, List<Address> addresses)
    {
        int id = r.GetInt32(r.GetOrdinal("customer_id"));
        string name = r.GetString(r.GetOrdinal("full_name"));
        string? email = Db.StrOrNull(r, "email");
        return r.GetString(r.GetOrdinal("customer_type")) == "Tourist"
            ? new ForeignTourist
            {
                Id = id, FullName = name, Email = email, Addresses = addresses,
                PassportNo = Db.StrOrNull(r, "passport_no") ?? "", Nationality = Db.StrOrNull(r, "nationality") ?? "",
            }
            : new LocalCustomer { Id = id, FullName = name, Email = email, Addresses = addresses, Phone = Db.StrOrNull(r, "phone") ?? "" };
    }

    private static Address MapAddress(Microsoft.Data.Sqlite.SqliteDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("address_id")), CustomerId = r.GetInt32(r.GetOrdinal("customer_id")),
        Label = r.GetString(r.GetOrdinal("label")), Street = r.GetString(r.GetOrdinal("street")),
        District = r.GetString(r.GetOrdinal("district")), IsHotel = r.GetInt32(r.GetOrdinal("is_hotel")) == 1,
    };
}

/// <summary>Employees (table staff).</summary>
public class StaffDAO(Db db)
{
    public List<Staff> GetAll()
    {
        using var c = db.Open();
        using var r = Db.Cmd(c, "SELECT staff_id, full_name, role, hub_id FROM staff ORDER BY staff_id").ExecuteReader();
        var list = new List<Staff>();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public Staff? GetById(int staffId)
    {
        using var c = db.Open();
        using var r = Db.Cmd(c, "SELECT staff_id, full_name, role, hub_id FROM staff WHERE staff_id = $id", null, ("$id", staffId)).ExecuteReader();
        return r.Read() ? Map(r) : null;
    }

    private static Staff Map(Microsoft.Data.Sqlite.SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0), FullName = r.GetString(1), Role = Enum.Parse<StaffRole>(r.GetString(2)),
        HubId = r.IsDBNull(3) ? null : r.GetInt32(3),
    };
}
