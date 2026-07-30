using Microsoft.EntityFrameworkCore;
using Npgsql;
using YampiOperationalAgent.Application.Abstractions;
using YampiOperationalAgent.Application.Exceptions;
using YampiOperationalAgent.Domain.Entities;
using YampiOperationalAgent.Domain.Enums;

namespace YampiOperationalAgent.Infrastructure.Persistence.Repositories;

internal sealed class PendingActionRepository(OperationalAgentDbContext dbContext) : IPendingActionRepository
{
    private static readonly PendingActionStatus[] ActiveStatuses =
    [
        PendingActionStatus.PendingConfirmation,
        PendingActionStatus.Confirmed,
        PendingActionStatus.Executing
    ];

    public Task<PendingAction?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.PendingActions
            .FirstOrDefaultAsync(pendingAction => pendingAction.Id == id, cancellationToken);
    }

    public Task<PendingAction?> GetActiveByConversationIdAsync(string conversationId, CancellationToken cancellationToken)
    {
        return dbContext.PendingActions
            .SingleOrDefaultAsync(
                pendingAction =>
                    pendingAction.ConversationId == conversationId
                    && ActiveStatuses.Contains(pendingAction.Status),
                cancellationToken);
    }

    public Task AddAsync(PendingAction pendingAction, CancellationToken cancellationToken)
    {
        return dbContext.PendingActions.AddAsync(pendingAction, cancellationToken).AsTask();
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new PendingActionConcurrencyException();
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException postgresException
                  && postgresException.SqlState == PostgresErrorCodes.UniqueViolation
                  && postgresException.ConstraintName == "ux_pending_actions_active_conversation")
        {
            throw new PendingActionConflictException();
        }
    }
}
