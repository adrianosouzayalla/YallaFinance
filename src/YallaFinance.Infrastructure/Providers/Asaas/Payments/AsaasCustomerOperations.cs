using System.Text.Json;
using YallaFinance.Application.Services.Payments.Contracts;
using YallaFinance.Application.Services.Payments.Pix.Contracts;
using YallaFinance.Infrastructure.Providers.Configuration;

namespace YallaFinance.Infrastructure.Providers.Asaas.Payments;

internal sealed class AsaasCustomerOperations(AsaasApiTransport transport)
{
    public async Task<AsaasApiResult<AsaasCustomer>> ResolveCustomerAsync(
        PaymentChargePayer payer,
        Guid tenantId,
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        var taxId = NormalizeTaxId(payer.TaxId);
        if (tenantId == Guid.Empty || !Text(payer.Name) || !TaxId(taxId))
            return new(AsaasApiOutcome.Rejected, default, "PixPayerInvalid");

        var customerReference = payer.CustomerId is { } customerId && customerId != Guid.Empty
            ? $"yalla:{tenantId:N}:customer:{customerId:N}"
            : null;
        if (customerReference is not null)
        {
            var referencedCustomers = await FindByReferenceAsync(customerReference, 0, 10,
                connection, cancellationToken);
            if (referencedCustomers.Outcome != AsaasApiOutcome.Completed || referencedCustomers.Value is null)
                return Propagate<AsaasCustomerPage, AsaasCustomer>(referencedCustomers);
            if (referencedCustomers.Value.HasMore)
                return new(AsaasApiOutcome.Indeterminate, default, "ProviderPaginationIncomplete");
            if (referencedCustomers.Value.Items.Count > 0)
            {
                var match = referencedCustomers.Value.Items.FirstOrDefault(customer =>
                    string.Equals(NormalizeTaxId(customer.Document), taxId, StringComparison.Ordinal));
                return match is null
                    ? new(AsaasApiOutcome.Rejected, default, "ProviderCustomerMismatch")
                    : new(AsaasApiOutcome.Completed, match, Diagnostic: referencedCustomers.Diagnostic);
            }
        }

        var customersByTaxId = await FindByTaxIdAsync(taxId, connection, cancellationToken);
        if (customersByTaxId.Outcome != AsaasApiOutcome.Completed || customersByTaxId.Value is null)
            return Propagate<AsaasCustomerPage, AsaasCustomer>(customersByTaxId);
        if (customersByTaxId.Value.Items.Count > 0)
        {
            var match = customersByTaxId.Value.Items.FirstOrDefault(customer =>
                string.Equals(NormalizeTaxId(customer.Document), taxId, StringComparison.Ordinal));
            return match is null
                ? new(AsaasApiOutcome.Indeterminate, default, "ProviderCustomerMismatch")
                : new(AsaasApiOutcome.Completed, match, Diagnostic: customersByTaxId.Diagnostic);
        }

        return await CreateAsync(new AsaasCreateCustomer(
            payer.Name,
            taxId,
            null,
            null,
            customerReference), connection, cancellationToken);
    }

    public Task<AsaasApiResult<AsaasCustomer>> ResolvePixCustomerAsync(
        PixChargePayer payer,
        Guid tenantId,
        ProviderConnection connection,
        CancellationToken cancellationToken) => ResolveCustomerAsync(
            new PaymentChargePayer(payer.TaxId, payer.Name, payer.CustomerId),
            tenantId,
            connection,
            cancellationToken);

    public Task<AsaasApiResult<AsaasCustomerPage>> FindByReferenceAsync(
        string reference,
        int offset,
        int limit,
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        if (!ValidReference(reference) || offset < 0 || limit is < 1 or > 100)
            return Invalid<AsaasCustomerPage>(cancellationToken);

        var path = "customers?externalReference=" + Uri.EscapeDataString(reference)
            + "&offset=" + offset.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + "&limit=" + limit.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return transport.SendAsync(HttpMethod.Get, path, null, connection,
            json => ReadPage(json, offset, limit), cancellationToken);
    }

