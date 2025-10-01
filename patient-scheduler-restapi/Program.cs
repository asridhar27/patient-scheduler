using Microsoft.EntityFrameworkCore;
using PatientScheduler.Data;
using PatientScheduler.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "Patient Scheduler API", 
        Version = "v1",
        Description = "A comprehensive patient scheduling system with transactional operations"
    });
});

// Add Entity Framework for In-Memory SQLite
// A single connection is maintained to keep the in-memory database alive for the duration of the app.
var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
connection.Open(); // The connection MUST be opened before it's used.

builder.Services.AddDbContext<PatientSchedulerContext>(options =>
    options.UseSqlite(connection));

// Add custom services
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IBulkOperationService, BulkOperationService>();

// Add background service
builder.Services.AddHostedService<BackgroundJobService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add logging
builder.Services.AddLogging();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PatientSchedulerContext>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

var app = builder.Build();

// Configure the HTTP request pipeline.
// Enable Swagger in both Development and Production for API documentation
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Patient Scheduler API v1");
    c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
});

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// Map health check endpoint
app.MapHealthChecks("/health");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PatientSchedulerContext>();
    context.Database.EnsureCreated();
}

app.Run();

// Make Program class accessible for testing
public partial class Program { }