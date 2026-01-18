using Microsoft.EntityFrameworkCore;

namespace VeronaShop.Data
{
    public static class DbInitializer
    {
        public static void EnsureSchema(ApplicationDbContext db)
        {
            // Ensure Notifications table exists for runtime-written notification records (best-effort).
            try
            {
                var ensureSql = @"IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'Notifications' AND s.name = 'dbo')
BEGIN
    CREATE TABLE [dbo].[Notifications](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [OrderNumber] NVARCHAR(max) NOT NULL,
        [RecipientEmail] NVARCHAR(256) NOT NULL,
        [Status] INT NOT NULL,
        [AttemptedAt] DATETIMEOFFSET NULL,
        [ErrorMessage] NVARCHAR(max) NULL,
        [CreatedAt] DATETIMEOFFSET NOT NULL
    );
END";

                try { db.Database.ExecuteSqlRaw(ensureSql); } catch { }
                // Notification views are tracked in NotificationViews table per admin; migrations should create that table.

                // Ensure Carriers table exists (best-effort) for delivery assignments
                var ensureCarriers = @"IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'Carriers' AND s.name = 'dbo')
BEGIN
    CREATE TABLE [dbo].[Carriers](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(200) NOT NULL,
        [Phone] NVARCHAR(100) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT DF_Carriers_IsActive DEFAULT(1)
    );
END";
                try { db.Database.ExecuteSqlRaw(ensureCarriers); } catch { }

                // Ensure Deliveries has CarrierId column for relationship to Carriers (keep old columns intact)
                var ensureCarrierIdOnDeliveries = @"IF COL_LENGTH('dbo.Deliveries', 'CarrierId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Deliveries] ADD [CarrierId] INT NULL;
END";
                try { db.Database.ExecuteSqlRaw(ensureCarrierIdOnDeliveries); } catch { }

                // Add working hours columns to Carriers if missing
                var ensureCarrierHours = @"IF COL_LENGTH('dbo.Carriers','DailyStart') IS NULL ALTER TABLE dbo.Carriers ADD DailyStart time NULL;
IF COL_LENGTH('dbo.Carriers','DailyEnd') IS NULL ALTER TABLE dbo.Carriers ADD DailyEnd time NULL;";
                try { db.Database.ExecuteSqlRaw(ensureCarrierHours); } catch { }
                // Ensure NotificationReads table exists for per-customer read receipts
                var ensureNotificationReads = @"IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'NotificationReads' AND s.name = 'dbo')
BEGIN
    CREATE TABLE [dbo].[NotificationReads](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [NotificationId] INT NOT NULL,
        [UserId] INT NULL,
        [RecipientEmail] NVARCHAR(256) NULL,
        [ReadAt] DATETIMEOFFSET NOT NULL
    );
END";
                try { db.Database.ExecuteSqlRaw(ensureNotificationReads); } catch { }

                // Ensure Orders has IsPaid and PaidAt columns (safe-add if missing)
                var ensureOrderPaidCols = @"IF COL_LENGTH('dbo.Orders','IsPaid') IS NULL
BEGIN
    ALTER TABLE dbo.Orders ADD IsPaid bit NOT NULL CONSTRAINT DF_Orders_IsPaid DEFAULT(0);
END
IF COL_LENGTH('dbo.Orders','PaidAt') IS NULL
BEGIN
    ALTER TABLE dbo.Orders ADD PaidAt datetimeoffset NULL;
END";
                try { db.Database.ExecuteSqlRaw(ensureOrderPaidCols); } catch { }
            }
            catch { }
        }
    }
}
