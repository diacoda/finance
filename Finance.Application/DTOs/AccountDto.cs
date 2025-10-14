namespace Finance.Application.DTOs;

public record HoldingDto(string Symbol, decimal Quantity, decimal CostBasis);
public record AccountDto(System.Guid Id, string Name, string Owner, System.Collections.Generic.IEnumerable<HoldingDto> Holdings);
