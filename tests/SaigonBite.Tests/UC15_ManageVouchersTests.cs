using Microsoft.AspNetCore.Mvc;
using SaigonBite.Web.Controllers;
using SaigonBite.Web.Domain;
using SaigonBite.Web.Infrastructure;

namespace SaigonBite.Tests;

/// <summary>
/// UC15 Manage Vouchers (author 523C0007). Test cases derived from the post-conditions of each CRUD operation
/// and from the Voucher state machine (Draft -> Active -> Expired | Deactivated; Draft may be deleted).
/// </summary>
public class UC15_ManageVouchersTests : IDisposable
{
    private readonly TestDb t = new();
    public void Dispose() => t.Dispose();

    private VoucherController Admin() => t.WithContext(new VoucherController(t.Vouchers, new FixedCurrentUser(staff: t.Staff.GetById(10)), t.Clock));

    private static Voucher Form(string code = "tet2027", int value = 10, int? max = 30000) => new()
    {
        Code = code, Description = "Tet holiday 10% off", DiscountType = DiscountType.Percent, DiscountValue = value, MaxDiscount = max,
        MinOrderValue = 120000, StartDate = new DateOnly(2026, 11, 20), EndDate = new DateOnly(2027, 2, 28), UsageLimit = 300,
    };

    [Fact(DisplayName = "TC-15-01 Create: a valid voucher is saved as Draft with an upper-case code and no uses")]
    public void CreateSavesDraft()
    {
        var r = Assert.IsType<RedirectToActionResult>(Admin().Create(Form()));
        var v = t.Vouchers.GetById((int)r.RouteValues!["id"]!)!;
        Assert.Equal("TET2027", v.Code);
        Assert.Equal(VoucherStatus.Draft, v.StoredStatus);
        Assert.Equal(0, v.UsedCount);
        Assert.Equal(10, v.CreatedBy);
    }

    [Fact(DisplayName = "TC-15-02 Create: invalid data or a duplicate code is refused and nothing is saved")]
    public void CreateRejectsInvalid()
    {
        long before = t.Scalar("SELECT COUNT(*) FROM voucher");
        var bad = Assert.IsType<VoucherEditVM>(Assert.IsType<ViewResult>(Admin().Create(Form("ab", 150, null))).Model);
        Assert.Contains(bad.Errors, e => e.Contains("4-15 capital letters"));
        Assert.Contains(bad.Errors, e => e.Contains("between 1 and 100"));
        var dup = Assert.IsType<VoucherEditVM>(Assert.IsType<ViewResult>(Admin().Create(Form("WELCOME10"))).Model);
        Assert.Contains(dup.Errors, e => e.Contains("already used"));
        Assert.Equal(before, t.Scalar("SELECT COUNT(*) FROM voucher"));
    }

    [Fact(DisplayName = "TC-15-03 Read: the list shows every voucher with its derived status")]
    public void ListShowsDerivedStatus()
    {
        var vm = Assert.IsType<VoucherListVM>(Assert.IsType<ViewResult>(Admin().Index(null)).Model);
        Assert.Equal(t.Scalar("SELECT COUNT(*) FROM voucher"), vm.Vouchers.Count);
        // OCTOBER5 is stored Active but ended on 31/10/2026, so it shows as Expired
        Assert.Equal(VoucherStatus.Expired, vm.Vouchers.Single(v => v.Code == "OCTOBER5").StatusOn(vm.Today));
        var expired = Assert.IsType<VoucherListVM>(Assert.IsType<ViewResult>(Admin().Index("Expired")).Model);
        Assert.All(expired.Vouchers, v => Assert.Equal(VoucherStatus.Expired, v.StatusOn(vm.Today)));
    }

    [Fact(DisplayName = "TC-15-04 Update: activating a Draft makes it Active and usable on an order")]
    public void ActivateDraft()
    {
        int id = (int)Assert.IsType<RedirectToActionResult>(Admin().Create(Form().StartingToday())).RouteValues!["id"]!;
        Admin().Activate(id);
        var v = t.Vouchers.GetById(id)!;
        Assert.Equal(VoucherStatus.Active, v.StoredStatus);
        Assert.Null(v.CheckUsable(150000, t.Clock.Today, usedBefore: false));
    }

    [Fact(DisplayName = "TC-15-05 Update: an Active voucher keeps its terms; only end date, limit and description change")]
    public void EditActiveKeepsTerms()
    {
        var v = t.Vouchers.GetByCode("SAVE20K")!;
        var form = new Voucher
        {
            Code = "CHANGED", Description = "Extended to December", DiscountType = DiscountType.Percent, DiscountValue = 50,
            MinOrderValue = 0, StartDate = v.StartDate, EndDate = new DateOnly(2026, 12, 31), UsageLimit = 400,
        };
        Admin().Edit(v.Id, form);
        var after = t.Vouchers.GetById(v.Id)!;
        Assert.Equal("SAVE20K", after.Code);
        Assert.Equal(DiscountType.Fixed, after.DiscountType);
        Assert.Equal(20000, after.DiscountValue);
        Assert.Equal(new DateOnly(2026, 12, 31), after.EndDate);
        Assert.Equal(400, after.UsageLimit);
        Assert.Equal("Extended to December", after.Description);
    }

    [Fact(DisplayName = "TC-15-06 Update: a deactivated voucher can no longer be used on an order")]
    public void DeactivateStopsUse()
    {
        var v = t.Vouchers.GetByCode("WELCOME10")!;
        Admin().Deactivate(v.Id);
        var after = t.Vouchers.GetById(v.Id)!;
        Assert.Equal(VoucherStatus.Deactivated, after.StoredStatus);
        Assert.Contains("deactivated", after.CheckUsable(200000, t.Clock.Today, false));
        // a deactivated voucher cannot be activated again
        var c = Admin();
        c.Activate(v.Id);
        Assert.Equal(VoucherStatus.Deactivated, t.Vouchers.GetById(v.Id)!.StoredStatus);
        Assert.Contains("Only a Draft", (string)c.TempData["Error"]!);
    }

    [Fact(DisplayName = "TC-15-07 Delete: an unused Draft is deleted; a voucher that has been active is kept")]
    public void DeleteOnlyDraft()
    {
        var draft = t.Vouchers.GetByCode("LUNCH15")!;
        Admin().Delete(draft.Id);
        Assert.Null(t.Vouchers.GetById(draft.Id));

        var active = t.Vouchers.GetByCode("SAVE20K")!;
        var c = Admin();
        c.Delete(active.Id);
        Assert.NotNull(t.Vouchers.GetById(active.Id));
        Assert.Contains("Deactivate it instead", (string)c.TempData["Error"]!);
    }

    [Fact(DisplayName = "TC-15-08 A voucher at its usage limit is Expired")]
    public void UsageLimitExpires()
    {
        var v = t.NewActiveVoucher("LIMIT1");
        t.Execute($"UPDATE voucher SET used_count = usage_limit WHERE voucher_id = {v.Id}");
        Assert.Equal(VoucherStatus.Expired, t.Vouchers.GetById(v.Id)!.StatusOn(t.Clock.Today));
    }
}

internal static class VoucherFormExtensions
{
    /// <summary>Starts the voucher today so that it is usable as soon as it is activated.</summary>
    public static Voucher StartingToday(this Voucher v) { v.StartDate = DateOnly.FromDateTime(TestDb.Now); return v; }
}
