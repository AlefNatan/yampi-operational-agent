using ModelContextProtocol.AspNetCore;
using YampiOperationalAgent.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services
    .AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.Stateless = true;
    });

var app = builder.Build();

app.MapMcp("/mcp");

app.Run();
