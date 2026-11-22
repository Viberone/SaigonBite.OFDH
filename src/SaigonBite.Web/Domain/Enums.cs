namespace SaigonBite.Web.Domain;

/// <summary>Order life cycle (Order state machine, 523C0007). Final states: Delivered, Cancelled, Rejected, DeliveryFailed.</summary>
public enum OrderStatus { Placed, Confirmed, Preparing, ReadyForPickup, OutForDelivery, Delivered, Cancelled, Rejected, DeliveryFailed }

public enum PaymentMethod { COD, EWallet, Card }

public enum PaymentStatus { Pending, Authorised, Captured, Collected, Voided }

/// <summary>Delivery life cycle (Delivery state machine, 523C0001).</summary>
public enum DeliveryStatus { Assigned, PickedUp, Delivered, Failed }

/// <summary>Voucher life cycle (Voucher state machine, 523C0007). Expired is derived from the dates and usage, not stored.</summary>
public enum VoucherStatus { Draft, Active, Expired, Deactivated }

public enum DiscountType { Percent, Fixed }

public enum StaffRole { Kitchen, Shipper, Office, Admin, Manager }

/// <summary>Raised when a state change is not allowed from the current state (a guard in a state machine).</summary>
public class DomainRuleException(string message) : Exception(message);
