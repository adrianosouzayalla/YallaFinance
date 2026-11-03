namespace YallaFinance.Infrastructure.Providers.Nibo.Financial.Responses;

internal sealed record NiboReceivableResponse(
    string? ScheduleId,
    string? Type,
    string? Reference,
    string? StakeholderId,
    NiboReceivableStakeholderResponse? Stakeholder,
    decimal? Value,
    DateTimeOffset? ScheduleDate,
    DateTimeOffset? DueDate,
    DateTimeOffset? AccrualDate,
    string? Description,
    IReadOnlyList<NiboReceivableCategoryResponse>? Categories,
    bool? IsPaid,
    DateTimeOffset? DeleteDate = null,
    bool? IsDeleted = null,
    bool? IsArchived = null);
