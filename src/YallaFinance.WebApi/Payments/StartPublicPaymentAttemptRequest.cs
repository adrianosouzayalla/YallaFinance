namespace YallaFinance.WebApi.Payments;

public sealed record StartPublicPaymentAttemptRequest(string MethodCode)
{
    public YallaFinance.Application.Services.Payments.Contracts.PublicCardPaymentInput? Card { get; init; }
    public override string ToString() => nameof(StartPublicPaymentAttemptRequest);
}
