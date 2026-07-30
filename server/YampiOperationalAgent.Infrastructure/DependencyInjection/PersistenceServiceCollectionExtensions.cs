using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YampiOperationalAgent.Application.Abstractions;
using YampiOperationalAgent.Application.Services;
using YampiOperationalAgent.Infrastructure.Persistence;
using YampiOperationalAgent.Infrastructure.Persistence.Repositories;

namespace YampiOperationalAgent.Infrastructure.DependencyInjection;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(OperationalAgentDbContext.ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{OperationalAgentDbContext.ConnectionStringName}' is not configured.");
        }

        services.AddDbContext<OperationalAgentDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IPendingActionRepository, PendingActionRepository>();
        services.AddScoped<IPendingActionService, PendingActionService>();

        return services;
    }
}
