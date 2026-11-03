namespace YallaFinance.Application.Common.Context;

public sealed class CurrentExecutionActorContext
{
    private Guid? actorId;
    private bool operational;

    public Guid RequireActorId() => actorId
        ?? throw new InvalidOperationException("A trusted execution actor is required before writing data.");

    public void SetAuthenticatedUser(Guid userId) => Initialize(userId, false);

    public void SetOperationalUser(Guid userId) => Initialize(userId, true);

    private void Initialize(Guid userId, bool isOperational)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("An execution actor cannot be empty.", nameof(userId));
        if (actorId is not null && (actorId != userId || operational != isOperational))
            throw new InvalidOperationException("The execution actor cannot change within an existing scope.");
        actorId = userId;
        operational = isOperational;
    }
}
