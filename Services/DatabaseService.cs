using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TiketLaut.Data;
using System;
using System.Threading.Tasks;

namespace TiketLaut.Services
{
    public class DatabaseService
    {
        private static string? _connectionString;
        private static readonly object _lock = new object();

        private static string GetConnectionString()
        {
            if (_connectionString == null)
            {
                lock (_lock)
                {
                    if (_connectionString == null)
                    {
                        var configuration = new ConfigurationBuilder()
                            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                            .AddEnvironmentVariables() // Environment variables akan override appsettings
                            .Build();

                        // Prioritas: Environment Variables → appsettings.json
                        _connectionString = Environment.GetEnvironmentVariable("SUPABASE_CONNECTION") 
                            ?? configuration.GetConnectionString("SupabaseConnection")
                            ?? throw new InvalidOperationException("SUPABASE_CONNECTION not configured in environment variables or appsettings.json");
                    }
                }
            }

            return _connectionString;
        }

        public static AppDbContext GetContext()
        {
            // This allows using var context pattern safely
            var connectionString = GetConnectionString();
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(connectionString);
            return new AppDbContext(optionsBuilder.Options);
        }

        public static async Task<bool> TestConnectionAsync()
        {
            try
            {
                var context = GetContext();
                return await context.Database.CanConnectAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database connection error: {ex.Message}");
                return false;
            }
        }
        public static void RefreshContext()
        {
            // No-op: Context instances are created per-call, so no cached context to clear
            // This method is kept for backward compatibility
        }
        public static void ClearTrackedEntities()
        {
            var context = GetContext();
            context.ChangeTracker.Clear();
        }
    }
}