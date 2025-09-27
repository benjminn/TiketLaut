using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using TiketLaut;
using TiketLaut.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure Entity Framework with Supabase PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

builder.Services.AddDbContext<TiketLautDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    });
    
    // Enable detailed errors in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "TiketLaut API", 
        Description = "TiketLaut API for Ferry Ticketing System with Supabase PostgreSQL Database",
        Version = "v1.0"
    });
});

var app = builder.Build();

// Database initialization and migration
await InitializeDatabaseAsync(app);

// Configure the HTTP request pipeline.
// Aktifkan Swagger untuk semua environment (tidak hanya Development)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TiketLaut API v1");
    c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    c.DocumentTitle = "TiketLaut API Documentation";
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Enhanced startup message
Console.WriteLine("🚢 TiketLaut API Starting...");
Console.WriteLine("🗄️  Database: PostgreSQL (Supabase)");
Console.WriteLine("📖 Swagger UI available at: https://localhost:5001 (or http://localhost:5000)");
Console.WriteLine("🛠️  API Documentation ready!");
Console.WriteLine("✅ Database connection established and tables created/updated!");

app.Run();

// Database initialization method
static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<TiketLautDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("🔄 Initializing database...");
        
        // Check database connection
        var canConnect = await context.Database.CanConnectAsync();
        if (!canConnect)
        {
            logger.LogError("❌ Cannot connect to database. Please check your connection string.");
            throw new InvalidOperationException("Database connection failed");
        }

        logger.LogInformation("✅ Database connection successful");

        // In development, apply migrations and seed data
        if (app.Environment.IsDevelopment())
        {
            logger.LogInformation("🔄 Applying database migrations...");
            await context.Database.MigrateAsync();
            logger.LogInformation("✅ Database migrations applied successfully");

            // Seed initial data
            logger.LogInformation("🌱 Seeding initial data...");
            await DbSeeder.SeedAsync(context, logger);
            logger.LogInformation("✅ Database seeding completed");
        }
        else
        {
            // In production, just ensure database exists (migrations should be run separately)
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("✅ Database schema verified");
        }

        logger.LogInformation("🎯 Database initialization completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Database initialization failed: {Message}", ex.Message);
        
        if (app.Environment.IsDevelopment())
        {
            logger.LogWarning("⚠️  Development mode: Application will continue but database operations may fail.");
            logger.LogInformation("💡 Make sure PostgreSQL is running and connection string is correct.");
        }
        else
        {
            throw; // Re-throw in production
        }
    }
}