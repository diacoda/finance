using Microsoft.AspNetCore.Identity;

namespace Finance.Tracking.WebApi;

public static class DbSeederExtensions
{
    public static async Task SeedDatabasesAsync(this IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();

        EnsureDatabasesCreated(scope);
        await SeedAdminUserAsync(scope, config);
        await LoadPricesAsync(scope);
        //await InitializeAccountsAsync(scope);
    }

    // -------------------------
    // Ensure Databases Exist
    // -------------------------
    private static void EnsureDatabasesCreated(IServiceScope scope)
    {
        var financeDb = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        financeDb.Database.EnsureCreated();

        var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        identityDb.Database.EnsureCreated();
    }

    // -------------------------
    // Seed Admin User & Role
    // -------------------------
    private static async Task SeedAdminUserAsync(IServiceScope scope, IConfiguration config)
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        string userName = config["SeedAdmin:UserName"] ?? string.Empty;
        string email = config["SeedAdmin:Email"] ?? string.Empty;
        string password = config["SeedAdmin:Password"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userName) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Admin user details are not properly configured in secrets.");
        }

        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole("Admin"));

        if (await userManager.FindByNameAsync(userName) == null)
        {
            var admin = new IdentityUser
            {
                UserName = userName,
                Email = email,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(admin, password);
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }

    // -------------------------
    // Load Prices
    // -------------------------
    private static async Task LoadPricesAsync(IServiceScope scope)
    {
        bool loadPrices = false; // could make configurable
        if (!loadPrices) return;

        var prices = CsvPriceLoader.LoadPricesFromCsv("daily.csv");
        var pricingService = scope.ServiceProvider.GetRequiredService<IPricingService>();
        await pricingService.SavePricesAsync(prices);
    }

    // -------------------------
    // Initialize Accounts
    // -------------------------
    /*
    private static async Task InitializeAccountsAsync(IServiceScope scope)
    {
        var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();
        //await accountService.InitializeAsync();
    }
    */
}
