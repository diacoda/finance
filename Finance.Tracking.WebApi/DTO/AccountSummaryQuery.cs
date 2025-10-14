namespace Finance.Tracking.DTO;
// Helper DTO for dynamic query
public class AccountSummaryQuery
{
    public string? Owner { get; set; }
    public AccountFilter? AccountFilter { get; set; }
    public DateOnly? AsOf { get; set; }
}