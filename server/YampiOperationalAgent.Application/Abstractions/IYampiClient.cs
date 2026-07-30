using YampiOperationalAgent.Application.Contracts;

namespace YampiOperationalAgent.Application.Abstractions;

public interface IYampiClient
{
    Task<IReadOnlyList<ProductSummaryDto>> SearchProductsAsync(string? query, CancellationToken cancellationToken);
    Task<SkuPriceUpdateResultDto> UpdateSkuPriceAsync(long skuId, SkuPriceUpdateRequest request, CancellationToken cancellationToken);
    Task<SkuStockUpdateResultDto> UpdateSkuStockAsync(long skuId, SkuStockUpdateRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<CustomerSummaryDto>> SearchCustomersAsync(string? query, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrderSummaryDto>> SearchOrdersAsync(string? query, string? status, CancellationToken cancellationToken);
}
