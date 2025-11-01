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
                            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                            .Build();

                        var connectionString = configuration.GetConnectionString("SupabaseConnection");

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
    }
}