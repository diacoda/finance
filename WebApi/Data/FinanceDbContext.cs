using Microsoft.EntityFrameworkCore;

namespace Finance.Tracking.Data;

public class FinanceDbContext : DbContext
{
    public DbSet<AccountSummary> AccountSummaries { get; set; }
    public DbSet<Price> Prices { get; set; }

    public DbSet<TotalMarketValue> TotalMarketValues { get; set; }

    public FinanceDbContext(DbContextOptions<FinanceDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountSummary>()
            .ToTable("AccountSummaries", t => t.ExcludeFromMigrations())
            .HasKey(a => new { a.Name, a.Date });

        modelBuilder.Entity<Price>()
            .ToTable("Prices", t => t.ExcludeFromMigrations())
            .HasKey(p => new { p.Symbol, p.Date });

        // Index on parsed fields for faster queries
        modelBuilder.Entity<AccountSummary>()
            .HasIndex(a => new { a.Owner, a.Type, a.Bank, a.Currency, a.Date });

        modelBuilder.Entity<TotalMarketValue>()
            .HasKey(a => new { a.AsOf, a.Type });

    }
}
