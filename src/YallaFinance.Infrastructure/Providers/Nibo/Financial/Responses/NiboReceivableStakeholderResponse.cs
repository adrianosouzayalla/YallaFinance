namespace YallaFinance.Infrastructure.Providers.Nibo.Financial.Responses;

internal sealed record NiboReceivableStakeholderResponse(
    string? Id,
    bool? IsDeleted = null,
    bool? IsArchived = null);
