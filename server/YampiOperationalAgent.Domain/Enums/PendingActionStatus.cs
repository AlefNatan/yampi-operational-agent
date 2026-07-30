namespace YampiOperationalAgent.Domain.Enums;

public enum PendingActionStatus
{
    PendingConfirmation = 1,
    Confirmed = 2,
    Canceled = 3,
    Replaced = 4,
    Executing = 5,
    Executed = 6,
    ExecutionFailed = 7
}
