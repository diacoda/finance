using System.Text.Json.Serialization;

public class TotalMarketValue
{
    [JsonPropertyName("type")]
    public TotalMarketValueType Type { get; set; } // Name identifier, e.g., "TotalMarketValue"
    [JsonPropertyName("asOf")]
    public DateOnly AsOf { get; set; } // Date of the total market value
    [JsonPropertyName("marketValue")]
    public double MarketValue { get; set; } // Total market value amount
}