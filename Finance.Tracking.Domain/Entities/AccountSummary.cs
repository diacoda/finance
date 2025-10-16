using System.Text.Json.Serialization;

namespace Finance.Tracking.Domain.Entities;

public class AccountSummary
{
    public required string Name { get; set; }
    public required string Owner { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AccountType Type { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AccountFilter AccountFilter { get; set; }
    public required Bank Bank { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Currency Currency { get; set; }
    public double Cash { get; set; } = 0.0;
    public double MarketValue { get; set; }
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public AssetClass AssetClass { get; set; }
}