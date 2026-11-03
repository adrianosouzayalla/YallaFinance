using System.Text.Json;
using YallaFinance.Application.Features.Charges.CreateCharge;

using YallaFinance.Domain.Jobs;
using YallaFinance.Application.Features.Jobs.Shared;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Charges.IntegrateCharge;

public sealed class ChargeProcessingQueries(JobQueries queries)
{
    private static readonly HashSet<string> PublicErrorCodes = new(StringComparer.Ordinal)
    {
        "InvalidChargePayload", "ChargeNotFound", "InvalidChargeItems", "UnknownJobType",
        "InvalidJobPayload", "JobHandlerError", "InvalidHandlerResult",
        "ChargeIntegrationPartial", "ChargeIntegrationFailed"
    };

    public async Task<ChargeDetailsResult> EnrichAsync(
        ChargeDetailsResult charge,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(charge.TenantId, out var tenantId) || !Guid.TryParse(charge.Id, out var chargeId))
            return charge;
        Job? latestIntegrationJob = null;
        for (var jobPageNumber = 1; ; jobPageNumber++)
        {
            var jobPage = await queries.ListAsync(tenantId, new(
                new IntegrateChargeJob(chargeId).BuildKey(),
                IntegrateChargeJob.JobType),
                jobPageNumber, 100, cancellationToken);
            if (jobPage.Items.Count > 0)
                latestIntegrationJob = jobPage.Items[^1];
            if (jobPage.Items.Count < 100)
                break;
        }
        if (latestIntegrationJob is null)
            return charge;
        JobExecution? latestExecution = null;
        for (var executionPageNumber = 1; ; executionPageNumber++)
        {
            var executionPage = await queries.ListExecutionsAsync(
                tenantId,
                latestIntegrationJob.Id,
                executionPageNumber,
                100,
                cancellationToken);
            if (executionPage.Items.Count > 0)
                latestExecution = executionPage.Items[^1];
            if (executionPage.Items.Count < 100)
                break;
        }
        ChargeIntegrationSummary? chargeIntegrationSummary = null;
        if (latestExecution?.Result is { } json)
        {
            try
            {
                chargeIntegrationSummary = JsonSerializer.Deserialize<ChargeIntegrationSummary>(
                    json,
                    JsonSerializerOptions.Web);
            }
            catch (JsonException) { }
            if (chargeIntegrationSummary?.ChargeId != chargeId || chargeIntegrationSummary.Items is null
                || chargeIntegrationSummary.TotalItems != chargeIntegrationSummary.Items.Count
                || chargeIntegrationSummary.ConfirmedItems < 0 || chargeIntegrationSummary.RejectedItems < 0 || chargeIntegrationSummary.IndeterminateItems < 0
                || chargeIntegrationSummary.TotalItems != chargeIntegrationSummary.ConfirmedItems + chargeIntegrationSummary.RejectedItems + chargeIntegrationSummary.IndeterminateItems)
                chargeIntegrationSummary = null;
        }
        var storedJobErrorCode = latestExecution?.Error?.Split(':', 2)[0];
        var chargeProcessingStatus = new ChargeProcessingStatus(
            latestIntegrationJob.Id,
            latestIntegrationJob.Status.ToString(),
            chargeIntegrationSummary,
            latestIntegrationJob.Status == JobStatus.Failed
                ? storedJobErrorCode is not null && PublicErrorCodes.Contains(storedJobErrorCode) ? storedJobErrorCode : "ChargeIntegrationFailed"
                : null);
        // The persisted item links remain the source of confirmed external identities.
        return charge with
        {
            Processing = chargeProcessingStatus
        };
    }
}
