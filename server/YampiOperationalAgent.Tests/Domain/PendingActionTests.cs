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

    [Fact]
    public void BeginExecution_WhenConfirmed_UpdatesStatusAndTimestamp()
    {
        var now = new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);
        var pendingAction = CreatePendingAction(PendingActionStatus.Confirmed);

        pendingAction.BeginExecution(now);

        Assert.Equal(PendingActionStatus.Executing, pendingAction.Status);
        Assert.Equal(now, pendingAction.UpdatedAtUtc);
    }

    [Fact]
    public void MarkAsExecuted_WhenExecuting_UpdatesExecutionFields()
    {
        var now = new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);
        var pendingAction = CreatePendingAction(PendingActionStatus.Executing);
        pendingAction.FailureMessage = "temporary failure";

        pendingAction.MarkAsExecuted(now);

        Assert.Equal(PendingActionStatus.Executed, pendingAction.Status);
        Assert.Equal(now, pendingAction.ExecutedAtUtc);
        Assert.Null(pendingAction.FailureMessage);
        Assert.Equal(now, pendingAction.UpdatedAtUtc);
    }

    [Fact]
    public void MarkExecutionFailed_WhenExecuting_UpdatesFailureFields()
    {
        var now = new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);
        var pendingAction = CreatePendingAction(PendingActionStatus.Executing);

        pendingAction.MarkExecutionFailed("failure message", now);

        Assert.Equal(PendingActionStatus.ExecutionFailed, pendingAction.Status);
        Assert.Equal("failure message", pendingAction.FailureMessage);
        Assert.Equal(now, pendingAction.UpdatedAtUtc);
    }

    [Fact]
    public void MarkExecutionFailed_WhenMessageIsTooLong_TruncatesTo1000Characters()
    {
        var now = new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);
        var pendingAction = CreatePendingAction(PendingActionStatus.Executing);
        var longMessage = new string('x', 1200);

        pendingAction.MarkExecutionFailed(longMessage, now);

        Assert.Equal(PendingActionStatus.ExecutionFailed, pendingAction.Status);
        Assert.Equal(1000, pendingAction.FailureMessage?.Length);
        Assert.Equal(new string('x', 1000), pendingAction.FailureMessage);
    }

    [Fact]
    public void MarkExecutionFailed_WhenMessageIsEmpty_ThrowsArgumentException()
    {
        var now = new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);
        var pendingAction = CreatePendingAction(PendingActionStatus.Executing);

        Assert.Throws<ArgumentException>(() => pendingAction.MarkExecutionFailed("   ", now));
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

    [Theory]
    [InlineData(PendingActionStatus.PendingConfirmation)]
    [InlineData(PendingActionStatus.Canceled)]
    [InlineData(PendingActionStatus.Replaced)]
    [InlineData(PendingActionStatus.Executing)]
    [InlineData(PendingActionStatus.Executed)]
    [InlineData(PendingActionStatus.ExecutionFailed)]
    public void BeginExecution_WhenStatusIsNotConfirmed_ThrowsInvalidOperationException(
        PendingActionStatus status)
    {
        var now = new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);
        var pendingAction = CreatePendingAction(status);

        Assert.Throws<InvalidOperationException>(() => pendingAction.BeginExecution(now));
    }

    [Theory]
    [InlineData(PendingActionStatus.PendingConfirmation)]
    [InlineData(PendingActionStatus.Confirmed)]
    [InlineData(PendingActionStatus.Canceled)]
    [InlineData(PendingActionStatus.Replaced)]
    [InlineData(PendingActionStatus.Executed)]
    [InlineData(PendingActionStatus.ExecutionFailed)]
    public void MarkAsExecuted_WhenStatusIsNotExecuting_ThrowsInvalidOperationException(
        PendingActionStatus status)
    {
        var now = new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);
        var pendingAction = CreatePendingAction(status);

        Assert.Throws<InvalidOperationException>(() => pendingAction.MarkAsExecuted(now));
    }

    [Theory]
    [InlineData(PendingActionStatus.PendingConfirmation)]
    [InlineData(PendingActionStatus.Confirmed)]
    [InlineData(PendingActionStatus.Canceled)]
    [InlineData(PendingActionStatus.Replaced)]
    [InlineData(PendingActionStatus.Executed)]
    [InlineData(PendingActionStatus.ExecutionFailed)]
    public void MarkExecutionFailed_WhenStatusIsNotExecuting_ThrowsInvalidOperationException(
        PendingActionStatus status)
    {
        var now = new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);
        var pendingAction = CreatePendingAction(status);

        Assert.Throws<InvalidOperationException>(() => pendingAction.MarkExecutionFailed("failure", now));
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
