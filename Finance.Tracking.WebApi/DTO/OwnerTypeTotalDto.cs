namespace Finance.Tracking.DTO;
// DTO for Owner and AccountFilter total
public class OwnerTypeTotalDto
{
    public string Owner { get; set; } = default!;
    public string Type { get; set; } = default!;
    public double Total { get; set; }
}
