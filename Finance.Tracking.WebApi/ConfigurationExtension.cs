
namespace Finance.Tracking.WebApi;

public static class ConfigurationExtensions
{
    public static IConfigurationBuilder AddAppConfiguration<T>(
        this IConfigurationBuilder builder,
        IHostEnvironment env) where T : class
    {
        builder
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

        if (!env.IsEnvironment("Testing"))
        {
            // Only load user-secrets outside of Testing
            builder.AddUserSecrets<T>(optional: true, reloadOnChange: true);
        }

        builder.AddEnvironmentVariables();

        return builder;
    }
}