using YallaFinance.Infrastructure.Providers.Configuration;
using System.Net;
using System.Text.Json;
using Refit;

using YallaFinance.Infrastructure.Common.Errors;
using YallaFinance.Infrastructure.Providers.Nibo.Financial.Requests;
using YallaFinance.Application.Services.Financial.Contracts;
using YallaFinance.Application.Features.Charges.SettleCharge;
using YallaFinance.Application.Repositories.Charges.Contracts.Settlement;

namespace YallaFinance.Infrastructure.Providers.Nibo.Financial;

internal sealed partial class NiboFinancialProvider
{
    public async Task<Guid> ResolveReceiptAccountAsync(
        ChargeSettlementPayment payment,
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        var account = await connections.ResolveMappingAsync(
            payment.TenantId,
            connection.Id,
            "ReceiptAccount",
            payment.PaymentMethodId.ToString("D"),
            cancellationToken);

        if (account is null)
            throw new ChargeSettlementConfigurationException("ReceiptAccountMappingMissing");

        if (!Guid.TryParse(account, out var accountId) || accountId == Guid.Empty)
            throw new ChargeSettlementConfigurationException("ReceiptAccountMappingInvalid");

        return accountId;
    }

    public async Task<SettleReceivableResult> SettleAsync(
        SettleReceivableData data,
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        var settlementStatus = await QuerySettlementAsync(data, connection, cancellationToken);
        if (settlementStatus.State == ReceivableSettlementState.MatchingReceipt)
            return new(
                FinancialIntegrationOutcome.Completed,
                connection.Id,
                settlementStatus.ProviderReceiptId,
                AlreadyCompleted: true);
        if (settlementStatus.State != ReceivableSettlementState.Open)
            return new(
                settlementStatus.State == ReceivableSettlementState.Indeterminate ? FinancialIntegrationOutcome.Indeterminate
                    : FinancialIntegrationOutcome.Rejected,
                connection.Id,
                ErrorCode: settlementStatus.ErrorCode,
                Diagnostic: settlementStatus.Diagnostic);
        var options = await configuration.ReadAsync(connection, cancellationToken);
        using var client = clients.Create(options);
        var niboApi = RestService.For<INiboApi>(
            client,
            new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(JsonSerializerOptions.Web)
            });
        await coordinator.AcquireHttpSlotAsync(options.ApiToken, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        int? statusCode = null;
        try
        {
            using var response = await niboApi.SettleReceivableAsync(Guid.Parse(data.ProviderExternalId),
                new SettleNiboReceivableRequest(
                    data.ProviderAccountId,
                    data.ReceiptDate,
                    data.Identifier,
                    data.Amount),
                options.ApiToken, cancellationToken);
            statusCode = (int)response.StatusCode;
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                coordinator.DelayRequests(
                    options.ApiToken,
                    response.Headers.RetryAfter?.Delta
                    ?? (response.Headers.RetryAfter?.Date - DateTimeOffset.UtcNow) ?? TimeSpan.FromSeconds(1));
            if (response.StatusCode != HttpStatusCode.OK)
            {
                var httpStatusCode = (int)response.StatusCode;
                var providerError = await ReadProviderErrorAsync(response.Content, cancellationToken);
                var diagnostic = CreateFinancialProviderDiagnostic(
                    "SettleReceivable",
                    "POST",
                    "/schedules/credit/{scheduleId}/receipts",
                    httpStatusCode) with
                {
                    ProviderErrorCode = providerError.Code,
                    ProviderErrorMessage = providerError.Message
                };
                return httpStatusCode is >= 400 and < 500 && httpStatusCode != 408
                    ? new(
                        FinancialIntegrationOutcome.Rejected,
                        connection.Id,
                        ErrorCode: MapHttpStatusToErrorCode(response.StatusCode),
                        Diagnostic: diagnostic)
                    : new(
                        FinancialIntegrationOutcome.Indeterminate,
                        connection.Id,
                        ErrorCode: "ProviderOutcomeUnknown",
                        Diagnostic: diagnostic);
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var providerReceiptId = Guid.TryParse(responseBody.Trim().Trim('"'), out var parsedReceiptId)
                ? parsedReceiptId.ToString()
                : null;

            // The NIBO response returns the receipt ID; confirm it against the receivable before accepting it.
            var confirmation = await QuerySettlementAsync(data, connection, cancellationToken);
            return confirmation.State == ReceivableSettlementState.MatchingReceipt
                ? new(FinancialIntegrationOutcome.Completed, connection.Id, confirmation.ProviderReceiptId)
                : confirmation.ErrorCode == "ReceiptNotVisible" && providerReceiptId is not null
                    ? new(FinancialIntegrationOutcome.Completed, connection.Id, providerReceiptId)
                    : new(
                        FinancialIntegrationOutcome.Indeterminate,
                        connection.Id,
                        ErrorCode: "SettlementNotConfirmed",
                        Diagnostic: confirmation.Diagnostic);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or OperationCanceledException
            or ApiException or ApiRequestException or JsonException)
        {
            return new(
                FinancialIntegrationOutcome.Indeterminate,
                connection.Id,
                ErrorCode: "ProviderOutcomeUnknown",
                Diagnostic: CreateFinancialProviderDiagnostic(
                    "SettleReceivable",
                    "POST",
                    "/schedules/credit/{scheduleId}/receipts",
                    statusCode,
                    exception));
        }
    }
}
