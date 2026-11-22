using System.Globalization;
using SaigonBite.Web.Domain;

namespace SaigonBite.Web.Infrastructure;

/// <summary>Formatting helpers used by the views.</summary>
public static class Fmt
{
    public static string Vnd(long v) => v.ToString("N0", CultureInfo.InvariantCulture) + " ₫";

    public static string StatusClass(OrderStatus s) => s switch
    {
        OrderStatus.Delivered => "text-bg-success",
        OrderStatus.Cancelled or OrderStatus.Rejected or OrderStatus.DeliveryFailed => "text-bg-danger",
        OrderStatus.ReadyForPickup or OrderStatus.OutForDelivery => "text-bg-primary",
        _ => "text-bg-warning",
    };
}
