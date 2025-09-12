using Microsoft.AspNetCore.Authentication;

namespace Finance.Tracking.Tests.Integration;

public class FakeAuthOptions : AuthenticationSchemeOptions
{
    public string Username { get; set; } = "admin";
}