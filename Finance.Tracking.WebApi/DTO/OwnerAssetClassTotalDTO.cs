namespace Finance.Tracking.DTO;

public class OwnerAssetClassTotalDTO
{
    public string Owner { get; set; } = string.Empty;
    public string AssetClass { get; set; } = string.Empty;
    public double Percentage { get; set; }
    public double Total { get; set; }
}