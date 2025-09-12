namespace Finance.Tracking.Interfaces;

public interface IAuthService
{
    Task<string?> GenerateJwtTokenAsync(string userName, string password);
}