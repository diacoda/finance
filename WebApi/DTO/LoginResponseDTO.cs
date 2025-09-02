namespace Finance.Tracking.DTO;
// DTO for response
public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}