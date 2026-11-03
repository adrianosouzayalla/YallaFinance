using YallaFinance.Application;
namespace YallaFinance.Application.Features.Payments.Shared;

public static class PaymentLinkValidity
{
    public static string? Validate(DateTimeOffset expiresAt, DateOnly dueDate)
    {
        var saoPauloTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        var localExpiration = TimeZoneInfo.ConvertTime(expiresAt, saoPauloTimeZone);
        return DateOnly.FromDateTime(localExpiration.DateTime) > dueDate
            ? "A validade do link não pode ultrapassar o vencimento da cobrança."
            : null;
    }
}
