using YallaFinance.Domain.Customers;

namespace YallaFinance.Application.Repositories.Customers.Contracts;

public sealed record CustomerAddressRecord(Guid Id, CustomerAddressData Data);

public sealed record CustomerProfileData(Guid Id, Guid TenantId, string Name, string Document, string DocumentType,
    string? DocumentIssuingCountry, string? Email, string? Phone, CustomerPhoneData? StructuredPhone,
    CustomerAddressRecord? Address)
{
    public string PersonType => DocumentType switch
    {
        "CPF" => "INDIVIDUAL",
        "CNPJ" => "COMPANY",
        _ => throw new InvalidOperationException("Tipo de documento não suportado para o cadastro nacional.")
    };
}

// Null wrapper means omission; a wrapper containing null is an explicit clear.
public sealed record CustomerFieldChange<T>(T? Value);

public sealed record CustomerProfileUpdate(CustomerFieldChange<string>? DocumentIssuingCountry = null,
    CustomerFieldChange<CustomerPhoneData>? Phone = null, CustomerAddressUpdate? Address = null);

public sealed record CustomerAddressUpdate(CustomerFieldChange<string>? CountryCode = null,
    CustomerFieldChange<string>? PostalCode = null, CustomerFieldChange<string>? Street = null,
    CustomerFieldChange<string>? Number = null, CustomerFieldChange<bool?>? WithoutNumber = null,
    CustomerFieldChange<string>? Complement = null, CustomerFieldChange<string>? District = null,
    CustomerFieldChange<string>? City = null, CustomerFieldChange<string>? StateCode = null);
