using System.Globalization;
using YallaFinance.Application.Services.Payments;
using YallaFinance.Application.Services.Payments.Contracts;
using YallaFinance.Application.Repositories.Settings;



namespace YallaFinance.Infrastructure.Services.Payments;

public sealed class PaymentPublicationSettings(ITenantSettingRepository repository) : IPaymentPublicationSettings
{
    public async Task<PaymentTenantSettings> GetAsync(Guid tenant, CancellationToken ct) =>
        ResolveTenantPaymentSettings(await repository.ReadGroupAsync(tenant, "Payments", ct));

    private static PaymentTenantSettings ResolveTenantPaymentSettings(IReadOnlyDictionary<string, string> values)
    {
        if (!values.TryGetValue("MaximumLinkDays", out var daysText)
            || !int.TryParse(daysText, NumberStyles.None, CultureInfo.InvariantCulture, out var days) || days is < 1 or > 365)
            throw new InvalidOperationException("Configure os parâmetros de Payments desta empresa no banco.");
        var origin = values.GetValueOrDefault("PublicOrigin") ?? "";
        if (!string.IsNullOrEmpty(origin) && !PaymentLinkBuilder.IsValidPublicOrigin(origin))
            throw new InvalidOperationException("Configure uma origem pública válida para Payments desta empresa no banco.");
        return new(days, origin);
    }
}
