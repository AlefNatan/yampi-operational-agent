namespace YampiOperationalAgent.Application.Contracts;

public sealed record SkuPriceUpdateResultDto(
    long Id,
    string Sku,
    decimal PreviousPriceSale,
    decimal CurrentPriceSale);
