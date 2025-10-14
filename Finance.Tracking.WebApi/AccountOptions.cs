namespace Finance.Tracking.WebApi;

public class AccountOptions
{
    public Dictionary<string, AccountRaw> Accounts { get; set; } = new();
}