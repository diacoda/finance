using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Finance.Tracking.Tests.Integration;

public class AuthenticatedApiTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthenticatedApiTest(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Login_Returns_JwtToken()
    {

        var loginData = new { UserName = "admin", Password = "Admin987^" };
        var response = await _client.PostAsync("/api/auth/login",
            new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();
        Assert.Contains("token", responseString);
    }

    [Fact]
    public async Task GetAccountsNames_ReturnsExpectedAccounts()
    {
        // 1️⃣ Login to get token
        var loginData = new { UserName = "admin", Password = "Admin987^" };
        var loginResp = await _client.PostAsync("/api/auth/login",
            new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json"));
        loginResp.EnsureSuccessStatusCode();

        var loginJson = await loginResp.Content.ReadAsStringAsync();
        using var loginDoc = JsonDocument.Parse(loginJson);
        var token = loginDoc.RootElement.GetProperty("token").GetString();

        // 2️⃣ Call protected endpoint with token
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/accounts/names");
        response.EnsureSuccessStatusCode();

        // 3️⃣ Validate headers
        Assert.Equal("application/json; charset=utf-8",
            response.Content.Headers.ContentType?.ToString());

        // 4️⃣ Parse content
        var json = await response.Content.ReadAsStringAsync();
        var accounts = JsonSerializer.Deserialize<List<string>>(json);

        Assert.NotNull(accounts);
        Assert.NotEmpty(accounts);

        // 5️⃣ Validate expected data
        Assert.Contains("p1-TD-NonReg-CAD", accounts);
        Assert.Contains("p2-WS-RRSP-CAD", accounts);

        // 6️⃣ Optional: assert exact set if deterministic
        var expectedAccounts = new List<string>
        {
            "p1-TD-NonReg-CAD",
            "p1-TD-RRSP-CAD",
            "p1-TD-RESP-CAD",
            "p1-WS-NonReg-CAD",
            "p1-WS-RRSP-CAD",
            "p1-WS-TFSA-CAD",
            "p2-TD-NonReg-CAD",
            "p2-TD-RRSP-CAD",
            "p2-WS-NonReg-CAD",
            "p2-WS-RRSP-CAD"
        };

        Assert.Equal(expectedAccounts.OrderBy(x => x), accounts.OrderBy(x => x));
    }

}
