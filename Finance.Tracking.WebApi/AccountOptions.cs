namespace Finance.Tracking.WebApi;

public class AccountOptions
{
    public Dictionary<string, AccountConfiguration> Accounts { get; set; } = new();
}