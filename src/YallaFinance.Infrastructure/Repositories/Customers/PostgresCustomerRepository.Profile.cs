using Microsoft.EntityFrameworkCore;
using YallaFinance.Application.Repositories.Customers.Contracts;
using YallaFinance.Domain.Customers;
using YallaFinance.Infrastructure.Database.Generated.Entities;

namespace YallaFinance.Infrastructure.Repositories.Customers;

public sealed partial class PostgresCustomerRepository
{
    public Task<CustomerProfileData?> FindProfileAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken) =>
        db.Customers.AsNoTracking().Where(customer => customer.TenantId == tenantId && customer.Id == customerId)
            .Select(customer => new CustomerProfileData(customer.Id, customer.TenantId, customer.Name,
                customer.Document, customer.DocumentType, customer.DocumentIssuingCountry, customer.Email, customer.Phone,
                customer.PhoneCountryCallingCode == null ? null : new CustomerPhoneData(customer.PhoneCountryCallingCode, customer.PhoneNationalNumber!),
                customer.CustomerAddress == null ? null : new CustomerAddressRecord(customer.CustomerAddress.Id,
                    new CustomerAddressData(customer.CustomerAddress.CountryCode, customer.CustomerAddress.PostalCode, customer.CustomerAddress.Street,
                        customer.CustomerAddress.Number, customer.CustomerAddress.WithoutNumber, customer.CustomerAddress.Complement,
                        customer.CustomerAddress.District, customer.CustomerAddress.City, customer.CustomerAddress.StateCode))))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<bool> UpdateProfileAsync(Guid tenantId, Guid customerId, CustomerProfileUpdate update,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(update);
        var customer = await db.Customers.Include(customer => customer.CustomerAddress)
            .SingleOrDefaultAsync(customer => customer.TenantId == tenantId && customer.Id == customerId, cancellationToken);
        if (customer is null) return false;
        ApplyProfileUpdate(customer, update);
        return true;
    }

    internal static void ApplyProfileUpdate(Customer customer, CustomerProfileUpdate update)
    {
        var country = update.DocumentIssuingCountry is { } countryChange ? countryChange.Value : customer.DocumentIssuingCountry;
        if (country is not null && country != "BR")
            throw new ArgumentException("CPF e CNPJ devem possuir país emissor BR.");
        var internationalPhone = update.Phone?.Value?.ToInternationalNumber();
        CustomerAddressData? addressData = null;
        if (update.Address is { } change)
        {
            var current = customer.CustomerAddress;
            addressData = new(
                change.CountryCode is { } countryCode ? countryCode.Value : current?.CountryCode,
                change.PostalCode is { } postalCode ? postalCode.Value : current?.PostalCode,
                change.Street is { } street ? street.Value : current?.Street,
                change.Number is { } number ? number.Value : current?.Number,
                change.WithoutNumber is { } withoutNumber ? withoutNumber.Value : current?.WithoutNumber,
                change.Complement is { } complement ? complement.Value : current?.Complement,
                change.District is { } district ? district.Value : current?.District,
                change.City is { } city ? city.Value : current?.City,
                change.StateCode is { } state ? state.Value : current?.StateCode);
            addressData.Validate();
        }
        // Validate every supplied component before mutating tracked entities.
        if (update.DocumentIssuingCountry is not null) customer.DocumentIssuingCountry = country;
        if (update.Phone is not null)
        {
            customer.PhoneCountryCallingCode = update.Phone.Value?.CountryCallingCode;
            customer.PhoneNationalNumber = update.Phone.Value?.NationalNumber;
            customer.Phone = internationalPhone;
        }
        if (addressData is not null)
        {
            var address = customer.CustomerAddress ??= new CustomerAddress
            {
                Id = Guid.NewGuid(), TenantId = customer.TenantId, CustomerId = customer.Id, Customer = customer
            };
            address.CountryCode = addressData.CountryCode;
            address.PostalCode = addressData.PostalCode;
            address.Street = addressData.Street;
            address.Number = addressData.Number;
            address.WithoutNumber = addressData.WithoutNumber;
            address.Complement = addressData.Complement;
            address.District = addressData.District;
            address.City = addressData.City;
            address.StateCode = addressData.StateCode;
        }
    }
}
