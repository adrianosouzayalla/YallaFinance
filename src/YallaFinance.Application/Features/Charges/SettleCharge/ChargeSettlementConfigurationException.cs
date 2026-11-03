using YallaFinance.Application;
namespace YallaFinance.Application.Features.Charges.SettleCharge;

public sealed class ChargeSettlementConfigurationException(string code) : Exception(code)
{
    public string Code { get; } = code;
}
