using Microsoft.EntityFrameworkCore;
using YallaFinance.Domain.Customers;
using YallaFinance.Infrastructure.Database.Generated.Entities;

namespace YallaFinance.Infrastructure.Database.Generated;

public partial class YallaFinanceDbContext
{
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareCustomerData();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        PrepareCustomerData();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void PrepareCustomerData()
    {
        ChangeTracker.DetectChanges();
        foreach (var entry in ChangeTracker.Entries<Customer>().Where(entry => entry.State is EntityState.Added or EntityState.Modified))
        {
            var customer = entry.Entity;
            bool Changed(string name) => entry.State == EntityState.Added || entry.Property(name).IsModified;
            if ((Changed(nameof(Customer.Document)) || Changed(nameof(Customer.DocumentType)))
                && !CustomerDocument.HasValidFormat(customer.Document, customer.DocumentType))
                throw new ArgumentException("Informe um documento compatível com o tipo CPF ou CNPJ.");
            if (Changed(nameof(Customer.DocumentIssuingCountry)) && customer.DocumentIssuingCountry is { } country
                && (!CustomerAddressData.IsCountryCode(country) || country != "BR"))
                throw new ArgumentException("CPF e CNPJ devem possuir país emissor BR.");

            var structuredChanged = Changed(nameof(Customer.PhoneCountryCallingCode)) || Changed(nameof(Customer.PhoneNationalNumber));
            if (!structuredChanged && Changed(nameof(Customer.Phone)) && customer.PhoneCountryCallingCode is not null
                && customer.Phone != $"+{customer.PhoneCountryCallingCode}{customer.PhoneNationalNumber}")
            {
                customer.PhoneCountryCallingCode = null;
                customer.PhoneNationalNumber = null;
            }
            if (structuredChanged && (customer.PhoneCountryCallingCode is not null || customer.PhoneNationalNumber is not null))
            {
                var phone = new CustomerPhoneData(customer.PhoneCountryCallingCode!, customer.PhoneNationalNumber!).ToInternationalNumber();
                if (Changed(nameof(Customer.Phone)) && customer.Phone is not null && customer.Phone != phone)
                    throw new ArgumentException("O telefone informado diverge dos componentes estruturados.");
                customer.Phone = phone;
            }
        }
        foreach (var entry in ChangeTracker.Entries<CustomerAddress>().Where(entry => entry.State is EntityState.Added or EntityState.Modified))
        {
            var address = entry.Entity;
            if (address.TenantId == Guid.Empty || address.CustomerId == Guid.Empty
                || ChangeTracker.Entries<Customer>().Any(owner => owner.Entity.Id == address.CustomerId
                    && owner.Entity.TenantId != address.TenantId))
                throw new ArgumentException("O endereço deve pertencer ao cliente da mesma empresa.");
            new CustomerAddressData(address.CountryCode, address.PostalCode, address.Street, address.Number,
                address.WithoutNumber, address.Complement, address.District, address.City, address.StateCode).Validate();
        }
        ChangeTracker.DetectChanges();
    }
}
