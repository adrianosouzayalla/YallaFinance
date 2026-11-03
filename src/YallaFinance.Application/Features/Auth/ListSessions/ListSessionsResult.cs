using YallaFinance.Application.Repositories.Sessions.Contracts;
using YallaFinance.Application;


namespace YallaFinance.Application.Features.Auth.ListSessions;

public sealed record ListSessionsResult(ActiveSessionPage Page);
