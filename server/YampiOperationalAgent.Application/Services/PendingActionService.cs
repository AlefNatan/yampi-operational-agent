using YampiOperationalAgent.Application.Abstractions;
using YampiOperationalAgent.Application.Contracts;
using YampiOperationalAgent.Application.Exceptions;
using YampiOperationalAgent.Domain.Entities;
using YampiOperationalAgent.Domain.Enums;

namespace YampiOperationalAgent.Application.Services;

public sealed class PendingActionService(
    IPendingActionRepository repository,
    TimeProvider timeProvider) : IPendingActionService
{
    public async Task<PendingAction> CreateOrReplaceAsync(
        CreateOrReplacePendingActionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateCreateOrReplaceRequest(request);
        var conversationId = request.ConversationId.Trim();

        var currentPendingAction = await repository.GetActiveByConversationIdAsync(
            conversationId,
            cancellationToken);

        var now = timeProvider.GetUtcNow();
        var pendingAction = new PendingAction
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Type = request.Type,
            Status = PendingActionStatus.PendingConfirmation,
            SkuId = request.SkuId,
            SkuCode = NormalizeOptional(request.SkuCode),
            SkuName = request.SkuName.Trim(),
            CurrentValue = request.CurrentValue,
            NewValue = request.NewValue,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        if (currentPendingAction is not null)
        {
            if (currentPendingAction.Status is PendingActionStatus.Confirmed or PendingActionStatus.Executing)
            {
                throw new PendingActionInvalidTransitionException(
                    "The active pending action for this conversation cannot be replaced after confirmation.");
            }

            TryApplyTransition(() => currentPendingAction.MarkAsReplaced(pendingAction.Id, now));
        }

        await repository.AddAsync(pendingAction, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return pendingAction;
    }

    public async Task<PendingAction> ConfirmAsync(
        string conversationId,
        Guid pendingActionId,
        CancellationToken cancellationToken)
    {
        var pendingAction = await GetByConversationAndIdAsync(
            conversationId,
            pendingActionId,
            cancellationToken);

        TryApplyTransition(() => pendingAction.Confirm(timeProvider.GetUtcNow()));
        await repository.SaveChangesAsync(cancellationToken);

        return pendingAction;
    }

    public async Task<PendingAction> CancelAsync(
        string conversationId,
        Guid pendingActionId,
        CancellationToken cancellationToken)
    {
        var pendingAction = await GetByConversationAndIdAsync(
            conversationId,
            pendingActionId,
            cancellationToken);

        TryApplyTransition(() => pendingAction.Cancel(timeProvider.GetUtcNow()));
        await repository.SaveChangesAsync(cancellationToken);

        return pendingAction;
    }

    public async Task<PendingAction> BeginExecutionAsync(
        string conversationId,
        Guid pendingActionId,
        CancellationToken cancellationToken)
    {
        var pendingAction = await GetByConversationAndIdAsync(
            conversationId,
            pendingActionId,
            cancellationToken);

        TryApplyTransition(() => pendingAction.BeginExecution(timeProvider.GetUtcNow()));
        await repository.SaveChangesAsync(cancellationToken);

        return pendingAction;
    }

    public async Task<PendingAction> CompleteExecutionAsync(
        string conversationId,
        Guid pendingActionId,
        CancellationToken cancellationToken)
    {
        var pendingAction = await GetByConversationAndIdAsync(
            conversationId,
            pendingActionId,
            cancellationToken);

        TryApplyTransition(() => pendingAction.MarkAsExecuted(timeProvider.GetUtcNow()));
        await repository.SaveChangesAsync(cancellationToken);

        return pendingAction;
    }

    public async Task<PendingAction> FailExecutionAsync(
        string conversationId,
        Guid pendingActionId,
        string failureMessage,
        CancellationToken cancellationToken)
    {
        var pendingAction = await GetByConversationAndIdAsync(
            conversationId,
            pendingActionId,
            cancellationToken);

        TryApplyArgumentTransition(
            () => pendingAction.MarkExecutionFailed(failureMessage, timeProvider.GetUtcNow()));
        await repository.SaveChangesAsync(cancellationToken);

        return pendingAction;
    }

    private async Task<PendingAction> GetByConversationAndIdAsync(
        string conversationId,
        Guid pendingActionId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            throw new ArgumentException("ConversationId is required.", nameof(conversationId));
        }

        var pendingAction = await repository.GetByIdAsync(pendingActionId, cancellationToken);
        if (pendingAction is null || !string.Equals(
                pendingAction.ConversationId,
                conversationId.Trim(),
                StringComparison.Ordinal))
        {
            throw new PendingActionNotFoundException();
        }

        return pendingAction;
    }

    private static void ValidateCreateOrReplaceRequest(CreateOrReplacePendingActionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ConversationId))
        {
            throw new ArgumentException("ConversationId is required.", nameof(request));
        }

        if (!Enum.IsDefined(request.Type))
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.Type),
                request.Type,
                "PendingActionType must be a defined value.");
        }

        if (request.SkuId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "SkuId must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(request.SkuName))
        {
            throw new ArgumentException("SkuName is required.", nameof(request));
        }

        ValidateValueRange(request.Type, request.CurrentValue, nameof(request.CurrentValue));
        ValidateValueRange(request.Type, request.NewValue, nameof(request.NewValue));
    }

    private static void ValidateValueRange(PendingActionType type, decimal value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "The value must be greater than or equal to zero.");
        }

        if (type == PendingActionType.Stock && decimal.Truncate(value) != value)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Stock values must be whole numbers.");
        }
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void TryApplyTransition(Action transition)
    {
        try
        {
            transition();
        }
        catch (InvalidOperationException exception)
        {
            throw new PendingActionInvalidTransitionException(exception.Message);
        }
    }

    private static void TryApplyArgumentTransition(Action transition)
    {
        try
        {
            transition();
        }
        catch (InvalidOperationException exception)
        {
            throw new PendingActionInvalidTransitionException(exception.Message);
        }
        catch (ArgumentException)
        {
            throw;
        }
    }
}
