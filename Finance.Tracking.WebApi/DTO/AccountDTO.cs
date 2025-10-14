namespace Finance.Tracking.DTO;

public class AccountDTO
{
    public required string Name { get; set; }
    public double Cash { get; set; }
    // Change from Dictionary to ICollection<Holding>
    public ICollection<HoldingDTO> Holdings { get; set; } = new List<HoldingDTO>();
    public double MarketValue { get; set; }
}
