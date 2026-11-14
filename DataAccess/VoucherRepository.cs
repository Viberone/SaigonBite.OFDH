using Microsoft.EntityFrameworkCore;
using SaigonBite.OFDH.Domain;

namespace SaigonBite.OFDH.DataAccess;

public class VoucherRepository
{
    private readonly AppDbContext _db;

    public VoucherRepository(AppDbContext db) => _db = db;

    public List<Voucher> GetAll() =>
        _db.Vouchers.AsNoTracking().OrderBy(v => v.Code).ToList();

    public Voucher? GetById(int id) => _db.Vouchers.Find(id);

    public Voucher? GetByCode(string code) =>
        _db.Vouchers.AsNoTracking().FirstOrDefault(v => v.Code == code);

    public void Add(Voucher voucher)
    {
        _db.Vouchers.Add(voucher);
        _db.SaveChanges();
    }

    public void Update(Voucher voucher)
    {
        _db.Vouchers.Update(voucher);
        _db.SaveChanges();
    }

    public void ExpireOutdated(DateTime today)
    {
        var outdated = _db.Vouchers
            .Where(v => v.Status == VoucherStatus.Active && v.ValidTo < today)
            .ToList();
        foreach (var v in outdated) v.Status = VoucherStatus.Expired;
        if (outdated.Count > 0) _db.SaveChanges();
    }
}