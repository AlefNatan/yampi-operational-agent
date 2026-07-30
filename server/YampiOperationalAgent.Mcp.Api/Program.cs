using ModelContextProtocol.AspNetCore;
using YampiOperationalAgent.Infrastructure.DependencyInjection;
using YampiOperationalAgent.Mcp.Api.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services
    .AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.Stateless = true;
    })
    .WithToolsFromAssembly(typeof(YampiSearchTools).Assembly);

var app = builder.Build();

app.MapMcp("/mcp");

app.Run();
