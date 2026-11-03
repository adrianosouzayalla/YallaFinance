using YallaFinance.Domain.Jobs;

namespace YallaFinance.Application.Repositories.Jobs;

public sealed record JobRetryCandidate(Job Job, int Attempts, DateTimeOffset? LastFinishedAt);

// Consulta separada de IJobRepository para nao alterar as implementacoes de teste existentes.
public interface IJobRetryRepository
{
    Task<IReadOnlyList<JobRetryCandidate>> ListFailedJobsForRetryAsync(int batchSize, CancellationToken cancellationToken);
}
