using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeronaShop.Migrations
{
    /// <inheritdoc />
    public partial class FF : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Region column only if it does not already exist
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.AspNetUsers','Region') IS NULL
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [Region] nvarchar(max) NULL;
END
");

            // Create Notifications table if missing
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables t WHERE t.name = 'Notifications' AND SCHEMA_NAME(t.schema_id) = 'dbo')
BEGIN
    CREATE TABLE [dbo].[Notifications] (
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [OrderNumber] nvarchar(max) NOT NULL,
        [RecipientEmail] nvarchar(256) NOT NULL,
        [Status] int NOT NULL,
        [AttemptedAt] datetimeoffset NULL,
        [ErrorMessage] nvarchar(max) NULL,
        [CreatedAt] datetimeoffset NOT NULL
    );
END
");

            // Create Posts table if missing
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables t WHERE t.name = 'Posts' AND SCHEMA_NAME(t.schema_id) = 'dbo')
BEGIN
    CREATE TABLE [dbo].[Posts] (
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Title] nvarchar(max) NOT NULL,
        [Summary] nvarchar(max) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL
    );
END
");

            // Create ProductImages table if missing
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables t WHERE t.name = 'ProductImages' AND SCHEMA_NAME(t.schema_id) = 'dbo')
BEGIN
    CREATE TABLE [dbo].[ProductImages] (
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ProductId] int NOT NULL,
        [Url] nvarchar(max) NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT FK_ProductImages_Products_ProductId FOREIGN KEY(ProductId) REFERENCES dbo.Products(Id)
    );
    IF NOT EXISTS (SELECT 1 FROM sys.indexes i JOIN sys.tables t ON i.object_id = t.object_id WHERE t.name = 'ProductImages' AND i.name = 'IX_ProductImages_ProductId')
    BEGIN
        CREATE INDEX IX_ProductImages_ProductId ON [dbo].[ProductImages]([ProductId]);
    END
END
");

            // Create Promotions table if missing
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables t WHERE t.name = 'Promotions' AND SCHEMA_NAME(t.schema_id) = 'dbo')
BEGIN
    CREATE TABLE [dbo].[Promotions] (
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Title] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [StartAt] datetimeoffset NOT NULL,
        [EndAt] datetimeoffset NOT NULL
    );
END
");

            // Create NotificationViews table if missing (ensure Notifications exists first)
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables t WHERE t.name = 'NotificationViews' AND SCHEMA_NAME(t.schema_id) = 'dbo')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.tables t2 WHERE t2.name = 'Notifications' AND SCHEMA_NAME(t2.schema_id) = 'dbo')
    BEGIN
        RAISERROR('Notifications table missing; NotificationViews cannot be created by this migration.', 16, 1);
    END
    ELSE
    BEGIN
        CREATE TABLE [dbo].[NotificationViews] (
            [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
            [NotificationId] int NOT NULL,
            [AdminId] int NOT NULL,
            [ViewedAt] datetimeoffset NOT NULL,
            CONSTRAINT FK_NotificationViews_Notifications_NotificationId FOREIGN KEY(NotificationId) REFERENCES dbo.Notifications(Id)
        );
        IF NOT EXISTS (SELECT 1 FROM sys.indexes i JOIN sys.tables t ON i.object_id = t.object_id WHERE t.name = 'NotificationViews' AND i.name = 'IX_NotificationViews_NotificationId_AdminId')
        BEGIN
            CREATE INDEX IX_NotificationViews_NotificationId_AdminId ON [dbo].[NotificationViews]([NotificationId], [AdminId]);
        END
    END
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationViews");

            migrationBuilder.DropTable(
                name: "Posts");

            migrationBuilder.DropTable(
                name: "ProductImages");

            migrationBuilder.DropTable(
                name: "Promotions");

            migrationBuilder.DropTable(
                name: "Notifications");

            // Do not drop Region here because it may have been created by an earlier migration; avoid destructive rollback.
        }
    }
}
