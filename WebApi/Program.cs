using Finance.Tracking;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Configuration.AddAppConfiguration<Program>();

// Services / DI
builder.Services.AddApplicationServices(builder.Configuration);

// Build app
var app = builder.Build();

// Middleware / pipeline
app.UseApplicationPipeline();

// Seed DBs & initialize
await app.SeedDatabasesAsync(builder.Configuration);

app.Run();

// For integration testing
public partial class Program { }
