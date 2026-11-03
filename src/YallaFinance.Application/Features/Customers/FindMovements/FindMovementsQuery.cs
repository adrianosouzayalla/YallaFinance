using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.Fleet.Contracts;
using YallaFinance.Application;


namespace YallaFinance.Application.Features.Customers.FindMovements;

public sealed record FindMovementsQuery(string CustomerId, DateOnly OccurrenceDate) : IRequest<NotificationResult<IReadOnlyList<FleetMovementResponse>>>;
