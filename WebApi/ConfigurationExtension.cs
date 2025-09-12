
namespace Finance.Tracking;

public static class ConfigurationExtensions
{
    public static IConfigurationBuilder AddAppConfiguration<T>(this IConfigurationBuilder builder) where T : class
    {
        return builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                      .AddUserSecrets<T>(optional: true, reloadOnChange: true)
                      .AddEnvironmentVariables();
    }
}