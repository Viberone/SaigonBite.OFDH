namespace SaigonBite.Web.Domain;

/// <summary>Abstract customer; the generalization has two subtypes with different registration and payment options.</summary>
public abstract class Customer
{
    public int Id { get; init; }
    public string FullName { get; init; } = "";
    public string? Email { get; init; }
    public List<Address> Addresses { get; init; } = [];

    public abstract string CustomerType { get; }
    /// <summary>Online option differs by subtype (A08); both may pay cash on delivery.</summary>
    public abstract IReadOnlyList<PaymentMethod> AllowedPaymentMethods { get; }
    public bool CanPayWith(PaymentMethod m) => AllowedPaymentMethods.Contains(m);
}

public class LocalCustomer : Customer
{
    public string Phone { get; init; } = "";   // Vietnamese mobile number (A09)
    public override string CustomerType => "Local";
    public override IReadOnlyList<PaymentMethod> AllowedPaymentMethods => [PaymentMethod.COD, PaymentMethod.EWallet];
}

public class ForeignTourist : Customer
{
    public string PassportNo { get; init; } = "";
    public string Nationality { get; init; } = "";
    public override string CustomerType => "Tourist";
    public override IReadOnlyList<PaymentMethod> AllowedPaymentMethods => [PaymentMethod.COD, PaymentMethod.Card];
}

public class Address
{
    public int Id { get; init; }
    public int CustomerId { get; init; }
    public string Label { get; init; } = "";
    public string Street { get; init; } = "";
    public string District { get; init; } = "";
    public bool IsHotel { get; init; }
    public override string ToString() => $"{Label}: {Street}, {District}";
}

/// <summary>Employee; kitchen staff and shippers belong to one hub (A10).</summary>
public class Staff
{
    public int Id { get; init; }
    public string FullName { get; init; } = "";
    public StaffRole Role { get; init; }
    public int? HubId { get; init; }
}
