namespace YampiOperationalAgent.Application.Contracts;

public sealed record OrderSummaryDto(
    long Id,
    long Number,
    string? Status,
    string? PaymentStatus,
    string? CustomerName,
    decimal? ValueTotal,
    bool Cancelled,
    bool Paid,
    DateTimeOffset? CreatedAt);
