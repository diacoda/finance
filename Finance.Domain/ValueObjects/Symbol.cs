namespace Finance.Domain.ValueObjects;

public sealed class Symbol : IEquatable<Symbol>
{
    public string Ticker { get; }
    public Symbol(string ticker)
    {
        if (string.IsNullOrWhiteSpace(ticker)) throw new ArgumentException("ticker required");
        Ticker = ticker.Trim().ToUpperInvariant();
    }
    public bool Equals(Symbol? other) => other is not null && Ticker == other.Ticker;
    public override bool Equals(object? obj) => Equals(obj as Symbol);
    public override int GetHashCode() => Ticker.GetHashCode();
    public override string ToString() => Ticker;
}
