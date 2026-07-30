namespace YampiOperationalAgent.Application.Contracts;

public sealed record SkuSummaryDto(
    long Id,
    long ProductId,
    string Sku,
    string? Title,
    decimal PriceSale,
    decimal PriceCost,
    bool QuantityManaged,
    int Availability,
    int AvailabilitySoldout,
    bool BlockedSale,
    decimal Weight,
    decimal Height,
    decimal Width,
    decimal Length,
    bool AllowSellWithoutCustomization,
    int Order,
    IReadOnlyList<string> VariationsValuesIds,
    IReadOnlyList<SkuStockSummaryDto> CurrentStock);
