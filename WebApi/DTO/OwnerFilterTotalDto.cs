namespace Finance.Tracking.DTO;
// DTO for Owner and AccountFilter total
public class OwnerFilterTotalDto
{
    public string Owner { get; set; } = default!;
    public string AccountFilter { get; set; } = default!;
    public double Total { get; set; }
}