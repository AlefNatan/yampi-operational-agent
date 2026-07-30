using YampiOperationalAgent.Domain.Entities;
using YampiOperationalAgent.Domain.Enums;

namespace YampiOperationalAgent.Tests.Domain;

public sealed class PendingActionTests
{
    [Fact]
    public void MarkAsReplaced_WhenPendingConfirmation_UpdatesReplacementFields()
    {
        var now = new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);
        var replacementId = Guid.NewGuid();
        var pendingAction = CreatePendingAction();

        pendingAction.MarkAsReplaced(replacementId, now);

        Assert.Equal(PendingActionStatus.Replaced, pendingAction.Status);
        Assert.Equal(replacementId, pendingAction.ReplacedByActionId);
        Assert.Equal(now, pendingAction.ReplacedAtUtc);
        Assert.Equal(now, pendingAction.UpdatedAtUtc);
    }

    [Fact]
    public void Confirm_WhenPendingConfirmation_UpdatesConfirmationFields()
    {
        var now = new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);
        var pendingAction = CreatePendingAction();

        pendingAction.Confirm(now);

        Assert.Equal(PendingActionStatus.Confirmed, pendingAction.Status);
        Assert.Equal(now, pendingAction.ConfirmedAtUtc);
        Assert.Equal(now, pendingAction.UpdatedAtUtc);
    }

    [Fact]
    public void Cancel_WhenPendingConfirmation_UpdatesCancellationFields()
    {
        var now = new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);
        var pendingAction = CreatePendingAction();

        pendingAction.Cancel(now);

        Assert.Equal(PendingActionStatus.Canceled, pendingAction.Status);
        Assert.Equal(now, pendingAction.CanceledAtUtc);
        Assert.Equal(now, pendingAction.UpdatedAtUtc);
    }

    [Theory]
    [InlineData(PendingActionStatus.Confirmed)]
    [InlineData(PendingActionStatus.Canceled)]
    [InlineData(PendingActionStatus.Replaced)]
    [InlineData(PendingActionStatus.Executing)]
    [InlineData(PendingActionStatus.Executed)]
    [InlineData(PendingActionStatus.ExecutionFailed)]
    public void Transitions_WhenStatusIsNotPendingConfirmation_ThrowInvalidOperationException(
        PendingActionStatus status)
    {
        var now = new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);
        var pendingAction = CreatePendingAction(status);

        Assert.Throws<InvalidOperationException>(() => pendingAction.Confirm(now));
        Assert.Throws<InvalidOperationException>(() => pendingAction.Cancel(now));
        Assert.Throws<InvalidOperationException>(() => pendingAction.MarkAsReplaced(Guid.NewGuid(), now));
    }

    private static PendingAction CreatePendingAction(
        PendingActionStatus status = PendingActionStatus.PendingConfirmation)
    {
        return new PendingAction
        {
            Id = Guid.NewGuid(),
            ConversationId = "conversation-1",
            Type = PendingActionType.Price,
            Status = status,
            SkuId = 10,
            SkuName = "Sku Teste",
            CurrentValue = 100m,
            NewValue = 120m,
            CreatedAtUtc = new DateTimeOffset(2026, 7, 30, 11, 0, 0, TimeSpan.Zero),
            UpdatedAtUtc = new DateTimeOffset(2026, 7, 30, 11, 0, 0, TimeSpan.Zero)
        };
    }
}
