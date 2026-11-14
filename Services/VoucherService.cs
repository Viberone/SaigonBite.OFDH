using SaigonBite.OFDH.DataAccess;
using SaigonBite.OFDH.Domain;

namespace SaigonBite.OFDH.Services;

public class VoucherService
{
    private readonly VoucherRepository _repo;
    public VoucherService(VoucherRepository repo) => _repo = repo;

    public List<Voucher> List()
    {
        _repo.ExpireOutdated(DateTime.Today);   // Active -> Expired when ValidTo has passed
        return _repo.GetAll();
    }

    public Voucher? Find(int id) => _repo.GetById(id);

    public void Create(Voucher v)
    {
        Normalize(v);
        Validate(v);
        if (_repo.GetByCode(v.Code) != null)
            throw new ArgumentException("This voucher code already exists.");

        v.Status = VoucherStatus.Draft;
        v.UsedCount = 0;
        _repo.Add(v);
    }

    public void Edit(Voucher input)
    {
        var v = _repo.GetById(input.VoucherId)
                ?? throw new ArgumentException("Voucher not found.");
        if (v.Status != VoucherStatus.Draft)
            throw new InvalidOperationException("Only a Draft voucher can be edited.");

        Normalize(input);
        Validate(input);

        var other = _repo.GetByCode(input.Code);
        if (other != null && other.VoucherId != v.VoucherId)
            throw new ArgumentException("Another voucher already uses this code.");

        v.Code = input.Code;
        v.Type = input.Type;
        v.Value = input.Value;
        v.MaxDiscount = input.MaxDiscount;
        v.MinOrderValue = input.MinOrderValue;
        v.ValidFrom = input.ValidFrom;
        v.ValidTo = input.ValidTo;
        v.UsageLimit = input.UsageLimit;
        _repo.Update(v);
    }

    public void Activate(int id)
    {
        var v = _repo.GetById(id) ?? throw new ArgumentException("Voucher not found.");
        if (v.Status != VoucherStatus.Draft)
            throw new InvalidOperationException("Only a Draft voucher can be activated.");
        if (v.ValidTo < DateTime.Today)
            throw new ArgumentException("Cannot activate: the end date has already passed.");
        v.Status = VoucherStatus.Active;
        _repo.Update(v);
    }

    public void Deactivate(int id)
    {
        var v = _repo.GetById(id) ?? throw new ArgumentException("Voucher not found.");
        if (v.Status is VoucherStatus.Expired or VoucherStatus.Deactivated)
            throw new InvalidOperationException("This voucher is already closed.");
        v.Status = VoucherStatus.Deactivated;
        _repo.Update(v);
    }

    private static void Normalize(Voucher v)
    {
        v.Code = (v.Code ?? "").Trim().ToUpperInvariant();
        if (v.Type == VoucherType.Fixed) v.MaxDiscount = null;
    }

    private static void Validate(Voucher v)
    {
        if (string.IsNullOrWhiteSpace(v.Code))
            throw new ArgumentException("Code is required.");
        if (v.Value <= 0)
            throw new ArgumentException("Value must be greater than 0.");
        if (v.Type == VoucherType.Percentage && v.Value > 100)
            throw new ArgumentException("A percentage cannot exceed 100.");
        if (v.MaxDiscount.HasValue && v.MaxDiscount.Value <= 0)
            throw new ArgumentException("Maximum discount must be greater than 0.");
        if (v.MinOrderValue < 0)
            throw new ArgumentException("Minimum order value cannot be negative.");
        if (v.UsageLimit <= 0)
            throw new ArgumentException("Usage limit must be at least 1.");
        if (v.ValidTo < v.ValidFrom)
            throw new ArgumentException("End date must not be before start date.");
    }
}