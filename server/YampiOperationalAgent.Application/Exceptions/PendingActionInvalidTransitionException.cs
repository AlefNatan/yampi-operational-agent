namespace YampiOperationalAgent.Application.Exceptions;

public sealed class PendingActionInvalidTransitionException : Exception
{
    public PendingActionInvalidTransitionException(string message)
        : base(message)
    {
    }
}
