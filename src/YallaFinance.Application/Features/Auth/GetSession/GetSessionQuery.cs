using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Auth.GetSession;

public sealed record GetSessionQuery
    : IRequest<NotificationResult<GetSessionResult>>;
