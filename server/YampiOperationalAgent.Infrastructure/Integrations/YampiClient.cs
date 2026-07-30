using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using YampiOperationalAgent.Application.Abstractions;
using YampiOperationalAgent.Application.Contracts;
using YampiOperationalAgent.Infrastructure.Options;

namespace YampiOperationalAgent.Infrastructure.Integrations;

internal sealed class YampiClient(
    HttpClient httpClient,
    IOptions<YampiOptions> yampiOptions) : IYampiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly YampiOptions _options = yampiOptions.Value;

    public async Task<IReadOnlyList<ProductSummaryDto>> SearchProductsAsync(string? query, CancellationToken cancellationToken)
    {
        var items = await GetPagedDataAsync($"{GetAlias()}/catalog/products", ["include=skus"], cancellationToken);

        return items
            .Select(MapProduct)
            .Where(product => MatchesProduct(product, query))
            .ToArray();
    }

    public async Task<SkuPriceUpdateResultDto> UpdateSkuPriceAsync(
        long skuId,
        SkuPriceUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var sku = await GetSkuByIdAsync(skuId, cancellationToken);

        var payload = new UpdateSkuPriceRequest
        {
            PriceSale = request.PriceSale
        };

        var updatedSku = await UpdateSkuAsync(skuId, payload, cancellationToken);

        return new SkuPriceUpdateResultDto(
            updatedSku?.Id ?? sku.Id,
            updatedSku?.Sku ?? sku.Sku,
            sku.PriceSale,
            updatedSku?.PriceSale ?? request.PriceSale);
    }

    public async Task<SkuStockUpdateResultDto> UpdateSkuStockAsync(
        long skuId,
        SkuStockUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var sku = await GetSkuByIdAsync(skuId, cancellationToken);

        var payload = new UpdateSkuStockRequest
        {
            Availability = request.Quantity,
            AvailabilitySoldout = request.MinQuantity
        };

        var updatedSku = await UpdateSkuAsync(skuId, payload, cancellationToken);

        return new SkuStockUpdateResultDto(
            sku.Id,
            sku.Availability,
            updatedSku?.Availability ?? request.Quantity,
            sku.AvailabilitySoldout,
            updatedSku?.AvailabilitySoldout ?? request.MinQuantity);
    }

    public async Task<IReadOnlyList<CustomerSummaryDto>> SearchCustomersAsync(string? query, CancellationToken cancellationToken)
    {
        var parameters = new List<string>();
        if (!string.IsNullOrWhiteSpace(query))
        {
            parameters.Add($"q={Uri.EscapeDataString(query)}");
        }

        var items = await GetPagedDataAsync($"{GetAlias()}/customers", parameters, cancellationToken);

        return items.Select(MapCustomer).ToArray();
    }

    public async Task<IReadOnlyList<OrderSummaryDto>> SearchOrdersAsync(
        string? query,
        string? status,
        CancellationToken cancellationToken)
    {
        var parameters = new List<string>();

        if (!string.IsNullOrWhiteSpace(query))
        {
            parameters.Add($"q={Uri.EscapeDataString(query)}");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            parameters.Add($"filters[status]={Uri.EscapeDataString(status)}");
        }

        var items = await GetPagedDataAsync($"{GetAlias()}/orders", parameters, cancellationToken);
        var orders = items.Select(MapOrder).ToArray();

        return ApplyOrderFilters(orders, query, status);
    }

    private async Task<IReadOnlyList<JsonElement>> GetPagedDataAsync(
        string endpoint,
        IReadOnlyCollection<string> parameters,
        CancellationToken cancellationToken)
    {
        const int pageSize = 100;
        var results = new List<JsonElement>();
        var currentPage = 1;
        int? totalPages = null;

        while (true)
        {
            var pageParameters = new List<string>(parameters)
            {
                $"page={currentPage}",
                $"limit={pageSize}"
            };

            using var document = await SendForDocumentAsync(
                HttpMethod.Get,
                BuildEndpoint(endpoint, pageParameters),
                cancellationToken);

            if (document.RootElement.TryGetProperty("data", out var dataElement)
                && dataElement.ValueKind == JsonValueKind.Array)
            {
                results.AddRange(dataElement.EnumerateArray().Select(item => item.Clone()));
            }

            totalPages ??= ExtractTotalPages(document.RootElement);

            if (totalPages is null || currentPage >= totalPages.Value)
            {
                break;
            }

            currentPage++;
        }

        return results;
    }

    private async Task<YampiSkuResponse?> UpdateSkuAsync<TRequest>(
        long skuId,
        TRequest payload,
        CancellationToken cancellationToken)
    {
        var responsePayload = await SendAsync(
            HttpMethod.Put,
            $"{GetAlias()}/catalog/skus/{skuId}",
            JsonSerializer.Serialize(payload, JsonOptions),
            cancellationToken);

        return DeserializeSkuResponse(responsePayload);
    }

    private async Task<SkuSummaryDto> GetSkuByIdAsync(long skuId, CancellationToken cancellationToken)
    {
        var items = await GetPagedDataAsync(
            $"{GetAlias()}/catalog/skus",
            ["include=current_stock"],
            cancellationToken);

        var sku = items
            .Select(DeserializeSku)
            .OfType<YampiSkuResponse>()
            .Select(MapSku)
            .FirstOrDefault(item => item.Id == skuId);

        return sku ?? throw new InvalidOperationException($"SKU {skuId} was not found.");
    }

    private async Task<string> SendAsync(
        HttpMethod method,
        string endpoint,
        string? content,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, endpoint);
        ApplyAuthenticationHeaders(request);

        if (content is not null)
        {
            request.Content = new StringContent(content, Encoding.UTF8, "application/json");
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw CreateRequestException(response, payload);
        }

        return payload;
    }

    private async Task<JsonDocument> SendForDocumentAsync(
        HttpMethod method,
        string endpoint,
        CancellationToken cancellationToken)
    {
        var payload = await SendAsync(method, endpoint, null, cancellationToken);
        return JsonDocument.Parse(payload);
    }

    private void ApplyAuthenticationHeaders(HttpRequestMessage request)
    {
        if (string.IsNullOrWhiteSpace(_options.UserToken)
            || string.IsNullOrWhiteSpace(_options.UserSecretKey))
        {
            throw new InvalidOperationException(
                "Yampi credentials are not configured. Set Yampi:UserToken and Yampi:UserSecretKey.");
        }

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("User-Token", _options.UserToken);
        request.Headers.TryAddWithoutValidation("User-Secret-Key", _options.UserSecretKey);
    }

    private string GetAlias()
    {
        if (string.IsNullOrWhiteSpace(_options.Alias))
        {
            throw new InvalidOperationException("Yampi alias is not configured. Set Yampi:Alias.");
        }

        return _options.Alias.Trim();
    }

    private static HttpRequestException CreateRequestException(HttpResponseMessage response, string payload)
    {
        var publicMessage = TryExtractPublicMessage(payload);
        var suffix = string.IsNullOrWhiteSpace(publicMessage)
            ? string.Empty
            : $": {publicMessage}";

        return new HttpRequestException(
            $"Yampi request failed with status {(int)response.StatusCode} ({response.ReasonPhrase}){suffix}");
    }

    private static string? TryExtractPublicMessage(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            foreach (var propertyName in new[] { "message", "error", "detail" })
            {
                if (root.TryGetProperty(propertyName, out var property)
                    && property.ValueKind == JsonValueKind.String)
                {
                    var value = property.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    private static string BuildEndpoint(string endpoint, IReadOnlyCollection<string> parameters)
    {
        if (parameters.Count == 0)
        {
            return endpoint;
        }

        return $"{endpoint}?{string.Join("&", parameters)}";
    }

    private static int? ExtractTotalPages(JsonElement root)
    {
        foreach (var candidate in new int?[]
        {
            GetNestedInt32(root, "meta", "pagination", "total_pages"),
            GetNestedInt32(root, "meta", "pagination", "last_page"),
            GetNestedInt32(root, "meta", "page_count"),
            GetNestedInt32(root, "meta", "last_page")
        })
        {
            if (candidate is > 0)
            {
                return candidate;
            }
        }

        return null;
    }

    private static ProductSummaryDto MapProduct(JsonElement element)
    {
        return new ProductSummaryDto(
            GetInt64(element, "id"),
            GetString(element, "name") ?? string.Empty,
            GetString(element, "sku"),
            GetBoolean(element, "active"),
            GetBoolean(element, "has_variations"),
            GetInt32OrNull(element, "total_in_stock"),
            GetString(element, "url"));
    }

    private static CustomerSummaryDto MapCustomer(JsonElement element)
    {
        var phone = GetNestedString(element, "phone", "full_number")
            ?? GetNestedString(element, "phone", "formated_number");

        return new CustomerSummaryDto(
            GetInt64(element, "id"),
            GetString(element, "name") ?? string.Empty,
            GetString(element, "email"),
            GetBoolean(element, "active"),
            GetString(element, "type"),
            phone);
    }

    private static OrderSummaryDto MapOrder(JsonElement order)
    {
        var paymentStatus = GetPaymentStatus(order);
        var statusName = GetNestedString(order, "status", "data", "name");

        return new OrderSummaryDto(
            GetInt64(order, "id"),
            GetInt64(order, "number"),
            statusName,
            paymentStatus,
            GetNestedString(order, "customer", "data", "name"),
            GetDecimal(order, "value_total"),
            GetBoolean(order, "cancelled"),
            IsPaid(order, paymentStatus, statusName),
            ParseDate(GetNestedString(order, "created_at", "date")));
    }

    private static bool MatchesProduct(ProductSummaryDto product, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return product.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(product.Sku)
                && product.Sku.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<OrderSummaryDto> ApplyOrderFilters(
        IReadOnlyList<OrderSummaryDto> orders,
        string? query,
        string? status)
    {
        IEnumerable<OrderSummaryDto> filtered = orders;

        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(order =>
                order.CustomerName?.Contains(query, StringComparison.OrdinalIgnoreCase) == true
                || order.Number.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)
                || order.Id.ToString().Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            filtered = filtered.Where(order =>
                order.PaymentStatus?.Contains(status, StringComparison.OrdinalIgnoreCase) == true
                || order.Status?.Contains(status, StringComparison.OrdinalIgnoreCase) == true
                || MatchesOrderStatusAlias(order, status));
        }

        return filtered.ToArray();
    }

    private static bool MatchesOrderStatusAlias(OrderSummaryDto order, string status)
    {
        var normalized = status.Trim().ToLowerInvariant();

        return normalized switch
        {
            "paid" => order.Paid || string.Equals(order.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase),
            "waiting_payment" => string.Equals(order.PaymentStatus, "waiting_payment", StringComparison.OrdinalIgnoreCase),
            "cancelled" => order.Cancelled,
            _ => false
        };
    }

    private static bool IsPaid(JsonElement order, string? paymentStatus, string? statusName)
    {
        if (GetBoolean(order, "paid"))
        {
            return true;
        }

        return string.Equals(paymentStatus, "paid", StringComparison.OrdinalIgnoreCase)
            || string.Equals(statusName, "Pagamento aprovado", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetPaymentStatus(JsonElement order)
    {
        if (!order.TryGetProperty("transactions", out var transactionsElement))
        {
            return null;
        }

        if (!transactionsElement.TryGetProperty("data", out var transactionDataElement))
        {
            return null;
        }

        if (transactionDataElement.ValueKind == JsonValueKind.Object)
        {
            return GetNestedString(transactionDataElement, "status", "name");
        }

        if (transactionDataElement.ValueKind == JsonValueKind.String)
        {
            return transactionDataElement.GetString();
        }

        if (transactionDataElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in transactionDataElement.EnumerateArray())
            {
                var status = GetNestedString(item, "status", "name");
                if (!string.IsNullOrWhiteSpace(status))
                {
                    return status;
                }
            }
        }

        return null;
    }

    private static DateTimeOffset? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTimeOffset.TryParse(value, out var parsed)
            ? parsed
            : null;
    }

    private static YampiSkuResponse? DeserializeSkuResponse(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        if (root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("data", out var dataElement)
            && dataElement.ValueKind == JsonValueKind.Object)
        {
            return JsonSerializer.Deserialize<YampiSkuResponse>(dataElement.GetRawText(), JsonOptions);
        }

        if (root.ValueKind == JsonValueKind.Object)
        {
            return JsonSerializer.Deserialize<YampiSkuResponse>(root.GetRawText(), JsonOptions);
        }

        return null;
    }

    private static YampiSkuResponse? DeserializeSku(JsonElement element)
        => JsonSerializer.Deserialize<YampiSkuResponse>(element.GetRawText(), JsonOptions);

    private static SkuSummaryDto MapSku(YampiSkuResponse sku)
    {
        return new SkuSummaryDto(
            sku.Id,
            sku.ProductId,
            sku.Sku ?? string.Empty,
            sku.Title,
            sku.PriceSale,
            sku.PriceCost,
            sku.QuantityManaged,
            sku.Availability,
            sku.AvailabilitySoldout,
            sku.BlockedSale,
            sku.Weight,
            sku.Height,
            sku.Width,
            sku.Length,
            sku.AllowSellWithoutCustomization,
            sku.Order,
            sku.VariationsValuesIds ?? [],
            NormalizeCurrentStock(sku.CurrentStock));
    }

    private static IReadOnlyList<SkuStockSummaryDto> NormalizeCurrentStock(JsonElement? currentStockElement)
    {
        if (currentStockElement is null)
        {
            return [];
        }

        var element = currentStockElement.Value;
        if (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty("data", out var dataElement))
        {
            element = dataElement;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            var single = MapSkuStock(element);
            return single is null ? [] : [single];
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var items = new List<SkuStockSummaryDto>();
        foreach (var item in element.EnumerateArray())
        {
            var mapped = MapSkuStock(item);
            if (mapped is not null)
            {
                items.Add(mapped);
            }
        }

        return items;
    }

    private static SkuStockSummaryDto? MapSkuStock(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return new SkuStockSummaryDto(
            GetInt64(element, "id"),
            GetInt64(element, "stock_id"),
            (int)GetInt64(element, "quantity"),
            (int)GetInt64(element, "min_quantity"));
    }

    private static string? GetNestedString(JsonElement element, params string[] path)
    {
        var current = element;

        foreach (var segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
            {
                return null;
            }
        }

        return current.ValueKind switch
        {
            JsonValueKind.String => current.GetString(),
            JsonValueKind.Number => current.ToString(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            _ => null
        };
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.ToString(),
            _ => null
        };
    }

    private static long GetInt64(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return 0;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetInt64(out var value) => value,
            JsonValueKind.String when long.TryParse(property.GetString(), out var value) => value,
            _ => 0
        };
    }

    private static int? GetInt32OrNull(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetInt32(out var value) => value,
            JsonValueKind.String when int.TryParse(property.GetString(), out var value) => value,
            _ => null
        };
    }

    private static int? GetNestedInt32(JsonElement element, params string[] path)
    {
        var value = GetNestedString(element, path);
        return int.TryParse(value, out var parsed) ? parsed : null;
    }

    private static decimal? GetDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetDecimal(out var value) => value,
            JsonValueKind.String when decimal.TryParse(property.GetString(), out var value) => value,
            _ => null
        };
    }

    private static bool GetBoolean(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        return property.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(property.GetString(), out var value) => value,
            JsonValueKind.Number when property.TryGetInt32(out var value) => value != 0,
            _ => false
        };
    }

    private sealed class YampiSkuResponse
    {
        public long Id { get; init; }

        [JsonPropertyName("product_id")]
        public long ProductId { get; init; }

        public string? Sku { get; init; }
        public string? Title { get; init; }

        [JsonPropertyName("price_sale")]
        public decimal PriceSale { get; init; }

        [JsonPropertyName("price_cost")]
        public decimal PriceCost { get; init; }

        [JsonPropertyName("quantity_managed")]
        public bool QuantityManaged { get; init; }

        public int Availability { get; init; }

        [JsonPropertyName("availability_soldout")]
        public int AvailabilitySoldout { get; init; }

        [JsonPropertyName("blocked_sale")]
        public bool BlockedSale { get; init; }

        public decimal Weight { get; init; }
        public decimal Height { get; init; }
        public decimal Width { get; init; }
        public decimal Length { get; init; }

        [JsonPropertyName("allow_sell_without_customization")]
        public bool AllowSellWithoutCustomization { get; init; }

        public int Order { get; init; }

        [JsonPropertyName("variations_values_ids")]
        public List<string>? VariationsValuesIds { get; init; }

        [JsonPropertyName("current_stock")]
        public JsonElement? CurrentStock { get; init; }
    }

    private sealed class UpdateSkuPriceRequest
    {
        [JsonPropertyName("price_sale")]
        public decimal PriceSale { get; init; }
    }

    private sealed class UpdateSkuStockRequest
    {
        [JsonPropertyName("availability")]
        public int Availability { get; init; }

        [JsonPropertyName("availability_soldout")]
        public int AvailabilitySoldout { get; init; }
    }
}
