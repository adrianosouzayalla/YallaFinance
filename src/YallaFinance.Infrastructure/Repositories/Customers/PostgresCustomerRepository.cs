using Microsoft.EntityFrameworkCore;
using YallaFinance.Infrastructure.Database.Generated;
using YallaFinance.Infrastructure.Database.Generated.Entities;
using YallaFinance.Application.Repositories.Customers;

namespace YallaFinance.Infrastructure.Repositories.Customers;

public sealed partial class PostgresCustomerRepository : ICustomerRepository
{
    private readonly YallaFinanceDbContext db;
    public PostgresCustomerRepository(YallaFinanceDbContext db)
    {
        this.db = db;
    }

    public Task<Customer?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Customers.SingleOrDefaultAsync(value => value.Id == id, cancellationToken);

    public void Add(Customer customer) => db.Customers.Add(customer);

    public async Task<ChargeCustomerRecord?> FindLocalForChargeAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken) =>
        await db.Customers.AsNoTracking()
            .Where(customer => customer.TenantId == tenantId && customer.Id == customerId)
            .Select(customer => new ChargeCustomerRecord(
                customer.Id,
                customer.TenantId,
                customer.Name,
                customer.Document,
                customer.DocumentType,
                customer.Email,
                customer.Phone,
                customer.Active,
                customer.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ChargeCustomerRecord>> FindLocalByDocumentForChargeAsync(
        Guid tenantId,
        string normalizedDocument,
        CancellationToken cancellationToken) =>
        await db.Customers.AsNoTracking()
            .Where(customer => customer.TenantId == tenantId && customer.NormalizedDocument == normalizedDocument)
            .Select(customer => new ChargeCustomerRecord(
                customer.Id,
                customer.TenantId,
                customer.Name,
                customer.Document,
                customer.DocumentType,
                customer.Email,
                customer.Phone,
                customer.Active,
                customer.CreatedAt))
            .ToArrayAsync(cancellationToken);

    public Task AcquireCustomerLockAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var lockKey = $"customer:{customerId}";
        return db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))",
            cancellationToken);
    }

    public Task AcquireDocumentLockAsync(Guid tenantId, string normalizedDocument, CancellationToken cancellationToken)
    {
        var lockKey = GetDocumentLockKey(tenantId, normalizedDocument);
        return db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))",
            cancellationToken);
    }

    internal static string GetDocumentLockKey(Guid tenantId, string normalizedDocument) =>
        $"inbound-customer-document:{tenantId}:{normalizedDocument}";

    public Task AddForChargeAsync(ChargeCustomerData customer, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var entity = new Customer
        {
            Id = customer.Id,
            TenantId = customer.TenantId,
            Name = customer.Name,
            Document = customer.Document,
            DocumentType = customer.DocumentType,
            Email = customer.Email,
            Phone = customer.Phone,
            Active = customer.Active,
        };
        CompleteAddressForCharge(entity, customer.Address);
        db.Customers.Add(entity);
        return Task.CompletedTask;
    }

    public async Task UpdateForChargeAsync(ChargeCustomerData customer, CancellationToken cancellationToken)
    {
        var persistedCustomer = await db.Customers.Include(value => value.CustomerAddress).SingleAsync(
            value => value.TenantId == customer.TenantId && value.Id == customer.Id,
            cancellationToken);
        persistedCustomer.Name = customer.Name;
        persistedCustomer.Document = customer.Document;
        persistedCustomer.DocumentType = customer.DocumentType;
        persistedCustomer.Email = customer.Email;
        persistedCustomer.Phone = customer.Phone;
        persistedCustomer.Active = customer.Active;
        CompleteAddressForCharge(persistedCustomer, customer.Address);
    }

    internal static void CompleteAddressForCharge(Customer customer, YallaFinance.Domain.Customers.CustomerAddressData? incoming)
    {
        if (incoming is null) return;
        incoming.Validate();
        var current = customer.CustomerAddress;
        // Never combine address components belonging to different countries.
        if (current?.CountryCode is not null && incoming.CountryCode is not null
            && current.CountryCode != incoming.CountryCode)
            return;
        var addressData = new YallaFinance.Domain.Customers.CustomerAddressData(
            current?.CountryCode ?? incoming.CountryCode, current?.PostalCode ?? incoming.PostalCode,
            current?.Street ?? incoming.Street,
            current?.WithoutNumber is not null ? current.Number : incoming.Number,
            current?.WithoutNumber ?? incoming.WithoutNumber,
            current?.Complement ?? incoming.Complement, current?.District ?? incoming.District,
            current?.City ?? incoming.City, current?.StateCode ?? incoming.StateCode);
        addressData.Validate();
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
