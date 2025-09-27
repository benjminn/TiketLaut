# 🗄️ Database Setup Guide - TiketLaut Supabase Integration

## 📋 Overview
This guide explains how to set up and use the Supabase PostgreSQL database integration for the TiketLaut Ferry Ticketing System.

## 🚀 Quick Start

### 1. Prerequisites
- Supabase account and project created
- .NET 9.0 SDK installed
- Entity Framework Tools installed globally

### 2. Database Configuration

#### For Supabase Production:
Update `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=YOUR_SUPABASE_HOST;Database=YOUR_DATABASE_NAME;Username=YOUR_USERNAME;Password=YOUR_PASSWORD;Port=5432;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

#### For Local Development:
Update `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=tiketlaut_dev;Username=postgres;Password=password;Port=5432;SSL Mode=Disable"
  }
}
```

### 3. Get Your Supabase Connection Details
1. Go to your Supabase project dashboard
2. Navigate to Settings → Database
3. Copy the connection string details:
   - Host: `db.YOUR_PROJECT_REF.supabase.co`
   - Database: Usually `postgres`
   - Username: Usually `postgres`
   - Password: Your database password

### 4. Run Database Migrations
```bash
# Apply migrations to create all tables
dotnet ef database update

# Or for first time setup, the app will auto-migrate in development
dotnet run
```

## 🏗️ Database Schema

The integration creates the following tables with proper relationships:

### Core Tables:
- **Admins** - System administrators with role-based access
- **Penggunas** - Regular users/customers
- **Pelabuhans** - Port/harbor information
- **Kapals** - Ferry/ship information
- **Jadwals** - Schedules with comprehensive pricing structure

### Ticketing Tables:
- **Tikets** - Main ticket records
- **Penumpangs** - Passenger information
- **RincianPenumpangs** - Junction table linking tickets to passengers
- **DetailKendaraans** - Vehicle type pricing per schedule
- **Pembayarans** - Payment records

### Communication:
- **Notifikasis** - Notification system for users

## 🔧 Key Features Implemented

### ✅ Entity Framework Configuration
- **Enum to String Conversion**: All enums (AdminRole, StatusTiket, JenisNotifikasi, JenisKendaraan) are stored as strings in PostgreSQL
- **Decimal Precision**: Currency fields use `decimal(18,2)` for accurate financial calculations
- **Relationships**: Proper foreign key constraints with cascade/restrict behaviors
- **Indexes**: Unique constraints on emails, usernames, NIK, etc.

### ✅ Database Features
- **Auto-Migration**: Development environment automatically applies migrations
- **Connection Retry**: Built-in retry logic for connection failures
- **Detailed Logging**: Comprehensive database operation logging
- **Error Handling**: Graceful handling of connection issues

### ✅ PostgreSQL Optimizations
- **SSL Support**: Ready for Supabase's SSL requirements
- **Connection Pooling**: Efficient connection management
- **Enum Mapping**: Proper enum to string conversions for PostgreSQL

## 🛠️ Common Commands

```bash
# Create a new migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Rollback to specific migration
dotnet ef database update PreviousMigrationName

# Remove last migration (if not applied)
dotnet ef migrations remove

# Generate SQL script for migrations
dotnet ef migrations script
```

## 🔍 Troubleshooting

### Connection Issues:
1. Verify Supabase project is active
2. Check connection string format
3. Ensure firewall/network access to Supabase
4. Verify SSL settings match your environment

### Migration Issues:
```bash
# Reset database (CAUTION: Destroys data)
dotnet ef database drop --force
dotnet ef database update
```

### Logging:
Check application logs for detailed error messages. The integration provides comprehensive logging for all database operations.

## 📚 Next Steps

1. **Data Seeding**: Add initial data for ports, ferries, and admin users
2. **Connection Pooling**: Configure advanced connection pool settings for production
3. **Backup Strategy**: Implement regular database backups
4. **Performance Monitoring**: Set up monitoring for database performance
5. **Security**: Review and implement database security best practices

## 🔗 Related Files

- `Data/TiketLautDbContext.cs` - Main Entity Framework context
- `Program.cs` - Database configuration and initialization
- `Migrations/` - Entity Framework migration files
- `Models/` - Entity classes with navigation properties

---
**🚢 TiketLaut - Ferry Ticketing System with Supabase PostgreSQL Integration**