namespace YampiOperationalAgent.Application.Contracts;

public sealed record CustomerSummaryDto(
    long Id,
    string Name,
    string? Email,
    bool Active,
    string? Type,
    string? Phone);
