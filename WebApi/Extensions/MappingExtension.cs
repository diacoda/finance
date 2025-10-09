namespace Finance.Tracking.Extensions;

using System.Collections.Generic;
using System.Linq;

public static class MappingExtensions
{
    public static AccountDTO MapAccountToDto(Account account)
    {
        if (account == null)
            throw new ArgumentNullException(nameof(account));

        // Extract "cash" from the holdings (Symbol = CASH)
        var cashHolding = account.Holdings
            .FirstOrDefault(h => h.Symbol == Symbol.CASH);

        return new AccountDTO
        {
            Name = account.Name,
            Cash = cashHolding?.Quantity ?? 0.0,
            Holdings = account.Holdings
                .Select(h => new HoldingDTO
                {
                    Symbol = h.Symbol,
                    Quantity = h.Quantity
                })
                .ToList()
        };
    }

    public static Account MapDtoToAccount(AccountDTO dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var parts = dto.Name.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4)
            throw new ArgumentException($"Invalid account key: {dto.Name}");

        var parsedType = Enum.Parse<AccountType>(parts[2], true);

        var account = new Account
        {
            Name = dto.Name,
            Owner = parts[0],
            Bank = Enum.Parse<Bank>(parts[1], true),
            Type = parsedType,
            AccountFilter = parsedType.ToAccountFilter(),
            MarketValue = 0,
            Currency = Enum.Parse<Currency>(parts[3], true),
            //Cash = dto.Cash,
            Holdings = dto.Holdings.Select(h => new Holding
            {
                Symbol = h.Symbol,           // no parsing needed
                Quantity = h.Quantity,
                AccountName = dto.Name
            }).ToList()
        };

        /* Need to review this
        // If DTO cash is provided, ensure we sync CASH holding
        if (dto.Cash > 0)
        {
            var existingCash = account.Holdings.FirstOrDefault(h => h.Symbol == Symbol.CASH);
            if (existingCash == null)
            {
                account.Holdings.Add(new Holding
                {
                    Symbol = Symbol.CASH,
                    Quantity = dto.Cash,
                    AccountName = dto.Name
                });
            }
            else
            {
                existingCash.Quantity = dto.Cash;
            }
        }
        */
        return account;
    }
}
