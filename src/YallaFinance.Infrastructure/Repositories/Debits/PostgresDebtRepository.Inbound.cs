using YallaFinance.Infrastructure.Database.Transactions;
using System.Data;
using YallaFinance.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using YallaFinance.Application.Common.Results;


using YallaFinance.Infrastructure.Database.Generated;
using E = YallaFinance.Infrastructure.Database.Generated.Entities;
using YallaFinance.Application.Repositories.Debits;
using YallaFinance.Application.Features.Inbound.Debits.ReceiveDebit;
using YallaFinance.Application.Common.Transactions;

namespace YallaFinance.Infrastructure.Repositories.Debits;

internal sealed partial class PostgresDebtRepository
{
    public async Task<NotificationResult<ReceiveDebitResult>> ReceiveAsync(
        Guid tenantId,
        ReceiveDebitPayload payload,
        CancellationToken cancellationToken)
    {
        try
        {
            return await unitOfWork.ExecuteAsync(
                async token =>
                {
                    if (!await db.Tenants.AnyAsync(x => x.Id == tenantId && x.Active, token))
                        return CreateInboundDebitFailureResult(
                            "InboundTenantUnavailable",
                            "Tenant indisponível para o processamento.");

                    var input = payload.Debit;
                    await AcquireInboundProcessingLockAsync($"inbound-debt:{tenantId}:{ReceiveDebitJob.Origin}:{input.ExternalId}", token);
                    var existing = await db.Debts.Include(x => x.Contract).ThenInclude(x => x.Customer).Include(x => x.Category)
                        .SingleOrDefaultAsync(
                            x => x.TenantId == tenantId &&
                                x.Origin == ReceiveDebitJob.Origin &&
                                x.ExternalId == input.ExternalId,
                            token);
                    if (existing is not null)
                        return ReceiveDebitReplay.Match(existing, payload)
                            ? NotificationResult<ReceiveDebitResult>.Success(new(
                                existing.Id,
                                existing.Contract.CustomerId,
                                existing.ContractId,
                                input.ExternalId,
                                false))
                            : CreateInboundDebitFailureResult(
                                "InboundDebitConflict",
                                "Identificador de débito já utilizado com dados diferentes.");

                    var category = await db.Categories.SingleOrDefaultAsync(
                        x => x.TenantId == tenantId &&
                            x.Code == input.CategoryCode &&
                            x.Active,
                        token);
                    if (category is null)
                        return CreateInboundDebitFailureResult(
                            "InboundCategoryUnavailable",
                            "Categoria indisponível para o tenant.");
                    var customerInput = payload.Customer;
                    var document = CustomerDocument.Normalize(customerInput.Document);
                    await AcquireInboundProcessingLockAsync($"inbound-customer:{tenantId}:{customerInput.ExternalId}", token);
                    await AcquireInboundProcessingLockAsync($"inbound-customer-document:{tenantId}:{document}", token);
                    var customer = await db.Customers.Include(value => value.CustomerAddress).SingleOrDefaultAsync(
                        x => x.TenantId == tenantId &&
                            x.ExternalId == customerInput.ExternalId,
                        token);
                    if (customer is not null &&
                        !ReceiveDebitPersistenceRules.CustomerMatches(
                            customer,
                            customerInput))
                        return CreateInboundDebitFailureResult(
                            "InboundCustomerConflict",
                            "Cliente existente incompatível com o recebimento.");
                    if (customer is null &&
                        await db.Customers.AnyAsync(
                            x => x.TenantId == tenantId &&
                                x.ExternalId == null &&
                                x.NormalizedDocument == document,
                            token))
                        return CreateInboundDebitFailureResult(
                            "InboundCustomerConflict",
                            "Cliente legado exige vinculação de identificador externo.");
                    var currentUtcTime = clock.GetUtcNow().UtcDateTime;
                    if (customer is null)
                    {
                        customer = new E.Customer
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenantId,
                            ExternalId = customerInput.ExternalId,
                            Name = customerInput.Name,
                            Document = customerInput.Document,
                            DocumentType = customerInput.DocumentType,
                            Email = customerInput.Email,
                            Phone = customerInput.Phone,
                            Active = true,
                            CreatedAt = currentUtcTime,
                            UpdatedAt = currentUtcTime
                        };
                        db.Customers.Add(customer);
                    }
                    var completion = ReceiveDebitPersistenceRules.CompleteMissingCustomerData(customer, customerInput);
                    if (completion.IsFailure)
                        return NotificationResult<ReceiveDebitResult>.Failure(completion.Errors);
                    var contractInput = input.Contract;
                    await AcquireInboundProcessingLockAsync($"inbound-contract:{tenantId}:{contractInput.ExternalId}", token);
                    await AcquireInboundProcessingLockAsync($"inbound-contract-number:{tenantId}:{contractInput.Number}", token);
                    var contract = await db.Contracts.SingleOrDefaultAsync(
                        x => x.TenantId == tenantId &&
                            x.ExternalId == contractInput.ExternalId,
                        token);
                    if (contract is not null &&
                        !ReceiveDebitPersistenceRules.ContractMatches(
                            contract,
                            customer.Id,
                            contractInput))
                        return CreateInboundDebitFailureResult(
                            "InboundContractConflict",
                            "Contrato existente incompatível com o recebimento.");
                    if (contract is null &&
                        await db.Contracts.AnyAsync(
                            x => x.TenantId == tenantId &&
                                x.ExternalId == null &&
                                x.Number == contractInput.Number,
                            token))
                        return CreateInboundDebitFailureResult(
                            "InboundContractConflict",
                            "Contrato legado exige vinculação de identificador externo.");
                    if (contract is null)
                    {
                        contract = new E.Contract
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenantId,
                            ExternalId = contractInput.ExternalId,
                            Number = contractInput.Number,
                            CustomerId = customer.Id,
                            Customer = customer,
                            Plate = contractInput.Plate,
                            Product = contractInput.Product,
                            Fleet = contractInput.Fleet,
                            Active = true,
                            CreatedAt = currentUtcTime,
                            UpdatedAt = currentUtcTime
                        };
                        db.Contracts.Add(contract);
                    }
                    var debt = new E.Debt
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        ContractId = contract.Id,
                        Contract = contract,
                        CategoryId = category.Id,
                        Category = category,
                        ExternalId = input.ExternalId,
                        Amount = input.Amount,
                        OccurrenceDate = input.OccurrenceDate,
                        DueDate = input.DueDate,
                        Description = input.Description,
                        Origin = ReceiveDebitJob.Origin,
                        Status = "Open",
                        CreatedAt = currentUtcTime,
                        UpdatedAt = currentUtcTime
                    };
                    db.Debts.Add(debt);
                    PostgresUnitOfWork.GetCommitValueOrThrow(await unitOfWork.CommitAsync(token));
                    return NotificationResult<ReceiveDebitResult>.Success(new(
                        debt.Id,
                        customer.Id,
                        contract.Id,
                        input.ExternalId,
                        true));
                },
                cancellationToken);
        }
        catch (DBConcurrencyException)
        {
            return CreateInboundDebitFailureResult(
                "InboundDebitConflict",
                "Uma restrição de unicidade impediu a persistência do recebimento.");
        }
    }

    private Task AcquireInboundProcessingLockAsync(string key, CancellationToken token) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))",
            token);

    private static NotificationResult<ReceiveDebitResult> CreateInboundDebitFailureResult(string code, string message) =>
        NotificationResult<ReceiveDebitResult>.Failure(new NotificationError(code, message));
}
