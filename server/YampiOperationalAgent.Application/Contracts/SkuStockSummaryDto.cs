namespace YampiOperationalAgent.Application.Contracts;

public sealed record SkuStockSummaryDto(
    long Id,
    long StockId,
    int Quantity,
    int MinQuantity);
