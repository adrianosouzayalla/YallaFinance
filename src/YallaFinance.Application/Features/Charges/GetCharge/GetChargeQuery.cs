using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Charges.CreateCharge;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Charges.GetCharge;

public sealed record GetChargeQuery(string Id) : IRequest<NotificationResult<ChargeDetailsResult>>;
