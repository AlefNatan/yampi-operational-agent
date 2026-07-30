using YampiOperationalAgent.Domain.Enums;

namespace YampiOperationalAgent.Domain.Entities;

public sealed class PendingAction
{
    public Guid Id { get; set; }

    public string ConversationId { get; set; } = string.Empty;

    public PendingActionType Type { get; set; }

    public PendingActionStatus Status { get; set; }

    public long SkuId { get; set; }

    public string? SkuCode { get; set; }

    public string SkuName { get; set; } = string.Empty;

    public decimal CurrentValue { get; set; }

    public decimal NewValue { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public DateTimeOffset? ConfirmedAtUtc { get; set; }

    public DateTimeOffset? CanceledAtUtc { get; set; }

    public DateTimeOffset? ReplacedAtUtc { get; set; }

    public DateTimeOffset? ExecutedAtUtc { get; set; }

    public Guid? ReplacedByActionId { get; set; }

    public string? FailureMessage { get; set; }

    public uint Version { get; set; }

    public void MarkAsReplaced(Guid replacementId, DateTimeOffset now)
    {
        EnsurePendingConfirmationTransition("replaced");

        Status = PendingActionStatus.Replaced;
        ReplacedAtUtc = now;
        ReplacedByActionId = replacementId;
        UpdatedAtUtc = now;
    }

    public void Confirm(DateTimeOffset now)
    {
        EnsurePendingConfirmationTransition("confirmed");

        Status = PendingActionStatus.Confirmed;
        ConfirmedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        EnsurePendingConfirmationTransition("canceled");

        Status = PendingActionStatus.Canceled;
        CanceledAtUtc = now;
        UpdatedAtUtc = now;
    }

    private void EnsurePendingConfirmationTransition(string targetState)
    {
        if (Status != PendingActionStatus.PendingConfirmation)
        {
            throw new InvalidOperationException(
                $"Pending action cannot be {targetState} when status is '{Status}'.");
        }
    }
}
