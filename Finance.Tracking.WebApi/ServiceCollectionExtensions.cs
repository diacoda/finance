using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Data.Common;
using System.Text;

namespace Finance.Tracking.WebApi;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config, bool isTesting = false)
    {
        services.AddDatabases(config, isTesting);
        services.AddIdentityServices();
        services.AddJwtAuthentication(config);
        services.AddAppServices(config);
        services.AddHttpClientServices();
        services.AddControllerServices();
        services.AddSwaggerServices();
        services.AddCorsPolicy();

        return services;
    }

    // -----------------------------
    // Database registration
    // -----------------------------
    private static IServiceCollection AddDatabases(this IServiceCollection services, IConfiguration config, bool isTesting)
    {
        if (false)
        {
            // Finance DB (separate in-memory connection)
            services.AddSingleton<DbConnection>(sp =>
            {
                var financeConn = new SqliteConnection("DataSource=:memory:");
                financeConn.Open();
                return financeConn;
            });
            services.AddDbContext<FinanceDbContext>((sp, opt) =>
                opt.UseSqlite(sp.GetRequiredService<DbConnection>()));

            // Identity DB (separate persistent in-memory connection)
            services.AddSingleton<DbConnection>(sp =>
            {
                var identityConn = new SqliteConnection("DataSource=:memory:");
                identityConn.Open(); // keep alive for whole app lifetime
                return identityConn;
            });
            services.AddDbContext<IdentityDbContext>((sp, opt) =>
                opt.UseSqlite(sp.GetRequiredService<DbConnection>()));
        }
        else
        {
            var financeDbPath = PathHelper.GetPath(config.GetValue<string>("Database:FinancePath") ?? "financeqa.db");
            services.AddDbContext<FinanceDbContext>(opt => opt.UseSqlite($"Data Source={financeDbPath}"));

            var identityDbPath = PathHelper.GetPath(config.GetValue<string>("Database:IdentityPath") ?? "identityqa.db");
            services.AddDbContext<IdentityDbContext>(opt => opt.UseSqlite($"Data Source={identityDbPath}"));
        }
        return services;
    }

    // -----------------------------
    // Identity registration
    // -----------------------------
    private static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services.AddIdentity<IdentityUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
        })
        .AddEntityFrameworkStores<IdentityDbContext>()
        .AddDefaultTokenProviders();

        services.AddAuthorization();
        return services;
    }

    // -----------------------------
    // JWT Authentication
    // -----------------------------
    private static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var jwtSettings = config.GetSection("Jwt");

        services.Configure<JwtOptions>(jwtSettings);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
            };
        });

        return services;
    }

    // -----------------------------
    // Application service registration
    // -----------------------------
    private static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<AccountOptions>(config);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IYahooService, YahooService>();
        services.AddScoped<IPricingService, PricingService>();
        services.AddScoped<IHistoryService, HistoryService>();
        services.AddScoped<IValuationService, ValuationService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IAccountSummaryRepository, AccountSummaryRepository>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<ITransactionService, TransactionService>();
        return services;
    }

    // -----------------------------
    // HttpClient
    // -----------------------------
    private static IServiceCollection AddHttpClientServices(this IServiceCollection services)
    {
        services.AddHttpClient();
        return services;
    }

    // -----------------------------
    // Controllers + JSON options
    // -----------------------------
    private static IServiceCollection AddControllerServices(this IServiceCollection services)
    {
        services.AddControllers(options => options.Filters.Add(new HttpResponseExceptionFilter()))
                .AddJsonOptions(opt =>
                    opt.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
        return services;
    }

    // -----------------------------
    // Swagger
    // -----------------------------
    private static IServiceCollection AddSwaggerServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
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

            c.AddSecurityDefinition("Bearer", securityScheme);
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { securityScheme, Array.Empty<string>() }
            });
        });

        return services;
    }

    // -----------------------------
    // CORS
    // -----------------------------
    private static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("http://localhost:5173", "https://localhost:5001")
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });
        return services;
    }
}
