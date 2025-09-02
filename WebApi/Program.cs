// Build the WebApplication
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Finance.Tracking;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;

bool loadPrices = false;
List<Price>? prices = null;
if (loadPrices)
{
    prices = CsvPriceLoader.LoadPricesFromCsv("daily.csv");
}

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                     .AddUserSecrets<Program>(optional: true, reloadOnChange: true);

// Database (SQLite)
string dbPath = PathHelper.GetPath(
    builder.Configuration.GetValue<string>("Database:RelativePath") ?? "finance.db");
builder.Services.AddDbContext<FinanceDbContext>(opt =>
    opt.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "Dan",
            ValidAudience = "FinanceApp",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("super-secret-key-12345-longer-wow-98765"))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers(options =>
{
    options.Filters.Add(new HttpResponseExceptionFilter());
});

// HTTP client
builder.Services.AddHttpClient();

// App services
builder.Services.Configure<AccountOptions>(builder.Configuration);
builder.Services.AddScoped<IYahooService, YahooService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IHistoryService, HistoryService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();

// Controllers + Swagger
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Finance API",
            Version = "v1",
            Description = "Portfolio & Account Tracking API"
        });

        var securityScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description = "Enter 'Bearer {token}'",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        };

        // Add JWT Bearer authentication to Swagger
        c.AddSecurityDefinition("Bearer", securityScheme);
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                securityScheme, new string[] { }
            }
        });
    });
// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173") // React dev server
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
var app = builder.Build();

// Ensure DB is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
    dbContext.Database.EnsureCreated();
    if (loadPrices && prices != null)
    {
        var pricingService = scope.ServiceProvider.GetRequiredService<IPricingService>();
        await pricingService.SavePricesAsync(prices);
    }
}

// Middleware
app.UseStaticFiles();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Finance API v1");
        c.RoutePrefix = "swagger"; // Swagger UI at /swagger

        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);

        // Inject JS to auto-apply token after login
        c.InjectJavascript("/swagger/custom-swagger.js");

    });
}
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors();
app.MapControllers();
app.Run();

// Program.cs
public partial class Program { }  // Add this at the end of the file

/*
// bind config (including secrets) into typed options
using IHost host = Host
        .CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((context, config) =>
            {
                // clear the default sources if you want full control
                // config.Sources.Clear();

                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                  .AddUserSecrets<Program>(optional: true, reloadOnChange: true);
            })
        .ConfigureServices((context, services) =>
            {
                //var connectionString = context.Configuration.GetConnectionString("DefaultConnection")
                //    ?? "Data Source=finance.db";
                string dbPath = PathHelper.GetPath(context.Configuration.GetValue<string>("Database:RelativePath") ?? "finance.db");

                services.AddDbContext<FinanceDbContext>(opt => opt.UseSqlite($"Data Source={dbPath}"));
                services.AddHttpClient();
                services.Configure<AccountOptions>(context.Configuration);
                services.AddSingleton<IYahooService>(sp =>
                    new YahooService(
                        sp.GetRequiredService<IHttpClientFactory>()));
                services.AddSingleton<IPricingService>(sp =>
                    new PricingService(
                        Enum.GetValues<Symbol>(),
                        sp.GetRequiredService<IHttpClientFactory>(),
                        sp.GetRequiredService<FinanceDbContext>(),
                        sp.GetRequiredService<IYahooService>()));
                services.AddSingleton<IAccountService, AccountService>();
                services.AddSingleton<IPortfolioService, PortfolioService>();
            })
            .Build();

// Resolve services
using var scope = host.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
dbContext.Database.EnsureCreated();

List<Price> prices = CsvPriceLoader.LoadPricesFromCsv("prices.csv");
var pricingService = host.Services.GetRequiredService<IPricingService>();
await pricingService.SavePrices(prices);

var accountService = host.Services.GetRequiredService<IAccountService>();
var portfolioService = host.Services.GetRequiredService<IPortfolioService>();

DateOnly asOf = DateOnly.FromDateTime(DateTime.Today);

// Calculate all account market values
await accountService.BuildSummariesByDateAsync(asOf);

// Display total portfolio value
Console.WriteLine($"Total Portfolio Value: {await portfolioService.GetTotalMarketValueAsync():C2}");

// Display total portfolio value by owner
Console.WriteLine("Total Portfolio Value by Owner:");
var totalsByOwner = await portfolioService.GetTotalMarketValueGroupedByOwnerAsync(asOf);
foreach (var kvp in totalsByOwner)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value:C2}");
}

// Display total portfolio value by owner and account filter
Console.WriteLine("Total Portfolio Value by Owner and Account Filter:");
var totalsByOwnerAndFilter = await portfolioService.GetMarketValueByOwnerAndAccountFilterAsync(asOf);
foreach (var kvp in totalsByOwnerAndFilter)
{
    Console.WriteLine($"{kvp.Key.Owner} / {kvp.Key.AccountFilter}: {kvp.Value:C2}");
}
// Keep console open
Console.WriteLine("Press any key to exit...");
Console.Read();
*/