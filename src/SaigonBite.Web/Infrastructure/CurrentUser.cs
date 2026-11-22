using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SaigonBite.Web.DataAccess;
using SaigonBite.Web.Domain;

namespace SaigonBite.Web.Infrastructure;

/// <summary>Who is using the system. The demo picks a seeded user instead of a password login (authentication is out of scope).</summary>
public interface ICurrentUser
{
    Customer? Customer { get; }
    Staff? Staff { get; }
    string? Role => Customer != null ? "Customer" : Staff?.Role.ToString();
    string DisplayName => Customer?.FullName ?? Staff?.FullName ?? "Guest";
}

public class CookieCurrentUser(IHttpContextAccessor http, CustomerDAO customers, StaffDAO staff) : ICurrentUser
{
    public const string Cookie = "sb_user";
    private readonly Lazy<(Customer?, Staff?)> _user = new(() =>
    {
        var v = http.HttpContext?.Request.Cookies[Cookie];
        if (v is null || v.Length < 3 || !int.TryParse(v[2..], out int id)) return (null, null);
        return v[0] == 'C' ? (customers.GetById(id), null) : (null, staff.GetById(id));
    });
    public Customer? Customer => _user.Value.Item1;
    public Staff? Staff => _user.Value.Item2;
}

/// <summary>Fixed user, used by the automated tests.</summary>
public class FixedCurrentUser(Customer? customer = null, Staff? staff = null) : ICurrentUser
{
    public Customer? Customer { get; } = customer;
    public Staff? Staff { get; } = staff;
}

/// <summary>Only lets users with one of the roles into the controller; others go back to the start page.</summary>
public class RoleAttribute(params string[] roles) : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var user = context.HttpContext.RequestServices.GetRequiredService<ICurrentUser>();
        if (user.Role is null || !roles.Contains(user.Role))
            context.Result = new RedirectToActionResult("Index", "Home", new { denied = string.Join("/", roles) });
    }
}

/// <summary>Current time, replaceable in tests.</summary>
public interface IClock { DateTime Now { get; } DateOnly Today => DateOnly.FromDateTime(Now); }
public class SystemClock : IClock { public DateTime Now => DateTime.Now; public DateOnly Today => DateOnly.FromDateTime(Now); }
public class FixedClock(DateTime now) : IClock { public DateTime Now { get; set; } = now; public DateOnly Today => DateOnly.FromDateTime(Now); }
