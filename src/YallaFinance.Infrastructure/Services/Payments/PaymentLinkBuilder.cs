using YallaFinance.Application.Repositories.Payments.Contracts;


namespace YallaFinance.Infrastructure.Services.Payments;

public static class PaymentLinkBuilder
{
    public static PaymentLinkResult Build(Guid id, string token, string publicOrigin) =>
        new(id, IsValidPublicOrigin(publicOrigin)
            ? publicOrigin.TrimEnd('/') + "/pay/" + Uri.EscapeDataString(token)
            : null);

    public static bool IsValidPublicOrigin(string? publicOrigin) =>
        Uri.TryCreate(publicOrigin, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps
            || uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)
        && string.IsNullOrEmpty(uri.UserInfo)
        && string.IsNullOrEmpty(uri.Query)
        && string.IsNullOrEmpty(uri.Fragment)
        && uri.AbsolutePath == "/";
}
