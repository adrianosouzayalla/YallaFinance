
using YallaFinance.Domain.Charges;
using YallaFinance.Application.Features.Charges.IntegrateCharge;
using YallaFinance.Application.Services.Financial.Contracts;
using YallaFinance.Application.Repositories.Charges.Contracts;
using YallaFinance.Application.Services.Charges.Contracts;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Charges.CreateCharge;

public sealed record ChargeDetailsResult(
    string Id,
    string IdempotencyKey,
    ChargeSource Source,
    string TenantId,
    string CustomerId,
    IReadOnlyList<string> DebtIds,
    decimal Amount,
    string CategoryId,
    DateOnly DueDate,
    string ProviderExternalId,
    string PixId,
    string PixPayload,
    DateTimeOffset CreatedAt,
    CreateChargeResult? FinancialIntegration = null,
    IReadOnlyList<ChargeItemResponse>? Items = null,
    string? Note = null,
    ChargeProcessingStatus? Processing = null,
    string Reference = "",
    string? ExactAmount = null,
    string? CustomerName = null)
{
    public static ChargeDetailsResult From(ChargeResult result) => new(
        result.Id,
        result.IdempotencyKey,
        result.Source,
        result.TenantId,
        result.CustomerId,
        result.DebtIds,
        result.Amount,
        result.CategoryId,
        result.DueDate,
        result.ProviderExternalId,
        result.PixId,
        result.PixPayload,
        result.CreatedAt);
}
