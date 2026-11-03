
using YallaFinance.Infrastructure.Providers;
using YallaFinance.Infrastructure.Providers.Configuration;
using YallaFinance.Application.Services.Payments.Pix.Contracts;

namespace YallaFinance.Infrastructure.Providers.Payments.Pix;

internal enum PixReceivedWindowRule
{
    Unrestricted,
    CalendarMonth
}

internal sealed record PixReceivedWindow(DateTimeOffset Start, DateTimeOffset End);

// Janela base de recebimentos, usada quando a conexao nao preenche o lookback. Providers podem declarar particularidades.
internal static class PixReceivedDefaults
{
    public const int BaseLookbackDays = 7;
}

internal interface IPixReceivedProvider : IProviderCapability
{
    // Limite de intervalo que o provider impõe a cada consulta. CalendarMonth: inicio e fim devem estar no mesmo mês civil.
    PixReceivedWindowRule WindowRule { get; }

    // Janela padrão de recebimentos (lookback configurado da conexão), calculada pelo próprio provider.
    Task<PixReceivedWindow> ResolveDefaultWindowAsync(
        ProviderConnection connection,
        CancellationToken cancellationToken);

    Task<PixReceivedQueryResult> QueryAsync(
        PixReceivedQuery query,
        ProviderConnection connection,
        CancellationToken cancellationToken);
}
