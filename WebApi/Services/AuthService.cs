using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
namespace Finance.Tracking.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IConfiguration _config;
    private readonly JwtOptions _jwtOptions;

    public AuthService(UserManager<IdentityUser> userManager, IOptions<JwtOptions> jwtOptions, IConfiguration config)
    {
        _userManager = userManager;
        _jwtOptions = jwtOptions.Value;
        _config = config;
    }
    public async Task<string?> GenerateJwtTokenAsync(string userName, string password)
    {
        var user = await _userManager.FindByNameAsync(userName);
        if (user == null || !await _userManager.CheckPasswordAsync(user, password))
        {
            return null;
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, userName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_jwtOptions.ExpiresInHours),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}