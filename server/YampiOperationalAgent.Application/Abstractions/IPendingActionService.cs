using YampiOperationalAgent.Application.Contracts;
using YampiOperationalAgent.Domain.Entities;

namespace YampiOperationalAgent.Application.Abstractions;

public interface IPendingActionService
{
    Task<PendingAction> CreateOrReplaceAsync(
        CreateOrReplacePendingActionRequest request,
        CancellationToken cancellationToken);

    Task<PendingAction> ConfirmAsync(
        string conversationId,
        Guid pendingActionId,
        CancellationToken cancellationToken);

    Task<PendingAction> CancelAsync(
        string conversationId,
        Guid pendingActionId,
        CancellationToken cancellationToken);

    Task<PendingAction> BeginExecutionAsync(
        string conversationId,
        Guid pendingActionId,
        CancellationToken cancellationToken);

    Task<PendingAction> CompleteExecutionAsync(
        string conversationId,
        Guid pendingActionId,
        CancellationToken cancellationToken);

    Task<PendingAction> FailExecutionAsync(
        string conversationId,
        Guid pendingActionId,
        string failureMessage,
        CancellationToken cancellationToken);
}
