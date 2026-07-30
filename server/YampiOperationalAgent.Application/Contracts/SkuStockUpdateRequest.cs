namespace YampiOperationalAgent.Application.Contracts;

public sealed record SkuStockUpdateRequest(
    int Quantity,
    int MinQuantity);
