using YallaFinance.Infrastructure.Providers.Configuration;


using YallaFinance.Infrastructure.Common.Errors;
using YallaFinance.Infrastructure.Services.Financial.Contracts;
using YallaFinance.Infrastructure.Providers.Nibo.Financial.Responses;
using YallaFinance.Application.Services.Financial.Contracts;

namespace YallaFinance.Infrastructure.Providers.Nibo.Financial;

internal sealed partial class NiboFinancialProvider
{
    public async Task<ReceivableSettlementQueryResult> QuerySettlementAsync(
        SettleReceivableData settlementData,
        ProviderConnection financialConnection,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(settlementData.ProviderExternalId, out var scheduleId))
        {
            return new(
                ReceivableSettlementState.Rejected,
                ErrorCode: "InvalidScheduleId");
        }

        var catalogQuery = new FinancialCatalogQuery(
            settlementData.TenantId,
            Provider,
            financialConnection.Id,
            Take: 100);
        var expectedReceipts = settlementData.ExpectedReceipts;

        if (expectedReceipts is not { Count: > 0 } ||
            expectedReceipts.Any(receipt =>
                string.IsNullOrWhiteSpace(receipt.Identifier) ||
                receipt.AccountId == Guid.Empty ||
                receipt.Amount <= 0 ||
                decimal.Round(receipt.Amount, 2) != receipt.Amount ||
                receipt.Date == default) ||
            expectedReceipts.Select(receipt => receipt.Identifier)
                .Distinct(StringComparer.Ordinal)
                .Count() != expectedReceipts.Count ||
            !expectedReceipts.Any(receipt => receipt.Identifier == settlementData.Identifier))
        {
            return new(
                ReceivableSettlementState.ConflictingReceipt,
                ErrorCode: "SettlementPlanInvalid");
        }

        var expectedReceiptsByIdentifier = expectedReceipts.ToDictionary(
            receipt => receipt.Identifier,
            StringComparer.Ordinal);
        var receipts = new List<NiboReceiptResponse>();
        var seenReceiptIds = new HashSet<Guid>();
        var matchedExpectedIdentifiers = new HashSet<string>(StringComparer.Ordinal);

        // Consulta os recebimentos registrados na Nibo.
        while (true)
        {
            var receiptPage = await ReadCatalogAsync<NiboReceiptResponse, NiboReceiptResponse>(
                catalogQuery,
                financialConnection,
                (niboApi, accessToken) => niboApi.FindReceiptsAsync(
                    $"scheduleId eq {scheduleId:D}",
                    catalogQuery.Skip,
                    catalogQuery.Take,
                    "date,entryId",
                    accessToken,
                    cancellationToken),
                receipt => receipt,
                cancellationToken,
                diagnosticOperation: "ListReceipts");

            if (!receiptPage.Success)
            {
                return CreateSettlementQueryFailure(
                    receiptPage.ErrorCode ?? "ProviderQueryFailed",
                    receiptPage.Diagnostic);
            }

            foreach (var receipt in receiptPage.Items)
            {
                if (receipt is null ||
                    receipt.EntryId is not Guid receiptId ||
                    receiptId == Guid.Empty ||
                    !seenReceiptIds.Add(receiptId))
                {
                    return new(
                        ReceivableSettlementState.Indeterminate,
                        ErrorCode: "ProviderResponseInvalid");
                }

                expectedReceiptsByIdentifier.TryGetValue(
                    receipt.Identifier ?? string.Empty,
                    out var expectedByIdentifier);
                var expectedReceipt = expectedByIdentifier ??
                    (expectedReceipts.Count == 1 ? expectedReceipts[0] : null);

                if (expectedReceipt is null ||
                    !matchedExpectedIdentifiers.Add(expectedReceipt.Identifier) ||
                    receipt.ScheduleId != scheduleId ||
                    receipt.Account is null ||
                    receipt.Account.Id != expectedReceipt.AccountId ||
                    receipt.Account.IsDeleted is not false ||
                    receipt.Value != expectedReceipt.Amount ||
                    receipt.Date is null ||
                    DateOnly.FromDateTime(receipt.Date.Value.DateTime) != expectedReceipt.Date ||
                    receipt.IsVirtual is not false ||
                    receipt.IsTransfer is not false)
                {
                    return new(
                        ReceivableSettlementState.ConflictingReceipt,
                        ErrorCode: "ReceiptConflict");
                }

                receipts.Add(receipt);
            }

            // Continua a consulta caso existam outras páginas.
            var nextSkip = catalogQuery.Skip + receiptPage.Items.Count;

            if (receiptPage.Items.Count == 0 &&
                nextSkip < receiptPage.TotalCount)
            {
                return new(
                    ReceivableSettlementState.Indeterminate,
                    ErrorCode: "ProviderPaginationInconsistent");
            }

            if (nextSkip >= receiptPage.TotalCount)
            {
                break;
            }

            catalogQuery = catalogQuery with
            {
                Skip = nextSkip
            };
        }

        // Sem recebimento encontrado, consulta o estado do agendamento.
        var scheduleResponse = await GetReceivableAsync(
            new(
                settlementData.TenantId,
                Provider,
                scheduleId.ToString(),
                financialConnection.Id),
            financialConnection,
            cancellationToken);

        if (!scheduleResponse.Success)
        {
            return CreateSettlementQueryFailure(
                scheduleResponse.ErrorCode ?? "ProviderQueryFailed",
                scheduleResponse.Diagnostic);
        }

        var receivable = scheduleResponse.Items.FirstOrDefault();

        if (receivable is null ||
            receivable.ProviderExternalId != scheduleId.ToString() ||
            !Guid.TryParse(receivable.Reference, out var chargeItemId) ||
            chargeItemId != settlementData.ChargeItemId ||
            expectedReceipts.Sum(receipt => receipt.Amount) != receivable.Amount)
        {
            return new(
                ReceivableSettlementState.ConflictingReceipt,
                ErrorCode: "ReceivableConflict");
        }

        var matchingReceipts = receipts
            .Where(receipt => receipt.Identifier == settlementData.Identifier || expectedReceipts.Count == 1)
            .ToArray();

        if (matchingReceipts.Length > 1)
        {
            return new(
                ReceivableSettlementState.ConflictingReceipt,
                ErrorCode: "ReceiptConflict");
        }

        if (matchingReceipts.Length == 1)
        {
            return new(
                ReceivableSettlementState.MatchingReceipt,
                matchingReceipts[0].EntryId!.Value.ToString());
        }

        if (receivable is { IsActive: true, IsPaid: false })
        {
            return new(ReceivableSettlementState.Open);
        }

        if (receivable.IsPaid && receipts.Count == 0)
        {
            return new(
                ReceivableSettlementState.ConflictingReceipt,
                ErrorCode: "ReceiptNotVisible");
        }

        return new(
            ReceivableSettlementState.ConflictingReceipt,
            ErrorCode: "ReceiptConflict");
    }

    private static ReceivableSettlementQueryResult CreateSettlementQueryFailure(
        string errorCode,
        FinancialProviderDiagnostic? diagnostic = null)
    {
        var settlementState = errorCode is
            "Unauthorized" or "ProviderNotFound" or "ProviderRejected"
                ? ReceivableSettlementState.Rejected
                : ReceivableSettlementState.Indeterminate;

        return new(
            settlementState,
            ErrorCode: errorCode,
            Diagnostic: diagnostic);
    }
}
