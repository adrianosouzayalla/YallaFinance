using YallaFinance.Application;
using YallaFinance.Domain.Customers;
using System.ComponentModel.DataAnnotations;
namespace YallaFinance.Application.Features.Inbound.Debits.ReceiveDebit;

public sealed record CustomerInboundData(
    [Required] string ExternalId,
    [Required] string Name,
    [Required] string Document,
    [Required] string DocumentType,
    [Required] string? Email,
    string? Phone,
    [Required] string? DocumentIssuingCountry = null,
    [Required] string? PhoneCountryCallingCode = null,
    [Required] string? PhoneNationalNumber = null,
    [Required] CustomerInboundAddressData? Address = null)
{
    // Old persisted jobs have none of these fields. This is not an HTTP validation bypass.
    public bool HasExtendedData() => DocumentIssuingCountry is not null || PhoneCountryCallingCode is not null
        || PhoneNationalNumber is not null || Address is not null;
}

// Complete inbound contract; the reusable canonical address still permits partial data in other journeys.
public sealed record CustomerInboundAddressData(
    [Required] string? CountryCode = null,
    [Required] string? PostalCode = null,
    [Required] string? Street = null,
    string? Number = null,
    [Required] bool? WithoutNumber = null,
    string? Complement = null,
    [Required] string? District = null,
    [Required] string? City = null,
    [Required] string? StateCode = null)
{
    public CustomerAddressData ToCanonicalData() => new(CountryCode, PostalCode, Street, Number, WithoutNumber,
        Complement, District, City, StateCode);
}
