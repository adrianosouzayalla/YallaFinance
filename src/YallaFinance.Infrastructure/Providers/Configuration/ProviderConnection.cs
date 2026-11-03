using YallaFinance.Infrastructure.Providers.Configuration;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace YallaFinance.Infrastructure.Providers.Configuration;

internal sealed record ProviderConnection(
    [property: Column("Id")] Guid Id,
    [property: Column("TenantId")] Guid TenantId,
    [property: Column("Provider")] string Provider,
    [property: Column("Name")] string Name,
    [property: Column("Enabled")] bool Enabled,
    [property: Column("Group"), JsonRequired] string Group = "FinancialSystem");
