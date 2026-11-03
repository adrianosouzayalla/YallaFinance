using System.Text.Json;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Services.Financial.Contracts;
using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application.Repositories.Charges;
using YallaFinance.Application.Services.Financial;
using YallaFinance.Application;





namespace YallaFinance.Application.Features.Charges.IntegrateCharge;

public sealed class IntegrateChargeHandler(
    IChargeRepository charges,
    IFinancialIntegrationService financial,
    ICurrentTenantContext tenant) : IJobHandler<IntegrateChargePayload>
{
    public async Task<JobHandlerResult> HandleAsync(
        IntegrateChargePayload payload,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (payload.ChargeId == Guid.Empty ||
            !Guid.TryParse(tenant.TenantId, out var tenantId) ||
            tenantId == Guid.Empty)
            return CreateFailureResult("InvalidChargePayload", "Cobrança e tenant são obrigatórios.");

        var charge = await charges.GetAsync(
            tenantId.ToString(),
            payload.ChargeId.ToString(),
            cancellationToken);
        if (charge is null)
            return CreateFailureResult("ChargeNotFound", "Cobrança não encontrada.");

        if (charge.TenantId != tenantId.ToString() ||
            charge.Id != payload.ChargeId.ToString() ||
            charge.Items is not { Count: > 0 })
            return CreateFailureResult("InvalidChargeItems", "Itens da cobrança inválidos.");

        var pendingRegistrations = await charges.GetPendingAsync(tenantId.ToString(), charge.Id, cancellationToken);

        var expectedUnlinkedChargeItemIds = charge.Items.Where(chargeItem => string.IsNullOrWhiteSpace(chargeItem.ProviderExternalId))
            .Select(chargeItem => Guid.Parse(chargeItem.Id)).ToHashSet();

        if (pendingRegistrations.Select(pendingRegistration => pendingRegistration.ItemId).Distinct().Count() != pendingRegistrations.Count
            || !expectedUnlinkedChargeItemIds.SetEquals(pendingRegistrations.Select(pendingRegistration => pendingRegistration.ItemId))
            || pendingRegistrations.Any(pendingRegistration => pendingRegistration.Data.TenantId != tenantId ||
                pendingRegistration.Data.ChargeId != pendingRegistration.ItemId))
            return CreateFailureResult("InvalidChargeItems", "Itens da cobrança inconsistentes.");

        var pendingRegistrationsByChargeItemId = pendingRegistrations.ToDictionary(pendingRegistration => pendingRegistration.ItemId);

        var integrationOutcomes = new List<ChargeIntegrationItemResult>();

        foreach (var chargeItem in charge.Items.OrderBy(chargeItem => chargeItem.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var chargeItemId = Guid.Parse(chargeItem.Id);
            if (!string.IsNullOrWhiteSpace(chargeItem.ProviderExternalId))
            {
                if (string.IsNullOrWhiteSpace(chargeItem.Provider))
                    return CreateFailureResult("InvalidChargeItems", "Vínculo financeiro inválido.");
                integrationOutcomes.Add(new(chargeItemId, "Completed", AlreadyLinked: true));
                continue;
            }
            var pendingRegistration = pendingRegistrationsByChargeItemId[chargeItemId];

            var chargeCreationResult = await financial.CreateChargeAsync(pendingRegistration.Data, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (chargeCreationResult.Success &&
                (string.IsNullOrWhiteSpace(chargeCreationResult.Provider) ||
                    string.IsNullOrWhiteSpace(chargeCreationResult.ProviderExternalId)))
                chargeCreationResult = chargeCreationResult with
                {
                    Outcome = FinancialIntegrationOutcome.Indeterminate,
                    ErrorCode = "ProviderResponseInvalid"
                };

            await charges.RecordProviderResultAsync(
                tenantId.ToString(),
                chargeItemId,
                chargeCreationResult.Provider ?? pendingRegistration.Provider,
                chargeCreationResult.Success ? chargeCreationResult.ProviderExternalId : null,
                cancellationToken);

            integrationOutcomes.Add(new(
                chargeItemId,
                chargeCreationResult.Outcome.ToString(),
                ErrorCode: chargeCreationResult.Success ? null : chargeCreationResult.ErrorCode,
                ConnectionId: chargeCreationResult.Success ? chargeCreationResult.ConnectionId : null,
                Diagnostic: chargeCreationResult.Success ? null : chargeCreationResult.Diagnostic));
        }
        var confirmedItemCount = integrationOutcomes.Count(outcome => outcome.Outcome == "Completed");
        var rejectedItemCount = integrationOutcomes.Count(outcome => outcome.Outcome == "Rejected");
        var indeterminateItemCount = integrationOutcomes.Count(outcome => outcome.Outcome == "Indeterminate");
        var allChargeItemsCompleted = confirmedItemCount == integrationOutcomes.Count;
        var integrationSummary = new ChargeIntegrationSummary(
            payload.ChargeId,
            allChargeItemsCompleted ? "Completed" : indeterminateItemCount > 0 ? "Indeterminate" : confirmedItemCount > 0 ? "Partial" : "Rejected",
            integrationOutcomes.Count,
            confirmedItemCount,
            rejectedItemCount,
            indeterminateItemCount,
            integrationOutcomes);
        return new(
            allChargeItemsCompleted,
            JsonSerializer.SerializeToElement(integrationSummary, JsonSerializerOptions.Web),
            allChargeItemsCompleted ? null : new JobError(
                confirmedItemCount > 0 ? "ChargeIntegrationPartial" : "ChargeIntegrationFailed",
                indeterminateItemCount > 0 ? "Há itens com resultado financeiro incerto." : "Há itens sem integração confirmada."));
    }

    private static JobHandlerResult CreateFailureResult(string code, string message) => new(
        false,
        Error: new(
            code,
            message));
}
