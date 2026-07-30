namespace YampiOperationalAgent.Infrastructure.Options;

public sealed class YampiOptions
{
    public const string SectionName = "Yampi";
    public const string EnvironmentVariablePrefix = "YAMPI_";

    public string ApiBaseUrl { get; set; } = "https://api.dooki.com.br/v2";
    public string Alias { get; set; } = string.Empty;
    public string UserToken { get; set; } = string.Empty;
    public string UserSecretKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}
