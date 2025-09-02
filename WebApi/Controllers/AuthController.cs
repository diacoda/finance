using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
namespace Finance.Tracking.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{

    public AuthController()
    {
    }

    // POST: api/auth/login
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult Login([FromBody] LoginRequest request)
    {
        if (request.UserName == "admin" && request.Password == "password")
        {
            // create claims
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, request.UserName),
            };

            // signing key
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("super-secret-key-12345-longer-wow-98765"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // token
            var token = new JwtSecurityToken(
                issuer: "Dan",
                audience: "FinanceApp",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new LoginResponse
            {
                Token = tokenString,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });
        }

        return Unauthorized();
    }
}
