namespace Finance.Infrastructure.Persistence;

using Finance.Domain.Entities;
using Finance.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public DbSet<Account> Accounts { get; set; } = null!; // mapped manually


    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public async Task AddAsync(Account account)
    {
        await base.AddAsync(account);
    }

    public async Task SaveChangesAsync()
    {
        await base.SaveChangesAsync();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Account mapping
        modelBuilder.Entity<Account>(b =>
        {
            b.ToTable("Accounts");
            b.HasKey("Id");


            // Map scalar props
            b.Property<Guid>("Id");
            b.Property(a => a.Name).HasField("Name");
            b.Property(a => a.Owner).HasField("Owner");


            // Map holdings as owned collection - requires a private backing field
            b.OwnsMany(typeof(Holding), "_holdings", hb =>
            {
                hb.ToTable("Holdings");
                hb.WithOwner().HasForeignKey("AccountId");
                hb.Property<Guid>("Id");
                hb.HasKey("Id");


                hb.Property<decimal>("Quantity");
                hb.Property<string>("_symbol").HasColumnName("Symbol");
                hb.Property<decimal>("_costBasisAmount").HasColumnName("CostBasisAmount");
                hb.Property<string>("_costBasisCurrency").HasColumnName("CostBasisCurrency");
            });
        });


        base.OnModelCreating(modelBuilder);
    }
}