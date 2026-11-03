



using System.Text.Json;
using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application.Services.Financial;
using YallaFinance.Application.Services.Financial.Contracts;
using YallaFinance.Application.Repositories.Charges;
using YallaFinance.Application.Services.Charges;
using YallaFinance.Application.Repositories.Charges.Contracts.Settlement;
using YallaFinance.Application.Services.Charges.Contracts;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Charges.SettleCharge;

public sealed class SettleChargeHandler(
    IJobExecutionContext jobExecutionContext,
    IChargeRepository charges,
    IChargeSettlementConfiguration settlementConfiguration,
    IFinancialReceivableSettlementService financialSettlementService
) : IJobHandler<SettleChargePayload>
{
    public async Task<JobHandlerResult> HandleAsync(
        SettleChargePayload settlementPayload,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (settlementPayload.ChargeId == Guid.Empty ||
            jobExecutionContext.TenantId == Guid.Empty ||
            jobExecutionContext.JobId == Guid.Empty ||
            jobExecutionContext.ExecutionId == Guid.Empty)
        {
            return CreateFailureResult(
                "InvalidChargePayload",
                "Cobrança e execução são obrigatórias.");
        }

        var chargeToSettle = await charges.LoadAsync(
            jobExecutionContext.TenantId,
            settlementPayload.ChargeId,
            cancellationToken);

        if (chargeToSettle?.IsSettled == true)
        {
            return new(
                true,
                JsonSerializer.SerializeToElement(new
                {
                    chargeToSettle.ChargeId,
                    Outcome = "Completed",
                    AlreadySettled = true
                }, JsonSerializerOptions.Web));
        }

        if (chargeToSettle is null)
        {
            return CreateFailureResult(
                "ChargeNotFound",
                "Cobrança não encontrada.");
        }

        var validationErrorCode = ChargeSettlementRules.Validate(
            chargeToSettle,
            jobExecutionContext.TenantId,
            settlementPayload.ChargeId);

        if (validationErrorCode is not null)
        {
            return CreateFailureResult(
                validationErrorCode,
                "Confira os itens e pagamentos da cobrança. A baixa exige um recebimento integral.");
        }

        ChargeSettlementCheckpoint settlementCheckpoint;
        // Each attempt builds its plan from current charge/payment data.
        // Job history is diagnostic and never controls a later attempt.
        var currentAllocations = ChargeSettlementRules.Allocate(chargeToSettle);
        var currentItems = new List<ChargeSettlementItemCheckpoint>(currentAllocations.Count);
        foreach (var allocation in currentAllocations)
        {
            ChargeSettlementTarget target;
            try
            {
                target = await settlementConfiguration.ResolveAsync(
                    allocation.Payment,
                    allocation.Item.FinancialConnectionId,
                    cancellationToken);
            }
            catch (ChargeSettlementConfigurationException exception)
            {
                return CreateFailureResult(
                    exception.Code,
                    "Confira a conexão financeira, o mapeamento da conta e o fuso do recebimento.");
            }

            if (target.ConnectionId == Guid.Empty || target.ReceiptDate == default)
                return CreateFailureResult("SettlementConfigurationInvalid", "Configuração de baixa inválida.");

            currentItems.Add(new(
                new SettleReceivableData(
                    chargeToSettle.TenantId,
                    target.ConnectionId,
                    allocation.Item.Provider!,
                    allocation.Item.Id,
                    allocation.Payment.Id,
                    allocation.Item.ProviderExternalId!,
                    Guid.Empty,
                    allocation.Amount,
                    target.ReceiptDate,
                    ChargeSettlementRules.Identifier(allocation.Payment.Id, allocation.Item.Id))));
        }

        var currentPayments = currentAllocations
            .Select(allocation => allocation.Payment)
            .DistinctBy(payment => payment.Id)
            .ToArray();
        var representativeCurrentPayment = currentPayments[^1];
        var currentItemsByIdentity = currentItems.ToDictionary(
            item => (item.Data.PaymentTransactionId, item.Data.ChargeItemId));

        settlementCheckpoint = new(
            2,
            0,
            chargeToSettle.ChargeId,
            representativeCurrentPayment.Id,
            representativeCurrentPayment.BankConnectionId,
            representativeCurrentPayment.ReceivedAt,
            currentItems.Select(item => currentItemsByIdentity[
                (item.Data.PaymentTransactionId, item.Data.ChargeItemId)]).ToArray(),
            currentPayments);

        var settlementItems = settlementCheckpoint.Items.ToArray();

        // Save the checkpoint assembled for the current execution.
        // Persiste a versão atual do checkpoint.
        async Task SaveCheckpointAsync()
        {
            var expectedRevision = settlementCheckpoint.Revision;

            settlementCheckpoint = settlementCheckpoint with
            {
                Revision = expectedRevision + 1,
                Items = settlementItems.ToArray()
            };

            await charges.SaveAsync(
                jobExecutionContext,
                settlementCheckpoint,
                expectedRevision,
                cancellationToken);
        }

        async Task<SettleReceivableData> BuildProviderDataAsync(
            int currentItemIndex,
            Guid? currentAccountOverride = null)
        {
            var currentItem = settlementItems[currentItemIndex];
            var expectedReceipts = new List<SettleReceivableExpectation>();
            Guid? currentAccount = null;

            foreach (var relatedSettlementItem in settlementItems.Where(settlementItem =>
                settlementItem.Data.ChargeItemId == currentItem.Data.ChargeItemId))
            {
                Guid accountId;
                if (relatedSettlementItem.Data.Identifier == currentItem.Data.Identifier && currentAccountOverride is { } overrideAccount)
                {
                    accountId = overrideAccount;
                }
                else
                {
                    var checkpointPayment = settlementCheckpoint.Payments?.SingleOrDefault(savedPayment =>
                        savedPayment.Id == relatedSettlementItem.Data.PaymentTransactionId);
                    if (checkpointPayment is null)
                        throw new ChargeSettlementConfigurationException("SettlementPaymentMismatch");

                    accountId = await settlementConfiguration.ResolveReceiptAccountAsync(
                        checkpointPayment,
                        relatedSettlementItem.Data.ConnectionId,
                        cancellationToken);
                    if (accountId == Guid.Empty)
                        throw new ChargeSettlementConfigurationException("ReceiptAccountMappingInvalid");
                }

                if (relatedSettlementItem.Data.Identifier == currentItem.Data.Identifier)
                    currentAccount = accountId;

                expectedReceipts.Add(new(
                    relatedSettlementItem.Data.Identifier,
                    accountId,
                    relatedSettlementItem.Data.Amount,
                    relatedSettlementItem.Data.ReceiptDate));
            }

            if (currentAccount is null)
                throw new ChargeSettlementConfigurationException("SettlementPlanInvalid");

            return currentItem.Data with
            {
                ProviderAccountId = currentAccount.Value,
                ExpectedReceipts = expectedReceipts
            };
        }

        await SaveCheckpointAsync();

        // Consulta todos os destinos antes de iniciar qualquer nova baixa,
        // inclusive aqueles de tentativas anteriores parcialmente concluídas.
        var hasBlockedSettlement = false;

        for (var settlementItemIndex = 0; settlementItemIndex < settlementItems.Length; settlementItemIndex++)
        {
            var settlementItem = settlementItems[settlementItemIndex];

            ReceivableSettlementQueryResult settlementQueryResult;
            SettleReceivableData settlementQueryData;
            try
            {
                settlementQueryData = await BuildProviderDataAsync(settlementItemIndex);
                settlementQueryResult = await financialSettlementService.QueryAsync(
                    settlementQueryData,
                    cancellationToken);
                // Persist the data used for this reconciliation, including the account
                // mapping resolved from the current database configuration.
                settlementItem = settlementItem with
                {
                    Data = settlementQueryData
                };
                settlementItems[settlementItemIndex] = settlementItem;
            }
            catch (ChargeSettlementConfigurationException exception)
            {
                hasBlockedSettlement = true;
                settlementItems[settlementItemIndex] = settlementItem with
                {
                    State = "Rejected",
                    ErrorCode = exception.Code
                };
                continue;
            }

            if (settlementQueryResult.State == ReceivableSettlementState.MatchingReceipt &&
                ValidReceipt(settlementQueryResult.ProviderReceiptId))
            {
                settlementItems[settlementItemIndex] = settlementItem with
                {
                    Data = settlementQueryData,
                    State = "Completed",
                    ProviderReceiptId = settlementQueryResult.ProviderReceiptId,
                    ErrorCode = null,
                    Diagnostic = null
                };
            }
            else if (settlementQueryResult.State == ReceivableSettlementState.Open)
            {
                settlementItems[settlementItemIndex] = settlementItem with
                {
                    State = "NotSent",
                    ErrorCode = null,
                    Diagnostic = null
                };
            }
            else
            {
                hasBlockedSettlement = true;
                var queryIsIndeterminate = settlementQueryResult.State == ReceivableSettlementState.Indeterminate ||
                    settlementQueryResult.State == ReceivableSettlementState.MatchingReceipt &&
                    !ValidReceipt(settlementQueryResult.ProviderReceiptId);

                settlementItems[settlementItemIndex] = settlementItem with
                {
                    State = queryIsIndeterminate ? "Indeterminate" : "Rejected",

                    ErrorCode = settlementQueryResult.ErrorCode ??
                        (queryIsIndeterminate ? "ProviderOutcomeUnknown" : "SettlementNotConfirmed"),

                    Diagnostic = settlementQueryResult.Diagnostic
                };
            }
        }

        await SaveCheckpointAsync();

        if (hasBlockedSettlement)
        {
            return Result(settlementCheckpoint);
        }

        // Executa a baixa somente dos itens ainda não concluídos.
        for (var settlementItemIndex = 0; settlementItemIndex < settlementItems.Length; settlementItemIndex++)
        {
            if (settlementItems[settlementItemIndex].State == "Completed")
            {
                continue;
            }

            var settlementItem = settlementItems[settlementItemIndex];
            var checkpointPayment = settlementCheckpoint.Payments?.SingleOrDefault(
                savedPayment => savedPayment.Id == settlementItem.Data.PaymentTransactionId);
            if (checkpointPayment is null)
            {
                settlementItems[settlementItemIndex] = settlementItem with
                {
                    State = "Rejected",
                    ErrorCode = "SettlementPaymentMismatch"
                };
                await SaveCheckpointAsync();
                break;
            }

            Guid currentAccountId;
            try
            {
                // Resolve the provider account mapping immediately before each new send.
                currentAccountId = await settlementConfiguration.ResolveReceiptAccountAsync(
                    checkpointPayment,
                    settlementItem.Data.ConnectionId,
                    cancellationToken);
            }
            catch (ChargeSettlementConfigurationException exception)
            {
                settlementItems[settlementItemIndex] = settlementItem with
                {
                    State = "Rejected",
                    ErrorCode = exception.Code
                };
                await SaveCheckpointAsync();
                break;
            }

            if (currentAccountId == Guid.Empty)
            {
                settlementItems[settlementItemIndex] = settlementItem with
                {
                    State = "Rejected",
                    ErrorCode = "SettlementConfigurationInvalid"
                };
                await SaveCheckpointAsync();
                break;
            }

            SettleReceivableData attemptData;
            try
            {
                attemptData = await BuildProviderDataAsync(settlementItemIndex, currentAccountId);
            }
            catch (ChargeSettlementConfigurationException exception)
            {
                settlementItems[settlementItemIndex] = settlementItem with
                {
                    State = "Rejected",
                    ErrorCode = exception.Code
                };
                await SaveCheckpointAsync();
                break;
            }

            settlementItems[settlementItemIndex] = settlementItems[settlementItemIndex] with
            {
                Data = attemptData,
                State = "Sending",
                ErrorCode = null,
                Diagnostic = null
            };

            await SaveCheckpointAsync();

            var settlementResult = await financialSettlementService.SettleAsync(
                attemptData,
                cancellationToken);

            if (settlementResult.Success &&
                (settlementResult.ConnectionId != settlementItems[settlementItemIndex].Data.ConnectionId ||
                    !ValidReceipt(settlementResult.ProviderReceiptId)))
            {
                settlementResult = new(
                    FinancialIntegrationOutcome.Indeterminate,
                    ErrorCode: "ProviderResponseInvalid");
            }

            settlementItems[settlementItemIndex] = settlementItems[settlementItemIndex] with
            {
                State = settlementResult.Outcome.ToString(),
                ProviderReceiptId = settlementResult.ProviderReceiptId,
                ErrorCode = settlementResult.ErrorCode,
                Diagnostic = settlementResult.Diagnostic
            };

            await SaveCheckpointAsync();

            if (!settlementResult.Success)
            {
                break;
            }
        }

        // Conclui a cobrança somente quando todos os itens estão confirmados.
        if (settlementItems.All(settlementItem => settlementItem.State == "Completed"))
        {
            await charges.CompleteChargeAsync(
                chargeToSettle.TenantId,
                chargeToSettle.ChargeId,
                settlementCheckpoint.PaymentTransactionId,
                settlementCheckpoint.Payments!
                    .Select(payment => payment.Id)
                    .ToArray(),
                cancellationToken);
        }

        return Result(settlementCheckpoint);
    }

    private static bool ValidReceipt(string? providerReceiptId)
    {
        return ChargeSettlementRules.IsValidExternalIdentifier(providerReceiptId);
    }

    private static JobHandlerResult CreateFailureResult(
        string errorCode,
        string errorMessage)
    {
        return new(
            false,
            Error: new(errorCode, errorMessage));
    }

    private static JobHandlerResult Result(
        ChargeSettlementCheckpoint settlementCheckpoint)
    {
        var completedItemCount = settlementCheckpoint.Items.Count(
            settlementItem => settlementItem.State == "Completed");

        var indeterminateItemCount = settlementCheckpoint.Items.Count(
            settlementItem => settlementItem.State is "Sending" or "Indeterminate");

        var allChargeItemsCompleted = completedItemCount == settlementCheckpoint.Items.Count;

        var settlementOutcome = allChargeItemsCompleted
            ? "Completed"
            : indeterminateItemCount > 0
                ? "Indeterminate"
                : completedItemCount > 0
                    ? "Partial"
                    : "Rejected";

        var settlementSummary = JsonSerializer.SerializeToElement(new
        {
            settlementCheckpoint.ChargeId,
            settlementCheckpoint.PaymentTransactionId,

            PaymentTransactionIds = settlementCheckpoint.Payments?
                .Select(payment => payment.Id),

            Outcome = settlementOutcome,

            TotalReceipts = settlementCheckpoint.Items.Count,

            ConfirmedItems = completedItemCount,

            RejectedItems = settlementCheckpoint.Items.Count(
                settlementItem => settlementItem.State == "Rejected"),

            IndeterminateItems = indeterminateItemCount,

            Items = settlementCheckpoint.Items.Select(settlementItem => new
            {
                settlementItem.Data.ChargeItemId,
                Outcome = settlementItem.State,
                settlementItem.ProviderReceiptId,
                settlementItem.ErrorCode,
                settlementItem.Diagnostic
            })
        }, JsonSerializerOptions.Web);

        return new(
            allChargeItemsCompleted,
            settlementSummary,
            allChargeItemsCompleted
                ? null
                : new(
                    indeterminateItemCount > 0
                        ? "ChargeSettlementIndeterminate"
                        : completedItemCount > 0
                            ? "ChargeSettlementPartial"
                            : "ChargeSettlementFailed",
                    "A baixa da cobrança ainda não foi confirmada integralmente."),
            JsonSerializer.SerializeToElement(
                settlementCheckpoint,
                JsonSerializerOptions.Web));
    }
}
