using YallaFinance.Infrastructure.Providers.Configuration;
using YallaFinance.Infrastructure.Providers;
using YallaFinance.Infrastructure.Providers.Financial;
using YallaFinance.Application.Repositories.Connections;


using YallaFinance.Infrastructure.Common.Errors;
using YallaFinance.Application.Services.Charges;
using YallaFinance.Application.Services.Charges.Contracts;
using YallaFinance.Application.Repositories.Charges.Contracts.Settlement;
using YallaFinance.Application.Features.Charges.SettleCharge;
using YallaFinance.Application.Repositories.Settings;

namespace YallaFinance.Infrastructure.Services.Financial;

internal sealed class FinancialSettlementConfiguration(
    ICompanyConnectionRepository connections,
    IProviderResolver<IFinancialReceivableSettlementProvider> settlementProviders,
    ITenantSettingRepository settings) : IChargeSettlementConfiguration
{
    public async Task<ChargeSettlementTarget> ResolveAsync(
        ChargeSettlementPayment payment,
        Guid? preferredConnectionId,
        CancellationToken cancellationToken)
    {
        if (payment.TenantId == Guid.Empty || payment.BankConnectionId == Guid.Empty || payment.PaymentMethodId == Guid.Empty || payment.Id == Guid.Empty
            || payment.ReceivedAt == default)
            throw new ChargeSettlementConfigurationException("SettlementPaymentMismatch");
        var bank = await connections.GetRuntimeAsync(payment.TenantId, payment.BankConnectionId, cancellationToken);
        if (bank is null || bank.TenantId != payment.TenantId || bank.Id != payment.BankConnectionId
            || bank.Group != "PaymentProvider")
            throw new ChargeSettlementConfigurationException("PaymentConnectionUnavailable");
        var connection = preferredConnectionId is { } connectionId
            ? await connections.GetRuntimeAsync(payment.TenantId, connectionId, cancellationToken)
            : await connections.GetEnabledRuntimeAsync(payment.TenantId, "FinancialSystem", cancellationToken);
        if (connection is null ||
            connection.TenantId != payment.TenantId ||
            !connection.Enabled ||
            connection.Group != "FinancialSystem")
            throw new ChargeSettlementConfigurationException("FinancialConnectionUnavailable");
        // A conexão só é elegível quando o resolver registra sua capacidade de liquidação.
        if (!settlementProviders.CanResolve(connection.Provider))
            throw new ChargeSettlementConfigurationException("ProviderChanged");
        var values = await settings.ReadGroupAsync(payment.TenantId, "RegionalSettings", cancellationToken);
        if (!values.TryGetValue("DefaultTimeZoneId", out var zoneId) || string.IsNullOrWhiteSpace(zoneId))
            throw new ChargeSettlementConfigurationException("ReceiptTimeZoneMissing");
        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
            return new(
                connection.Id,
                DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(
                    payment.ReceivedAt,
                    zone)
                    .DateTime));
        }
        catch (TimeZoneNotFoundException)
        {
            throw new ChargeSettlementConfigurationException("ReceiptTimeZoneInvalid");
        }
        catch (InvalidTimeZoneException)
        {
            throw new ChargeSettlementConfigurationException("ReceiptTimeZoneInvalid");
        }
    }

    public async Task<Guid> ResolveReceiptAccountAsync(
        ChargeSettlementPayment payment,
        Guid connectionId,
        CancellationToken cancellationToken)
    {
        if (payment.TenantId == Guid.Empty || payment.BankConnectionId == Guid.Empty ||
            payment.PaymentMethodId == Guid.Empty || payment.Id == Guid.Empty || connectionId == Guid.Empty)
            throw new ChargeSettlementConfigurationException("SettlementPaymentMismatch");


        var connection = await connections.GetRuntimeAsync(payment.TenantId, connectionId, cancellationToken);

        if (connection is null || connection.TenantId != payment.TenantId || connection.Id != connectionId ||
            !connection.Enabled || connection.Group != "FinancialSystem")
            throw new ChargeSettlementConfigurationException("FinancialConnectionUnavailable");

        // A capacidade registrada resolve o mapeamento de conta próprio do provider.
        if (!settlementProviders.CanResolve(connection.Provider))
            throw new ChargeSettlementConfigurationException("ProviderChanged");
        return await settlementProviders.Resolve(connection.Provider)
            .ResolveReceiptAccountAsync(payment, new(
                connection.Id,
                connection.TenantId,
                connection.Provider,
                connection.Name,
                connection.Enabled,
                connection.Group), cancellationToken);
    }
}
