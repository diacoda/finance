using Microsoft.EntityFrameworkCore;
using Finance.Tracking.WebApi;

Console.WriteLine("Starting cash holdings migration...");

string connectionString = "Data Source=" + PathHelper.GetPath("Db/finance.db"); // adjust path as needed
var options = new DbContextOptionsBuilder<FinanceDbContext>()
    .UseSqlite(connectionString) // adjust path as needed
    .Options;

using var db = new FinanceDbContext(options);

// Ensure database exists
if (!db.Database.CanConnect())
{
    Console.WriteLine("❌ Cannot connect to database.");
    return;
}

var accounts = await db.Accounts
    .Include(a => a.Holdings)
    .ToListAsync();

int created = 0;

foreach (var account in accounts)
{
    if (account.Cash <= 0)
        continue;

    bool hasCashHolding = account.Holdings.Any(h => h.Symbol == Symbol.CASH);
    if (hasCashHolding)
        continue; // skip if already present

    var cashHolding = new Holding
    {
        Symbol = Symbol.CASH,
        Quantity = account.Cash,
        AccountName = account.Name
    };

    account.Holdings.Add(cashHolding);
    db.Holdings.Add(cashHolding);
    created++;

    Console.WriteLine($"Added CASH holding for {account.Name} with {account.Cash}");
}

await db.SaveChangesAsync();

Console.WriteLine($"✅ Migration complete. Added {created} cash holdings.");
