using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application;


namespace YallaFinance.Application.Features.Charges.IntegrateCharge;

public sealed record IntegrateChargeJob(Guid ChargeId) : IJobDefinition<IntegrateChargePayload>
{
    public const string JobType = "FINANCIAL_CHARGE_CREATE_V1";
    public const string JobTitle = "Criar cobrança no sistema financeiro externo";
    public string Type => JobType;
    public string BuildKey() => $"charge:{ChargeId:D}";
    public IntegrateChargePayload BuildPayload() => new(ChargeId);
}
