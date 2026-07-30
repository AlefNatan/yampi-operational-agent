namespace YampiOperationalAgent.Application.Exceptions;

public sealed class PendingActionConflictException : Exception
{
    public PendingActionConflictException()
        : base("Another pending action is already active for this conversation.")
    {
    }
}
