using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TiketLaut.Data;
using System;
using System.Threading.Tasks;

namespace TiketLaut.Services
{
    public class DatabaseService
    {
        private static AppDbContext? _context;
        private static readonly object _lock = new object();

        public static AppDbContext GetContext()
        {
            if (_context == null)
            {
                lock (_lock)
                {
                    if (_context == null)
                    {
                        var configuration = new ConfigurationBuilder()
                            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                            .AddEnvironmentVariables() // Environment variables akan override appsettings
                            .Build();

                        // Prioritas: Environment Variables → appsettings.json
                        var connectionString = Environment.GetEnvironmentVariable("SUPABASE_CONNECTION") 
                            ?? configuration.GetConnectionString("SupabaseConnection")
                            ?? throw new InvalidOperationException("SUPABASE_CONNECTION not configured in environment variables or appsettings.json");

                        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                        optionsBuilder.UseNpgsql(connectionString);

                        _context = new AppDbContext(optionsBuilder.Options);
                    }
                }
            }

            return _context;
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

        /// <summary>
        /// Clear cached DbContext and force refresh from database.
        /// Call this after external changes or when you need fresh data.
        /// </summary>
        public static void RefreshContext()
        {
            lock (_lock)
            {
                if (_context != null)
                {
                    _context.Dispose();
                    _context = null;
                }
            }
        }

        /// <summary>
        /// Clear all tracked entities to prevent stale data issues.
        /// Call this after bulk SQL updates that bypass EF tracking.
        /// </summary>
        public static void ClearTrackedEntities()
        {
            var context = GetContext();
            context.ChangeTracker.Clear();
        }
    }
}