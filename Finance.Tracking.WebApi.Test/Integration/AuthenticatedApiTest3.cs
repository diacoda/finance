using Microsoft.Extensions.DependencyInjection;
namespace Finance.Tracking.Tests.Integration;

public class AuthenticatedApiTest3 : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;

    public AuthenticatedApiTest3(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AdminUser_CanAccessAdminEndpoint()
    {
        // Pass username through options when creating client
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.PostConfigure<FakeAuthOptions>("Fake", options =>
                {
                    options.Username = "admin";
                });
            });
        }).CreateClient();

        var response = await client.GetAsync("/api/accounts/names");
        response.EnsureSuccessStatusCode();
    }
}