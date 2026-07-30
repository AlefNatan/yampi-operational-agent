using System.ComponentModel;
using ModelContextProtocol.Server;
using YampiOperationalAgent.Application.Abstractions;
using YampiOperationalAgent.Application.Contracts;

namespace YampiOperationalAgent.Mcp.Api.Tools;

[McpServerToolType]
[Description("Ferramentas MCP somente leitura para consultar dados da Yampi.")]
public sealed class YampiSearchTools(IYampiClient yampiClient)
{
    [McpServerTool(
        Name = "search_products",
        Title = "Search Products",
        ReadOnly = true,
        UseStructuredContent = true)]
    [Description("Busca produtos na Yampi por texto opcional. Use para listar produtos ou localizar itens por nome ou SKU.")]
    public async Task<IReadOnlyList<ProductSummaryDto>> SearchProductsAsync(
        [Description("Texto opcional para buscar por nome do produto ou SKU.")]
        string? query = null,
        CancellationToken cancellationToken = default)
    {
        return await yampiClient.SearchProductsAsync(NormalizeOptional(query), cancellationToken);
    }

    [McpServerTool(
        Name = "search_customers",
        Title = "Search Customers",
        ReadOnly = true,
        UseStructuredContent = true)]
    [Description("Busca clientes na Yampi por texto opcional. Use para localizar clientes por nome, email ou outros dados indexados pela plataforma.")]
    public async Task<IReadOnlyList<CustomerSummaryDto>> SearchCustomersAsync(
        [Description("Texto opcional para buscar clientes.")]
        string? query = null,
        CancellationToken cancellationToken = default)
    {
        return await yampiClient.SearchCustomersAsync(NormalizeOptional(query), cancellationToken);
    }

    [McpServerTool(
        Name = "search_orders",
        Title = "Search Orders",
        ReadOnly = true,
        UseStructuredContent = true)]
    [Description("Consulta pedidos na Yampi com filtro opcional por texto e por status.")]
    public async Task<IReadOnlyList<OrderSummaryDto>> SearchOrdersAsync(
        [Description("Texto opcional para buscar pedidos por numero, identificador ou nome do cliente.")]
        string? query = null,
        [Description("Status opcional do pedido ou pagamento, como paid, waiting_payment ou cancelled.")]
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        return await yampiClient.SearchOrdersAsync(
            NormalizeOptional(query),
            NormalizeOptional(status),
            cancellationToken);
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
