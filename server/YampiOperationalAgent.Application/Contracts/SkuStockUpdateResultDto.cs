namespace YampiOperationalAgent.Application.Contracts;

public sealed record SkuStockUpdateResultDto(
    long SkuId,
    int PreviousQuantity,
    int CurrentQuantity,
    int PreviousMinQuantity,
    int CurrentMinQuantity);
