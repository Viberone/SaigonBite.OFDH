using Microsoft.AspNetCore.Mvc;
using SaigonBite.Web.DataAccess;
using SaigonBite.Web.Domain;
using SaigonBite.Web.Infrastructure;
using SaigonBite.Web.Integration;

namespace SaigonBite.Web.Controllers;

/// <summary>UC16 Produce Management Reports (author 523C0001): sales, hub workload and delivery performance.</summary>
[Role("Manager")]
public class ReportController(ReportDAO reports, HubDAO hubs, IClock clock) : Controller
{
    // SSD: requestReport(from, to, hub) -> three reports and the best-selling items
    public IActionResult Index(DateOnly? from, DateOnly? to, int? hubId)
    {
        var errors = new List<string>();
        var end = to ?? clock.Today;
        var start = from ?? end.AddDays(-13);
        if (start > end) { errors.Add("The start date must be on or before the end date."); start = end.AddDays(-13); }
        if (end.DayNumber - start.DayNumber > 92) { errors.Add("Choose a period of at most 3 months."); start = end.AddDays(-92); }
        return View(new ReportVM { Report = reports.Build(start, end, hubId), Hubs = hubs.GetAll(), Errors = errors });
    }
}

/// <summary>Supporting UC02 Browse hubs, brands and menus.</summary>
[Role("Customer")]
public class MenuController(HubDAO hubs, RuleDAO rules, IMapService map, ICurrentUser user) : Controller
{
    public IActionResult Index()
    {
        var r = rules.Load();
        var home = user.Customer!.Addresses.FirstOrDefault();
        var choices = hubs.GetAll()
            .Select(h => new HubChoice(h, h.LoadLabel(r), home == null ? null : map.DistanceKm(h.Id, home.District), home?.Label))
            .ToList();
        ViewBag.Rules = r;
        return View(choices);
    }
}

/// <summary>Supporting UC07 Confirm or reject, UC08 Prepare and mark ready, UC09 Item availability.</summary>
[Role("Kitchen")]
public class KitchenController(OrderDAO orders, HubDAO hubs, MenuItemDAO menu, RuleDAO rules, IPaymentGateway gateway,
                               ICurrentUser user, IClock clock) : Controller
{
    private int HubId => user.Staff!.HubId!.Value;

    public IActionResult Index() => View(new KitchenBoardVM
    {
        Hub = hubs.GetById(HubId)!, Rules = rules.Load(),
        Incoming = orders.GetForHub(HubId, OrderStatus.Placed),
        Cooking = orders.GetForHub(HubId, OrderStatus.Confirmed, OrderStatus.Preparing),
        Ready = orders.GetForHub(HubId, OrderStatus.ReadyForPickup),
        Menu = menu.GetMenu(HubId),
    });

    [HttpPost, ValidateAntiForgeryToken] public IActionResult Confirm(int id) => Step(id, o => o.Confirm(clock.Now), "confirmed");
    [HttpPost, ValidateAntiForgeryToken] public IActionResult Start(int id) => Step(id, o => o.StartPreparing(), "is being prepared");
    [HttpPost, ValidateAntiForgeryToken] public IActionResult Ready(int id) => Step(id, o => o.MarkReady(clock.Now), "is ready for pickup");

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Reject(int id, string reason)
    {
        var o = orders.GetById(id);
        if (o == null || o.HubId != HubId) return NotFound();
        try
        {
            o.Reject(reason, clock.Now);
            var p = orders.GetPayment(id)!;
            if (p.GatewayRef != null) gateway.Void(p.GatewayRef);
            p.Void();
            orders.SaveStatus(o, p, clock.Now);
            TempData["Message"] = $"Order #{id} rejected; the customer is notified and any online payment voided.";
        }
        catch (DomainRuleException e) { TempData["Error"] = e.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Availability(int itemId, bool available)
    {
        menu.SetAvailability(HubId, itemId, available);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Rain(bool on)
    {
        hubs.SetRainSurcharge(HubId, on);
        TempData["Message"] = on ? "Rain surcharge switched on for this hub." : "Rain surcharge switched off.";
        return RedirectToAction(nameof(Index));
    }

    private IActionResult Step(int id, Action<Order> change, string done)
    {
        var o = orders.GetById(id);
        if (o == null || o.HubId != HubId) return NotFound();
        try { change(o); orders.SaveStatus(o, null, clock.Now); TempData["Message"] = $"Order #{id} {done}."; }
        catch (DomainRuleException e) { TempData["Error"] = e.Message; }
        return RedirectToAction(nameof(Index));
    }
}

/// <summary>Start page: pick a demo user (no passwords in this prototype).</summary>
public class HomeController(CustomerDAO customers, StaffDAO staff) : Controller
{
    public IActionResult Index(string? denied)
    {
        ViewBag.Denied = denied;
        ViewBag.Customers = customers.GetAll().Take(4).ToList();
        ViewBag.Staff = staff.GetAll();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Login(string who)
    {
        Response.Cookies.Append(CookieCurrentUser.Cookie, who, new CookieOptions { HttpOnly = true, IsEssential = true });
        if (who.StartsWith('C')) return RedirectToAction("Index", "Menu");
        var s = int.TryParse(who.AsSpan(2), out int id) ? staff.GetById(id) : null;
        return s?.Role switch
        {
            StaffRole.Kitchen => RedirectToAction("Index", "Kitchen"),
            StaffRole.Shipper => RedirectToAction("Index", "Delivery"),
            StaffRole.Admin => RedirectToAction("Index", "Voucher"),
            StaffRole.Manager => RedirectToAction("Index", "Report"),
            _ => RedirectToAction(nameof(Index)),
        };
    }

    public IActionResult Logout()
    {
        Response.Cookies.Delete(CookieCurrentUser.Cookie);
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Error() => View();
}
