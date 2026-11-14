using Microsoft.AspNetCore.Mvc;
using SaigonBite.OFDH.Domain;
using SaigonBite.OFDH.Services;

namespace SaigonBite.OFDH.Controllers;

public class VoucherController : Controller
{
    private readonly VoucherService _service;
    public VoucherController(VoucherService service) => _service = service;

    public IActionResult Index() => View(_service.List());

    [HttpGet]
    public IActionResult Create() =>
        View(new Voucher { ValidFrom = DateTime.Today, ValidTo = DateTime.Today.AddMonths(1), UsageLimit = 100 });

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create([Bind("Code,Type,Value,MaxDiscount,MinOrderValue,ValidFrom,ValidTo,UsageLimit")] Voucher v)
    {
        if (!ModelState.IsValid) return View(v);
        try { _service.Create(v); return RedirectToAction(nameof(Index)); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { ModelState.AddModelError("", ex.Message); return View(v); }
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var v = _service.Find(id);
        return v == null ? NotFound() : View(v);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Edit([Bind("VoucherId,Code,Type,Value,MaxDiscount,MinOrderValue,ValidFrom,ValidTo,UsageLimit")] Voucher v)
    {
        if (!ModelState.IsValid) return View(v);
        try { _service.Edit(v); return RedirectToAction(nameof(Index)); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { ModelState.AddModelError("", ex.Message); return View(v); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Activate(int id) => ChangeState(() => _service.Activate(id));

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Deactivate(int id) => ChangeState(() => _service.Deactivate(id));

    private IActionResult ChangeState(Action action)
    {
        try { action(); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
}