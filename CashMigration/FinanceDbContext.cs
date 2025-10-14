using Microsoft.EntityFrameworkCore;

public class FinanceDbContext : DbContext
{
    public DbSet<Account> Accounts { get; set; } = default!;
    public DbSet<Holding> Holdings { get; set; } = default!;

    public FinanceDbContext(DbContextOptions<FinanceDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
