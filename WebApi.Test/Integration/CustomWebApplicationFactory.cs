using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Finance.Tracking;
using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace Finance.Tracking.Tests.Integration;


public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // In-memory configuration for testing
            var testConfig = new Dictionary<string, string?>
            {
                ["SeedAdmin:UserName"] = "admin",
                ["SeedAdmin:Email"] = "admin@test.com",
                ["SeedAdmin:Password"] = "Admin987^",
                ["Jwt:Key"] = "super-test-key-123456789-very-very-long-string-must-be-in-here",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience"
            };
            config.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureServices(services =>
        {
            services.Configure<JwtOptions>(options =>
            {
                options.Issuer = "TestIssuer";
                options.Audience = "TestAudience";
                options.ExpiresInHours = 1;
            });
        });
    }
}
