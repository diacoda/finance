namespace Finance.Tracking.Services;

public interface IAuthService
{
    Task<string?> GenerateJwtTokenAsync(string userName, string password);
}