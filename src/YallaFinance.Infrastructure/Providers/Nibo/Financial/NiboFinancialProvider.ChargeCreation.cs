using YallaFinance.Infrastructure.Providers.Configuration;
using YallaFinance.Infrastructure.Services.Financial.Contracts;
using YallaFinance.Infrastructure.Services.Financial;
using YallaFinance.Application.Services.Financial.Contracts;

namespace YallaFinance.Infrastructure.Providers.Nibo.Financial;

internal sealed partial class NiboFinancialProvider
{
    public async Task<CreateChargeResult> CreateChargeAsync(
        CreateChargeData data,
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        var chargeGate = await coordinator.AcquireChargeAsync(
            data.TenantId, connection.Id, data.ChargeId, cancellationToken);
        try
        {
            var document = FinancialDocument.Normalize(data.Customer.Document);
            if (document is null)
                return await CreateChargeCoreAsync(data, connection, cancellationToken);
            var customerGate = await coordinator.AcquireCustomerAsync(
                data.TenantId, connection.Id, document, cancellationToken);
            try
            {
                return await CreateChargeCoreAsync(data, connection, cancellationToken);
            }
            finally
            {
                customerGate.Release();
            }
        }
        finally
        {
            chargeGate.Release();
        }
    }

    private async Task<CreateChargeResult> CreateChargeCoreAsync(
        CreateChargeData data,
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var customerData = data.Customer;
        var document = FinancialDocument.Normalize(customerData.Document);
        if (string.IsNullOrWhiteSpace(customerData.Name) || customerData.Name.Any(char.IsControl) || data.Description.Any(char.IsControl) ||
            (customerData.Document is not null && document is null) ||
            (customerData.Email is not null && (!System.Net.Mail.MailAddress.TryCreate(
                customerData.Email, out var email) || email.Address != customerData.Email)) ||
            (customerData.Phone is not null && (customerData.Phone.Any(c => !(c is >= '0' and <= '9' or '+' or '-' or '(' or ')' or ' ')) ||
                customerData.Phone.Count(c => c is >= '0' and <= '9') is < 8 or > 15)))
            return CreateChargeResult.Rejected("Validation", connection.Id);

        var options = await configuration.ReadAsync(connection, cancellationToken);
        var category = await connections.ResolveMappingAsync(
            data.TenantId, connection.Id, "Category", data.CategoryId.ToString(), cancellationToken);
        if (category is null)
            return CreateChargeResult.Rejected("MappingMissing", connection.Id);
        if (!Guid.TryParse(category, out var categoryId) || categoryId == Guid.Empty)
            return CreateChargeResult.Rejected("InvalidMapping", connection.Id);
        if (document is null)
            return CreateChargeResult.Rejected("CustomerIdentityMissing", connection.Id);

        // Every attempt checks the provider again; prior results are diagnostics,
        // never an authorization to skip validation or block a new attempt.
        var existing = await FindReceivablesByReferenceAsync(
            new(data.TenantId, Provider, data.ChargeId, connection.Id), connection, cancellationToken);
        if (!existing.Success)
            return CreateChargeResult.Rejected(existing.ErrorCode!, connection.Id, diagnostic: existing.Diagnostic);
        if (existing.Items.Count > 1)
            return CreateChargeResult.Rejected("ReceivableAmbiguous", connection.Id);
        if (existing.Items.Count == 1)
        {
            var receivable = existing.Items[0];
            return CreateChargeResult.Completed(
                receivable.ProviderExternalId, receivable.ProviderCustomerId, connection.Id, alreadyExisted: true);
        }

        var customers = await FindCustomersByDocumentAsync(
            new(data.TenantId, Provider, document, connection.Id), connection, cancellationToken);
        if (!customers.Success)
            return CreateChargeResult.Rejected(customers.ErrorCode!, connection.Id, diagnostic: customers.Diagnostic);
        if (customers.Items.Count > 1)
            return CreateChargeResult.Rejected("CustomerAmbiguous", connection.Id);
        var customer = customers.Items.SingleOrDefault()?.ProviderExternalId;
        if (customer is not null && (!Guid.TryParse(customer, out var foundCustomerId) || foundCustomerId == Guid.Empty))
            return CreateChargeResult.Rejected("ProviderResponseInvalid", connection.Id);

        var customerCreated = false;
        if (customer is null)
        {
            var created = await CreateCustomerAsync(
                new(customerData.CustomerId, data.TenantId, customerData.Name.Trim(), Provider,
                    connection.Id, document, customerData.Email,
                    customerData.StructuredPhone?.ToInternationalNumber() ?? customerData.Phone,
                    customerData.Address), connection, cancellationToken);
            if (created.Outcome == FinancialIntegrationOutcome.Indeterminate)
                return CreateChargeResult.Indeterminate(connection.Id, code: "CustomerOutcomeUnknown", diagnostic: created.Diagnostic);
            if (!created.Success)
                return CreateChargeResult.Rejected(created.ErrorCode!, connection.Id, diagnostic: created.Diagnostic);
            if (!Guid.TryParse(created.ProviderExternalId, out var createdId) || createdId == Guid.Empty)
                return CreateChargeResult.Indeterminate(connection.Id, code: "CustomerOutcomeUnknown");
            customer = createdId.ToString();
            customerCreated = true;
        }

        try
        {
            var receivableData = new CreateReceivableIntegrationData(
                data.ChargeId, data.TenantId, customerData.CustomerId, data.CategoryId, data.Amount,
                data.ScheduleDate, data.DueDate, data.AccrualDate, data.Description,
                Provider, connection.Id, data.ExternalCostCenterId);
            var created = await SendReceivableAsync(receivableData, customer, category, options, cancellationToken);
            if (created.Outcome == FinancialIntegrationOutcome.Indeterminate)
                return CreateChargeResult.Indeterminate(connection.Id, customer, diagnostic: created.Diagnostic);
            if (!created.Success)
                return CreateChargeResult.Rejected(created.ErrorCode!, connection.Id, customer, created.Diagnostic);
            if (!Guid.TryParse(created.ProviderExternalId, out var createdId) || createdId == Guid.Empty)
                return CreateChargeResult.Indeterminate(connection.Id, customer);
            return CreateChargeResult.Completed(createdId.ToString(), customer, connection.Id);
        }
        catch (OperationCanceledException) when (customerCreated)
        {
            return CreateChargeResult.Indeterminate(connection.Id, customer, "CanceledAfterCustomerCreation");
        }
    }
}
