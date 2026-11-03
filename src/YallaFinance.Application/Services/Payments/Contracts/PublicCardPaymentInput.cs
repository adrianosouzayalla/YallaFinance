using System.Net.Mail;
using YallaFinance.Application.Repositories.Customers.Contracts;
using YallaFinance.Application.Services.Payments.CreditCard.Contracts;

namespace YallaFinance.Application.Services.Payments.Contracts;

// Transient checkout data. Never persist, log or enqueue this input.
public sealed record PublicCardHolderInput(string? Name = null, string? Email = null, string? Document = null,
    string? PostalCode = null, string? AddressNumber = null, string? Phone = null)
{
    public override string ToString() => nameof(PublicCardHolderInput);
}

public sealed record PublicCardPaymentInput(string HolderName, string Number, string ExpiryMonth,
    string ExpiryYear, string SecurityCode, bool SameHolderAsCustomer, PublicCardHolderInput? Holder = null,
    Guid SubmissionId = default)
{
    public override string ToString() => nameof(PublicCardPaymentInput);

    public bool TryCreatePayment(CustomerProfileData customer, out CreditCardPaymentInput? payment, out string[] missing)
    {
        var source = SameHolderAsCustomer ? CustomerHolder(customer) : new PublicCardHolderInput();
        var holder = new PublicCardHolderInput(Holder?.Name ?? (SameHolderAsCustomer ? source.Name : HolderName), Holder?.Email ?? source.Email,
            Holder?.Document ?? source.Document, Holder?.PostalCode ?? source.PostalCode,
            Holder?.AddressNumber ?? source.AddressNumber, Holder?.Phone ?? source.Phone);
        var errors = MissingFields(holder).ToList();
        if (!Text(HolderName, 100)) errors.Add("holderName");
        if (!Digits(Number, 13, 19)) errors.Add("number");
        if (!Digits(SecurityCode, 3, 4)) errors.Add("securityCode");
        if (!Digits(ExpiryMonth, 2, 2) || !int.TryParse(ExpiryMonth, out var month) || month is < 1 or > 12)
            errors.Add("expiryMonth");
        if (!Digits(ExpiryYear, 4, 4)) errors.Add("expiryYear");
        missing = errors.ToArray();
        payment = missing.Length == 0 ? new(HolderName.Trim(), Number, ExpiryMonth, ExpiryYear, SecurityCode,
            holder.Name!.Trim(), holder.Email!.Trim(), OnlyDigits(holder.Document), OnlyDigits(holder.PostalCode),
            holder.AddressNumber!.Trim(), NationalPhone(holder.Phone)) : null;
        return payment is not null;
    }

    public static string[] MissingCustomerFields(CustomerProfileData customer) => MissingFields(CustomerHolder(customer));

    private static PublicCardHolderInput CustomerHolder(CustomerProfileData customer) => new(customer.Name,
        customer.Email, customer.Document, customer.Address?.Data.PostalCode,
        customer.Address?.Data.WithoutNumber == true ? "S/N" : customer.Address?.Data.Number,
        customer.StructuredPhone is { CountryCallingCode: "55" } phone ? phone.NationalNumber : customer.Phone);

    private static string[] MissingFields(PublicCardHolderInput holder)
    {
        var missing = new List<string>();
        if (!Text(holder.Name, 100)) missing.Add("name");
        if (!Text(holder.Email, 254) || !MailAddress.TryCreate(holder.Email, out _)) missing.Add("email");
        if (OnlyDigits(holder.Document).Length is not (11 or 14)) missing.Add("document");
        if (OnlyDigits(holder.PostalCode).Length != 8) missing.Add("postalCode");
        if (!Text(holder.AddressNumber, 30)) missing.Add("addressNumber");
        if (NationalPhone(holder.Phone).Length is not (10 or 11)) missing.Add("phone");
        return missing.ToArray();
    }

    public static string NationalPhone(string? value)
    {
        var digits = OnlyDigits(value);
        return digits.StartsWith("55", StringComparison.Ordinal) && digits.Length is 12 or 13 ? digits[2..] : digits;
    }
    private static string OnlyDigits(string? value) => new((value ?? "").Where(char.IsAsciiDigit).ToArray());
    private static bool Digits(string? value, int min, int max) => value is not null && value.Length >= min
        && value.Length <= max && value.All(char.IsAsciiDigit);
    private static bool Text(string? value, int max) => !string.IsNullOrWhiteSpace(value)
        && value.Length <= max && !value.Any(char.IsControl);
}