    public Task<AsaasApiResult<AsaasCustomerPage>> FindByTaxIdAsync(
        string taxId,
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        if (!TaxId(taxId))
            return Invalid<AsaasCustomerPage>(cancellationToken);

        var path = "customers?cpfCnpj=" + Uri.EscapeDataString(taxId) + "&offset=0&limit=100";
        return transport.SendAsync(HttpMethod.Get, path, null, connection,
            json => ReadPage(json, 0, 100), cancellationToken);
    }

    public Task<AsaasApiResult<AsaasCustomer>> CreateAsync(
        AsaasCreateCustomer customer,
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        var phone = NormalizePhone(customer.Phone);
        if (!Text(customer.Name) || !TaxId(customer.Document)
            || customer.Reference is not null && !ValidReference(customer.Reference)
            || customer.Phone is not null && phone is null)
            return Invalid<AsaasCustomer>(cancellationToken);

        return transport.SendAsync(HttpMethod.Post, "customers", new
        {
            name = customer.Name,
            cpfCnpj = customer.Document,
            email = customer.Email,
            phone = phone?.Length == 10 ? phone : null,
            mobilePhone = phone?.Length == 11 ? phone : null,
            externalReference = customer.Reference,
            notificationDisabled = true
        }, connection, json =>
        {
            var result = ReadCustomer(json);
            if (customer.Reference is not null && result.Reference != customer.Reference
                || !string.Equals(NormalizeTaxId(result.Document), customer.Document, StringComparison.Ordinal))
                throw new JsonException();
            return result;
        }, cancellationToken);
    }

    private static AsaasCustomerPage ReadPage(JsonElement json, int offset, int limit)
    {
        var items = json.GetProperty("data");
        if (items.ValueKind != JsonValueKind.Array || items.GetArrayLength() > limit)
            throw new JsonException();
        return new(items.EnumerateArray().Select(ReadCustomer).ToArray(),
            json.GetProperty("hasMore").GetBoolean(), offset, limit);
    }

    private static AsaasCustomer ReadCustomer(JsonElement json) => new(
        RequiredId(json, "id"),
        OptionalText(json, "externalReference"),
        OptionalText(json, "cpfCnpj"));

    private static string RequiredId(JsonElement json, string propertyName)
    {
        var value = json.GetProperty(propertyName).GetString();
        return value is { Length: > 0 and <= 100 }
            && value.All(character => char.IsAsciiLetterOrDigit(character) || character == '_')
            ? value : throw new JsonException();
    }

    private static string? OptionalText(JsonElement json, string propertyName) =>
        json.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() : null;

    private static string? NormalizePhone(string? value)
    {
        if (value is null || value.Any(character => !char.IsAsciiDigit(character)
            && character is not ('+' or '-' or '(' or ')' or ' ')))
            return null;
        var phone = new string(value.Where(char.IsAsciiDigit).ToArray());
        if (phone.Length is 12 or 13 && phone.StartsWith("55", StringComparison.Ordinal))
            phone = phone[2..];
        return phone.Length is 10 or 11 ? phone : null;
    }

    private static Task<AsaasApiResult<T>> Invalid<T>(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new AsaasApiResult<T>(AsaasApiOutcome.Rejected, default, "Validation"));
    }

    private static bool ValidReference(string? value) => Text(value) && value!.Length <= 200;

    private static AsaasApiResult<TOutput> Propagate<TInput, TOutput>(AsaasApiResult<TInput> result) =>
        new(result.Outcome, default, result.ErrorCode, result.Diagnostic);

    private static bool TaxId(string? value) => value is { Length: 11 or 14 }
        && value.All(char.IsAsciiDigit);

    private static string NormalizeTaxId(string? value) => value is null
        ? string.Empty
        : new string(value.Where(char.IsAsciiDigit).ToArray());

    private static bool Text(string? value) => !string.IsNullOrWhiteSpace(value) && !value.Any(char.IsControl);
}

internal sealed record AsaasCreateCustomer(
    string Name,
    string Document,
    string? Email,
    string? Phone,
    string? Reference);

internal sealed record AsaasCustomer(string Id, string? Reference, string? Document)
{
    public override string ToString() => nameof(AsaasCustomer);
}

internal sealed record AsaasCustomerPage(
    IReadOnlyList<AsaasCustomer> Items,
    bool HasMore,
    int Offset,
    int Limit);
