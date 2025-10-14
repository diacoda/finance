var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddAppConfiguration<Program>(builder.Environment);
bool isTesting = builder.Environment.EnvironmentName == "Testing";
builder.Services.AddApplicationServices(builder.Configuration, isTesting);
var app = builder.Build();
app.UseApplicationPipeline();
await app.Services.SeedDatabasesAsync(builder.Configuration);
app.Run();

// For integration testing
public partial class Program { }
