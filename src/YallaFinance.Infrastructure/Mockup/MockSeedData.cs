using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Repositories.Debits.Contracts;
using YallaFinance.Application.Repositories.Customers.Contracts;
using YallaFinance.Application.Repositories.Fleet.Contracts;

namespace YallaFinance.Infrastructure.Mockup;

public sealed record MockSeedData(
    IReadOnlyList<DebtResponse> Debts,
    IReadOnlyList<CustomerResponse> Customers,
    IReadOnlyList<FleetMovementResponse> FleetMovements);
