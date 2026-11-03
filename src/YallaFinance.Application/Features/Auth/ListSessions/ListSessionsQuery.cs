using MediatR;

using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.Sessions.Contracts;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Auth.ListSessions;

public sealed record ListSessionsQuery(ActiveSessionFilter Filter) : IRequest<NotificationResult<ListSessionsResult>>;
