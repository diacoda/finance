public class OwnerTypeAccountNameDTO
{
    public string Owner { get; set; } = "";
    public string Type { get; set; } = "";
    public double Total { get; set; }
    public List<string> AccountNames { get; set; } = new();
}