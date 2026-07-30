using YampiOperationalAgent.Application.Abstractions;
using YampiOperationalAgent.Application.Contracts;
using YampiOperationalAgent.Application.Exceptions;
using YampiOperationalAgent.Application.Services;
using YampiOperationalAgent.Domain.Entities;
using YampiOperationalAgent.Domain.Enums;

namespace YampiOperationalAgent.Tests.Application;

public sealed class PendingActionServiceTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateOrReplaceAsync_WhenRequestIsValid_CreatesPendingAction()
    {
        var repository = new FakePendingActionRepository();
        var service = new PendingActionService(repository, new FakeTimeProvider(FixedNow));
        var request = CreateRequest(conversationId: "  conversation-1  ");

        var result = await service.CreateOrReplaceAsync(request, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("conversation-1", result.ConversationId);
        Assert.Equal(PendingActionStatus.PendingConfirmation, result.Status);
        Assert.Equal(FixedNow, result.CreatedAtUtc);
        Assert.Equal(FixedNow, result.UpdatedAtUtc);
        Assert.Equal("conversation-1", repository.LastActiveConversationId);
        Assert.Single(repository.Items);
        Assert.Equal(1, repository.AddCallCount);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CreateOrReplaceAsync_WhenPendingConfirmationExists_ReplacesPreviousAction()
    {
        var existingAction = CreatePendingAction(status: PendingActionStatus.PendingConfirmation);
        var repository = new FakePendingActionRepository(existingAction);
        var service = new PendingActionService(repository, new FakeTimeProvider(FixedNow));

        var result = await service.CreateOrReplaceAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(PendingActionStatus.Replaced, existingAction.Status);
        Assert.Equal(result.Id, existingAction.ReplacedByActionId);
        Assert.Equal(FixedNow, existingAction.ReplacedAtUtc);
        Assert.Equal(FixedNow, existingAction.UpdatedAtUtc);
        Assert.Equal(2, repository.Items.Count);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task ConfirmAsync_WhenActionMatchesConversation_ConfirmsAction()
    {
        var pendingAction = CreatePendingAction();
        var repository = new FakePendingActionRepository(pendingAction);
        var service = new PendingActionService(repository, new FakeTimeProvider(FixedNow));

        var result = await service.ConfirmAsync(
            pendingAction.ConversationId,
            pendingAction.Id,
            CancellationToken.None);

        Assert.Equal(PendingActionStatus.Confirmed, result.Status);
        Assert.Equal(FixedNow, result.ConfirmedAtUtc);
        Assert.Equal(FixedNow, result.UpdatedAtUtc);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CancelAsync_WhenActionMatchesConversation_CancelsAction()
    {
        var pendingAction = CreatePendingAction();
        var repository = new FakePendingActionRepository(pendingAction);
        var service = new PendingActionService(repository, new FakeTimeProvider(FixedNow));

        var result = await service.CancelAsync(
            pendingAction.ConversationId,
            pendingAction.Id,
            CancellationToken.None);

        Assert.Equal(PendingActionStatus.Canceled, result.Status);
        Assert.Equal(FixedNow, result.CanceledAtUtc);
        Assert.Equal(FixedNow, result.UpdatedAtUtc);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task ConfirmAsync_WhenCalledTwice_ThrowsInvalidTransitionException()
    {
        var pendingAction = CreatePendingAction(status: PendingActionStatus.PendingConfirmation);
        var repository = new FakePendingActionRepository(pendingAction);
        var service = new PendingActionService(repository, new FakeTimeProvider(FixedNow));

        await service.ConfirmAsync(pendingAction.ConversationId, pendingAction.Id, CancellationToken.None);
        var exception = await Assert.ThrowsAsync<PendingActionInvalidTransitionException>(() =>
            service.ConfirmAsync(pendingAction.ConversationId, pendingAction.Id, CancellationToken.None));

        Assert.Contains("cannot be confirmed", exception.Message);
        Assert.Equal(1, repository.SaveChangesCallCount);
        Assert.Equal(PendingActionStatus.Confirmed, pendingAction.Status);
    }

    [Fact]
    public async Task CancelAsync_WhenActionWasAlreadyConfirmed_ThrowsInvalidTransitionException()
    {
        var pendingAction = CreatePendingAction(status: PendingActionStatus.Confirmed);
        var repository = new FakePendingActionRepository(pendingAction);
        var service = new PendingActionService(repository, new FakeTimeProvider(FixedNow));

        var exception = await Assert.ThrowsAsync<PendingActionInvalidTransitionException>(() =>
            service.CancelAsync(pendingAction.ConversationId, pendingAction.Id, CancellationToken.None));

        Assert.Contains("cannot be canceled", exception.Message);
        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task ConfirmAsync_WhenConversationIsIncorrect_ThrowsNotFoundException()
    {
        var pendingAction = CreatePendingAction(conversationId: "conversation-1");
        var repository = new FakePendingActionRepository(pendingAction);
        var service = new PendingActionService(repository, new FakeTimeProvider(FixedNow));

        await Assert.ThrowsAsync<PendingActionNotFoundException>(() =>
            service.ConfirmAsync("conversation-2", pendingAction.Id, CancellationToken.None));

        Assert.Equal(0, repository.SaveChangesCallCount);
        Assert.Equal(PendingActionStatus.PendingConfirmation, pendingAction.Status);
    }

    [Fact]
    public async Task ConfirmAsync_WhenActionDoesNotExist_ThrowsNotFoundException()
    {
        var repository = new FakePendingActionRepository();
        var service = new PendingActionService(repository, new FakeTimeProvider(FixedNow));

        await Assert.ThrowsAsync<PendingActionNotFoundException>(() =>
            service.ConfirmAsync("conversation-1", Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CreateOrReplaceAsync_WhenTypeIsInvalid_ThrowsArgumentOutOfRangeException()
    {
        var repository = new FakePendingActionRepository();
        var service = new PendingActionService(repository, new FakeTimeProvider(FixedNow));
        var request = CreateRequest(type: (PendingActionType)999);

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.CreateOrReplaceAsync(request, CancellationToken.None));

        Assert.Equal("Type", exception.ParamName);
        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CreateOrReplaceAsync_WhenPriceIsNegative_ThrowsArgumentOutOfRangeException()
    {
        var repository = new FakePendingActionRepository();
        var service = new PendingActionService(repository, new FakeTimeProvider(FixedNow));
        var request = CreateRequest(type: PendingActionType.Price, newValue: -1m);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.CreateOrReplaceAsync(request, CancellationToken.None));

        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CreateOrReplaceAsync_WhenStockIsNegative_ThrowsArgumentOutOfRangeException()
    {
        var repository = new FakePendingActionRepository();
        var service = new PendingActionService(repository, new FakeTimeProvider(FixedNow));
        var request = CreateRequest(type: PendingActionType.Stock, newValue: -1m);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.CreateOrReplaceAsync(request, CancellationToken.None));

        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CreateOrReplaceAsync_WhenStockIsDecimal_ThrowsArgumentOutOfRangeException()
    {
        var repository = new FakePendingActionRepository();
        var service = new PendingActionService(repository, new FakeTimeProvider(FixedNow));
        var request = CreateRequest(type: PendingActionType.Stock, newValue: 10.5m);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.CreateOrReplaceAsync(request, CancellationToken.None));

        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CreateOrReplaceAsync_WhenActiveActionIsConfirmed_ThrowsInvalidTransitionException()
    {
        var repository = new FakePendingActionRepository(
            CreatePendingAction(status: PendingActionStatus.Confirmed));
        var service = new PendingActionService(repository, new FakeTimeProvider(FixedNow));

        await Assert.ThrowsAsync<PendingActionInvalidTransitionException>(() =>
            service.CreateOrReplaceAsync(CreateRequest(), CancellationToken.None));

        Assert.Equal(0, repository.SaveChangesCallCount);
        Assert.Equal(0, repository.AddCallCount);
    }

    [Fact]
    public async Task CreateOrReplaceAsync_WhenActiveActionIsExecuting_ThrowsInvalidTransitionException()
    {
        var repository = new FakePendingActionRepository(
            CreatePendingAction(status: PendingActionStatus.Executing));
        var service = new PendingActionService(repository, new FakeTimeProvider(FixedNow));

        await Assert.ThrowsAsync<PendingActionInvalidTransitionException>(() =>
            service.CreateOrReplaceAsync(CreateRequest(), CancellationToken.None));

        Assert.Equal(0, repository.SaveChangesCallCount);
        Assert.Equal(0, repository.AddCallCount);
    }

    private static CreateOrReplacePendingActionRequest CreateRequest(
        string conversationId = "conversation-1",
        PendingActionType type = PendingActionType.Price,
        long skuId = 10,
        string skuName = "Sku Teste",
        decimal currentValue = 100m,
        decimal newValue = 120m)
    {
        return new CreateOrReplacePendingActionRequest(
            conversationId,
            type,
            skuId,
            "SKU-001",
            skuName,
            currentValue,
            newValue);
    }

    private static PendingAction CreatePendingAction(
        string conversationId = "conversation-1",
        PendingActionStatus status = PendingActionStatus.PendingConfirmation)
    {
        return new PendingAction
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Type = PendingActionType.Price,
            Status = status,
            SkuId = 10,
            SkuCode = "SKU-001",
            SkuName = "Sku Teste",
            CurrentValue = 100m,
            NewValue = 120m,
            CreatedAtUtc = new DateTimeOffset(2026, 7, 30, 11, 0, 0, TimeSpan.Zero),
            UpdatedAtUtc = new DateTimeOffset(2026, 7, 30, 11, 0, 0, TimeSpan.Zero)
        };
    }

    private sealed class FakePendingActionRepository(params PendingAction[] items) : IPendingActionRepository
    {
        private static readonly PendingActionStatus[] ActiveStatuses =
        [
            PendingActionStatus.PendingConfirmation,
            PendingActionStatus.Confirmed,
            PendingActionStatus.Executing
        ];

        public List<PendingAction> Items { get; } = [.. items];

        public int AddCallCount { get; private set; }

        public int SaveChangesCallCount { get; private set; }

        public string? LastActiveConversationId { get; private set; }

        public Task<PendingAction?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.FirstOrDefault(pendingAction => pendingAction.Id == id));
        }

        public Task<PendingAction?> GetActiveByConversationIdAsync(string conversationId, CancellationToken cancellationToken)
        {
            LastActiveConversationId = conversationId;

            return Task.FromResult(
                Items.SingleOrDefault(pendingAction =>
                    pendingAction.ConversationId == conversationId
                    && ActiveStatuses.Contains(pendingAction.Status)));
        }

        public Task AddAsync(PendingAction pendingAction, CancellationToken cancellationToken)
        {
            AddCallCount++;
            Items.Add(pendingAction);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
