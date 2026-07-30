using YampiOperationalAgent.Domain.Enums;

namespace YampiOperationalAgent.Application.Contracts;

public sealed record CreateOrReplacePendingActionRequest(
    string ConversationId,
    PendingActionType Type,
    long SkuId,
    string? SkuCode,
    string SkuName,
    decimal CurrentValue,
    decimal NewValue);
