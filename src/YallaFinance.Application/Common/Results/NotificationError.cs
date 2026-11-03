using YallaFinance.Application;

namespace YallaFinance.Application.Common.Results;

public sealed record NotificationError(string Code, string Message, string? Field = null);
