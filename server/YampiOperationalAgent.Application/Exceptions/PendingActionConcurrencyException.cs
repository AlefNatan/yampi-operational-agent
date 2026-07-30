namespace YampiOperationalAgent.Application.Exceptions;

public sealed class PendingActionConcurrencyException : Exception
{
    public PendingActionConcurrencyException()
        : base("The pending action could not be saved because it was modified by another operation.")
    {
    }
}
