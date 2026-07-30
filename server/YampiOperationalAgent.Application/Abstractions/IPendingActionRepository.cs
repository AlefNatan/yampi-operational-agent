using YampiOperationalAgent.Domain.Entities;

namespace YampiOperationalAgent.Application.Abstractions;

public interface IPendingActionRepository
{
    Task<PendingAction?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PendingAction?> GetActiveByConversationIdAsync(string conversationId, CancellationToken cancellationToken);

    Task AddAsync(PendingAction pendingAction, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
