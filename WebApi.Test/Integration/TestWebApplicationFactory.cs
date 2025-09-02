using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Finance.Tracking.Services;
using System.Linq;

namespace Finance.Tracking.Tests.Integration;

public class TestWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    public Mock<IHistoryService> HistoryServiceMock { get; } = new Mock<IHistoryService>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing IHistoryService
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IHistoryService));
            if (descriptor != null) services.Remove(descriptor);

            // Add mocked IHistoryService
            services.AddSingleton(HistoryServiceMock.Object);

            // Replace authentication with fake and set as default
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Fake";
                options.DefaultChallengeScheme = "Fake";
            })
            .AddScheme<AuthenticationSchemeOptions, FakeAuthHandler>("Fake", options => { });
        });
    }
}
