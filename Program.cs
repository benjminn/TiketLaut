using Microsoft.OpenApi.Models;
using TiketLaut;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "TiketLaut API", 
        Description = "TiketLaut API for Ferry Ticketing System"
    });
});

var app = builder.Build();

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

// Startup message
Console.WriteLine("🚢 TiketLaut API Starting...");
Console.WriteLine("📖 Swagger UI available at: https://localhost:5001 (or http://localhost:5000)");
Console.WriteLine("🛠️  API Documentation ready!");

app.Run();