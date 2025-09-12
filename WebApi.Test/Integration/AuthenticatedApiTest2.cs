using System.Text.Json;

namespace Finance.Tracking.Tests.Integration;

public class AuthenticatedApiTest2 : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthenticatedApiTest2(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_StillWorksWithFakeAuth()
    {
        // Because FakeAuthHandler automatically injects a user,
        // we can call a protected endpoint directly.
        var response = await _client.GetAsync("/api/accounts/names");

        response.EnsureSuccessStatusCode();

        Assert.Equal("application/json; charset=utf-8",
            response.Content.Headers.ContentType?.ToString());

        var json = await response.Content.ReadAsStringAsync();
        var accounts = JsonSerializer.Deserialize<List<string>>(json);

        Assert.NotNull(accounts);
        Assert.NotEmpty(accounts);
        Assert.Contains("Dan-TD-NonReg-CAD", accounts);
        Assert.Contains("Oana-WS-TFSA-CAD", accounts);
    }

    [Fact]
    public async Task ProtectedEndpoint_ReturnsExpectedAccounts()
    {
        var response = await _client.GetAsync("/api/accounts/names");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var accounts = JsonSerializer.Deserialize<List<string>>(json);

        var expectedAccounts = new List<string>
        {
            "Dan-TD-NonReg-CAD",
            "Dan-TD-RESP-CAD",
            "Dan-TD-RRSP-CAD",
            "Dan-WS-LIRAFederal-CAD",
            "Dan-WS-LIRAProvincial-CAD",
            "Dan-WS-NonReg-CAD",
            "Dan-WS-RRSP-CAD",
            "Dan-WS-TFSA-CAD",
            "Oana-TD-NonReg-CAD",
            "Oana-TD-RRSPSpousal-CAD",
            "Oana-TD-TFSA-CAD",
            "Oana-WS-LIRAProvincial-CAD",
            "Oana-WS-NonReg-CAD",
            "Oana-WS-RRSP-CAD",
            "Oana-WS-TFSA-CAD"
        };

        Assert.Equal(expectedAccounts.OrderBy(x => x), accounts!.OrderBy(x => x));
    }
}
