using Microsoft.AspNetCore.Mvc;
using SaigonBite.Web.DataAccess;
using SaigonBite.Web.Domain;
using SaigonBite.Web.Infrastructure;
using SaigonBite.Web.Integration;

namespace SaigonBite.Web.Controllers;

/// <summary>
/// UC03 Place an Order (author 523C0007). Use case controller: one action per SSD message
/// (selectHub -> New, enterOrderDetails -> Quote, confirmOrder -> Place), plus order tracking and cancel (supporting UC04, UC05).
/// </summary>
[Role("Customer")]
public class OrderController(HubDAO hubs, MenuItemDAO menu, OrderDAO orders, VoucherDAO vouchers, RuleDAO rules,
                             IMapService map, IPaymentGateway gateway, ICurrentUser user, IClock clock) : Controller
{
    private Customer Customer => user.Customer!;

    // SSD 1: selectHub(hubId) -> menu with availability, or quieter hubs when busy
    [HttpGet]
    public IActionResult New(int hubId)
    {
        var r = rules.Load();
        var hub = hubs.GetById(hubId);
        if (hub == null) return NotFound();
        if (hub.IsBusy(r)) return View("HubBusy", BusyPage(hub, r));
        var page = Page(new OrderForm { HubId = hubId, AddressId = Customer.Addresses.FirstOrDefault()?.Id ?? 0 }, hub, r);
        return View("New", page);
    }

    // SSD 2: enterOrderDetails(items, address, voucher, paymentMethod) -> full price
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Quote(OrderForm form)
    {
        var (page, busy) = Price(form);
        if (busy != null) return View("HubBusy", busy);
        return View(page.Errors.Count > 0 ? "New" : "Quote", page);
    }

    // SSD 3: confirmOrder() -> order Placed, payment authorised (online) or pending (cash)
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Place(OrderForm form)
    {
        var (page, busy) = Price(form);       // price again: availability, voucher or load may have changed
        if (busy != null) return View("HubBusy", busy);
        if (page.Errors.Count > 0) return View("New", page);

        var r = page.Rules;
        var order = Order.Place(Customer, page.Hub, page.Address!, page.Lines, page.Quote!, form.PaymentMethod, clock.Now);
        var payment = new Payment { Method = form.PaymentMethod, Amount = order.Total };
        if (form.PaymentMethod != PaymentMethod.COD)
        {
            var reference = gateway.Authorise(form.PaymentMethod, order.Total, $"C{Customer.Id}");
            if (reference == null)
            {
                page.Errors.Add("The payment was declined. Choose another method or pay cash on delivery.");
                return View("Quote", page);
            }
            payment.Authorised(reference);
        }
        try
        {
            orders.InsertPlaced(order, payment, r.BusyLoad);
        }
        catch (HubBusyException)
        {
            if (payment.GatewayRef != null) gateway.Void(payment.GatewayRef);
            return View("HubBusy", BusyPage(hubs.GetById(form.HubId)!, r));
        }
        TempData["Message"] = $"Order #{order.Id} is placed. The kitchen has been notified.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    // supporting UC04 Track an order
    public IActionResult Index() => View(orders.GetByCustomer(Customer.Id));

    public IActionResult Details(int id)
    {
        var o = orders.GetById(id);
        if (o == null || o.CustomerId != Customer.Id) return NotFound();
        ViewBag.Payment = orders.GetPayment(id);
        return View(o);
    }

    // supporting UC05 Cancel an order: only while Placed (A07, confirmed at the Q&A)
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Cancel(int id)
    {
        var o = orders.GetById(id);
        if (o == null || o.CustomerId != Customer.Id) return NotFound();
        try
        {
            o.Cancel(clock.Now);
            var p = orders.GetPayment(id)!;
            if (p.GatewayRef != null) gateway.Void(p.GatewayRef);
            p.Void();
            orders.SaveStatus(o, p, clock.Now);
            TempData["Message"] = $"Order #{id} is cancelled.";
        }
        catch (DomainRuleException e) { TempData["Error"] = e.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---------- helpers ----------
    private OrderPageVM Page(OrderForm form, Hub hub, BusinessRules r) => new()
    {
        Form = form, Hub = hub, Rules = r, Menu = menu.GetMenu(hub.Id),
        Addresses = Customer.Addresses, PaymentMethods = Customer.AllowedPaymentMethods,
    };

    /// <summary>Validates the posted order and prices it. Returns a busy page instead when the hub is full.</summary>
    private (OrderPageVM Page, HubBusyVM? Busy) Price(OrderForm form)
    {
        var r = rules.Load();
        var hub = hubs.GetById(form.HubId) ?? throw new BadHttpRequestException("Unknown hub");
        if (hub.IsBusy(r)) return (null!, BusyPage(hub, r));
        var page = Page(form, hub, r);

        int lineNo = 0;
        foreach (var (itemId, qty) in form.Quantities.Where(q => q.Value > 0))
        {
            var item = page.Menu.FirstOrDefault(m => m.Id == itemId);
            if (item == null) { page.Errors.Add($"Item {itemId} is not on this hub's menu."); continue; }
            if (!item.IsAvailable) { page.Errors.Add($"{item.NameEn} has just sold out at {hub.Name}."); continue; }
            if (qty > 20) { page.Errors.Add($"At most 20 of {item.NameEn} per order."); continue; }
            page.Lines.Add(new OrderLine { LineNo = ++lineNo, ItemId = item.Id, ItemName = item.NameEn, Quantity = qty, UnitPrice = item.Price });
        }
        if (page.Lines.Count == 0 && page.Errors.Count == 0) page.Errors.Add("Choose at least one item.");

        page.Address = Customer.Addresses.FirstOrDefault(a => a.Id == form.AddressId);
        double? km = null;
        if (page.Address == null) page.Errors.Add("Choose a delivery address.");
        else
        {
            km = map.DistanceKm(hub.Id, page.Address.District);
            if (km == null || km > r.MaxKm)
                page.Errors.Add($"{page.Address.District} is outside the {r.MaxKm:0} km delivery radius of {hub.Name}.");
        }
        if (!Customer.CanPayWith(form.PaymentMethod))
            page.Errors.Add($"{Customer.CustomerType} customers can pay by {string.Join(" or ", Customer.AllowedPaymentMethods)}.");

        Voucher? voucher = null;
        if (!string.IsNullOrWhiteSpace(form.VoucherCode))
        {
            voucher = vouchers.GetByCode(form.VoucherCode);
            int food = page.Lines.Sum(l => l.LineTotal);
            string? problem = voucher == null
                ? $"Voucher {form.VoucherCode.Trim().ToUpperInvariant()} does not exist."
                : voucher.CheckUsable(food, clock.Today, orders.CustomerUsedVoucher(Customer.Id, voucher.Id));
            if (problem != null) { page.Errors.Add(problem); voucher = null; }
        }
        if (page.Errors.Count == 0)
            page.Quote = new PriceCalculator(r).Quote(hub, page.Lines, km!.Value, voucher);
        return (page, null);
    }

    private HubBusyVM BusyPage(Hub hub, BusinessRules r)
    {
        var home = Customer.Addresses.FirstOrDefault();
        var alternatives = hubs.GetAll()
            .Where(h => h.Id != hub.Id && !h.IsBusy(r))
            .Select(h => new HubChoice(h, h.LoadLabel(r), home == null ? null : map.DistanceKm(h.Id, home.District), home?.Label))
            .Where(c => c.DistanceKm is null || c.DistanceKm <= r.MaxKm)
            .OrderBy(c => c.Hub.Load).ToList();
        return new HubBusyVM { Hub = hub, Alternatives = alternatives };
    }
}
