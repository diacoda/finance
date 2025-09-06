using Microsoft.EntityFrameworkCore;

namespace Finance.Tracking.Data;

public class FinanceDbContext : DbContext
{
    public DbSet<AccountSummary> AccountSummaries { get; set; }
    public DbSet<Price> Prices { get; set; }

    public DbSet<TotalMarketValue> TotalMarketValues { get; set; }

    public DbSet<Account> Accounts { get; set; } = default!;
    public DbSet<Holding> Holdings { get; set; } = default!;

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

        modelBuilder.Entity<Account>()
            .HasKey(a => a.Name);

        modelBuilder.Entity<Account>()
            .HasMany(a => a.Holdings)
            .WithOne(h => h.Account)
            .HasForeignKey(h => h.AccountName)
            .HasPrincipalKey(a => a.Name)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Holding>()
            .HasKey(h => h.Id); // <-- added primary key
        modelBuilder.Entity<Holding>()
            .Property(h => h.AccountName)
            .IsRequired();
        modelBuilder.Entity<Holding>()
            .HasIndex(h => new { h.AccountName, h.Symbol })
            .IsUnique();
    }
}
