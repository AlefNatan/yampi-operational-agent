using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using YampiOperationalAgent.Application.Abstractions;
using YampiOperationalAgent.Infrastructure.Integrations;
using YampiOperationalAgent.Infrastructure.Options;

namespace YampiOperationalAgent.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<YampiOptions>(configuration.GetSection(YampiOptions.SectionName));
        services.PostConfigure<YampiOptions>(MergeWithEnvironmentVariables);

        services.AddHttpClient<IYampiClient, YampiClient>((serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<YampiOptions>>().Value;

            httpClient.BaseAddress = new Uri($"{options.ApiBaseUrl.TrimEnd('/')}/");
            httpClient.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        return services;
    }

    private static void MergeWithEnvironmentVariables(YampiOptions options)
    {
        var apiBaseUrl = Environment.GetEnvironmentVariable($"{YampiOptions.EnvironmentVariablePrefix}API_BASE_URL");
        var alias = Environment.GetEnvironmentVariable($"{YampiOptions.EnvironmentVariablePrefix}ALIAS");
        var userToken = Environment.GetEnvironmentVariable($"{YampiOptions.EnvironmentVariablePrefix}USER_TOKEN");
        var userSecretKey = Environment.GetEnvironmentVariable($"{YampiOptions.EnvironmentVariablePrefix}USER_SECRET_KEY");
        var timeoutSeconds = Environment.GetEnvironmentVariable($"{YampiOptions.EnvironmentVariablePrefix}TIMEOUT_SECONDS");

        if (!string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            options.ApiBaseUrl = apiBaseUrl;
        }

        if (!string.IsNullOrWhiteSpace(alias))
        {
            options.Alias = alias;
        }

        if (!string.IsNullOrWhiteSpace(userToken))
        {
            options.UserToken = userToken;
        }

        if (!string.IsNullOrWhiteSpace(userSecretKey))
        {
            options.UserSecretKey = userSecretKey;
        }

        if (int.TryParse(timeoutSeconds, out var parsedTimeout) && parsedTimeout > 0)
        {
            options.TimeoutSeconds = parsedTimeout;
        }
    }
}
