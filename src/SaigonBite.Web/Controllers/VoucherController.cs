using Microsoft.AspNetCore.Mvc;
using SaigonBite.Web.DataAccess;
using SaigonBite.Web.Domain;
using SaigonBite.Web.Infrastructure;

namespace SaigonBite.Web.Controllers;

/// <summary>
/// UC15 Manage Vouchers (author 523C0007): full CRUD of the Voucher entity.
/// Create, read (Index/Details), update (Edit, Activate, Deactivate) and delete (Draft only; others are deactivated).
/// </summary>
[Role("Admin")]
public class VoucherController(VoucherDAO vouchers, ICurrentUser user, IClock clock) : Controller
{
    public IActionResult Index(string? status)
    {
        var today = clock.Today;
        var list = vouchers.GetAll();
        if (Enum.TryParse<VoucherStatus>(status, out var s)) list = list.Where(v => v.StatusOn(today) == s).ToList();
        return View(new VoucherListVM { Vouchers = list, Today = today, Filter = status });
    }

    public IActionResult Details(int id)
    {
        var v = vouchers.GetById(id);
        return v == null ? NotFound() : View(new VoucherEditVM { Voucher = v, Today = clock.Today });
    }

    [HttpGet]
    public IActionResult Create() => View("Edit", new VoucherEditVM
    {
        IsNew = true, Today = clock.Today,
        Voucher = new Voucher { StartDate = clock.Today, EndDate = clock.Today.AddDays(30), UsageLimit = 100, DiscountValue = 10, MaxDiscount = 20000 },
    });

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(Voucher form)
    {
        form.Code = (form.Code ?? "").Trim().ToUpperInvariant();
        form.StoredStatus = VoucherStatus.Draft;
        form.CreatedBy = user.Staff?.Id;
        form.CreatedAt = clock.Now;
        if (form.DiscountType == DiscountType.Fixed) form.MaxDiscount = null;
        var errors = form.Validate();
        if (vouchers.CodeExists(form.Code)) errors.Add($"Code {form.Code} is already used.");
        if (errors.Count > 0) return View("Edit", new VoucherEditVM { Voucher = form, IsNew = true, Today = clock.Today, Errors = errors });
        vouchers.Insert(form);
        TempData["Message"] = $"Voucher {form.Code} is saved as Draft. Activate it when it should go live.";
        return RedirectToAction(nameof(Details), new { id = form.Id });
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var v = vouchers.GetById(id);
        if (v == null) return NotFound();
        if (!v.CanEdit(clock.Today)) { TempData["Error"] = $"{v.Code} is {v.StatusOn(clock.Today)} and can no longer be edited."; return RedirectToAction(nameof(Details), new { id }); }
        return View(new VoucherEditVM { Voucher = v, Today = clock.Today });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Edit(int id, Voucher form)
    {
        var v = vouchers.GetById(id);
        if (v == null) return NotFound();
        if (!v.CanEdit(clock.Today)) return RedirectToAction(nameof(Details), new { id });
        if (v.CanEditTerms)
        {
            v.Code = (form.Code ?? "").Trim().ToUpperInvariant(); v.DiscountType = form.DiscountType; v.DiscountValue = form.DiscountValue;
            v.MaxDiscount = form.DiscountType == DiscountType.Fixed ? null : form.MaxDiscount; v.MinOrderValue = form.MinOrderValue;
            v.StartDate = form.StartDate;
        }
        // an Active voucher keeps its terms; only the description, end date and usage limit may change
        v.Description = form.Description; v.EndDate = form.EndDate; v.UsageLimit = form.UsageLimit;
        var errors = v.Validate();
        if (vouchers.CodeExists(v.Code, v.Id)) errors.Add($"Code {v.Code} is already used.");
        if (errors.Count > 0) return View(new VoucherEditVM { Voucher = v, Today = clock.Today, Errors = errors });
        vouchers.Update(v);
        TempData["Message"] = $"Voucher {v.Code} is updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Activate(int id) => Transition(id, v => v.Activate(clock.Today), "is now Active");

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Deactivate(int id) => Transition(id, v => v.Deactivate(clock.Today), "is deactivated; customers can no longer use it");

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        var v = vouchers.GetById(id);
        if (v == null) return NotFound();
        if (!v.CanDelete || !vouchers.Delete(id))
        {
            TempData["Error"] = $"{v.Code} has been active, so it is kept for the order history. Deactivate it instead.";
            return RedirectToAction(nameof(Details), new { id });
        }
        TempData["Message"] = $"Draft voucher {v.Code} is deleted.";
        return RedirectToAction(nameof(Index));
    }

    private IActionResult Transition(int id, Action<Voucher> change, string done)
    {
        var v = vouchers.GetById(id);
        if (v == null) return NotFound();
        try { change(v); vouchers.Update(v); TempData["Message"] = $"Voucher {v.Code} {done}."; }
        catch (DomainRuleException e) { TempData["Error"] = e.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }
}
