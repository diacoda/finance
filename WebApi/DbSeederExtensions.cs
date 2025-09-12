using Microsoft.AspNetCore.Identity;

namespace Finance.Tracking;

public static class DbSeederExtensions
{
    public static async Task SeedDatabasesAsync(this WebApplication app, IConfiguration config)
    {
        using var scope = app.Services.CreateScope();

        // 1️⃣ Ensure DBs exist
        EnsureDatabasesCreated(scope);

        // 2️⃣ Seed Admin user & role
        await SeedAdminUserAsync(scope, config);

        // 3️⃣ Load prices if enabled
        await LoadPricesAsync(scope);

        // 4️⃣ Initialize accounts
        await InitializeAccountsAsync(scope);
    }

    // -------------------------
    // 1️⃣ Ensure Databases Exist
    // -------------------------
    private static void EnsureDatabasesCreated(IServiceScope scope)
    {
        var financeDb = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        financeDb.Database.EnsureCreated();

        var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        identityDb.Database.EnsureCreated();
    }

    // -------------------------
    // 2️⃣ Seed Admin User & Role
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
    // 3️⃣ Load Prices
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
    // 4️⃣ Initialize Accounts
    // -------------------------
    private static async Task InitializeAccountsAsync(IServiceScope scope)
    {
        var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();
        await accountService.InitializeAsync();
    }
}
