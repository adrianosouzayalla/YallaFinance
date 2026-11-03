namespace YallaFinance.Infrastructure.Providers.Nibo.Financial.Responses;

internal sealed record NiboReceiptResponse(
    Guid? EntryId,
    Guid? ScheduleId,
    NiboReceiptAccountResponse? Account,
    DateTimeOffset? Date,
    string? Identifier,
    decimal? Value,
    bool? IsVirtual,
    bool? IsTransfer);
