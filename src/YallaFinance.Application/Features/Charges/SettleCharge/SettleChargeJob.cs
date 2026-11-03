using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application;


namespace YallaFinance.Application.Features.Charges.SettleCharge;

public sealed record SettleChargeJob(Guid ChargeId) : IJobDefinition<SettleChargePayload>
{
    public const string JobType = "FINANCIAL_CHARGE_SETTLE_V1";
    public const string JobTitle = "Baixar cobrança no sistema financeiro externo";
    public string Type => JobType;
    public string BuildKey() => $"charge:{ChargeId:D}";
    public SettleChargePayload BuildPayload() => new(ChargeId);
}
