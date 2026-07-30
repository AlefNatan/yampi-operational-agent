namespace YampiOperationalAgent.Application.Contracts;

public sealed record ProductSummaryDto(
    long Id,
    string Name,
    string? Sku,
    bool Active,
    bool HasVariations,
    int? TotalInStock,
    string? Url);
