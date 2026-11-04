using MediatR;

namespace YallaFinance.Application.Features.Payments.GetPublicCheckoutStatus;

public sealed record GetPublicCheckoutStatusQuery(string Token) : IRequest<GetPublicCheckoutStatusResult>;
