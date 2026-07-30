namespace YampiOperationalAgent.Application.Exceptions;

public sealed class PendingActionNotFoundException : Exception
{
    public PendingActionNotFoundException()
        : base("The pending action was not found for this conversation.")
    {
    }
}
