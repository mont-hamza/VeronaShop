using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeronaShop.Migrations
{
    public partial class MigrateProductImageData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create ProductImages table if it does not exist (safe guard)
            migrationBuilder.Sql(@"IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'ProductImages' AND s.name = 'dbo')
BEGIN
    CREATE TABLE [dbo].[ProductImages] (
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ProductId] int NOT NULL,
        [Url] nvarchar(MAX) NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL
    );
    CREATE INDEX IX_ProductImages_ProductId ON [dbo].[ProductImages]([ProductId]);
END");

            // Copy non-empty ImageUrl values into ProductImages as primary image (SortOrder = 0)
            migrationBuilder.Sql(@"INSERT INTO [dbo].[ProductImages] (ProductId, Url, SortOrder, CreatedAt)
SELECT Id, ImageUrl, 0, SYSUTCDATETIME()
FROM [dbo].[Products]
WHERE ImageUrl IS NOT NULL AND LTRIM(RTRIM(ImageUrl)) <> ''
  AND NOT EXISTS (
    SELECT 1 FROM [dbo].[ProductImages] pi WHERE pi.ProductId = [dbo].[Products].Id
  );");

            // Drop the legacy ImageUrl column if it exists
            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.tables t ON c.object_id = t.object_id WHERE t.name = 'Products' AND c.name = 'ImageUrl')
BEGIN
    ALTER TABLE [dbo].[Products] DROP COLUMN [ImageUrl];
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add ImageUrl back (nullable) and populate from ProductImages first image if present
            migrationBuilder.Sql(@"IF NOT EXISTS (SELECT 1 FROM sys.columns c JOIN sys.tables t ON c.object_id = t.object_id WHERE t.name = 'Products' AND c.name = 'ImageUrl')
BEGIN
    ALTER TABLE [dbo].[Products] ADD [ImageUrl] nvarchar(max) NULL;
    UPDATE p
    SET p.ImageUrl = pi.Url
    FROM [dbo].[Products] p
    CROSS APPLY (
        SELECT TOP(1) Url FROM [dbo].[ProductImages] WHERE ProductId = p.Id ORDER BY SortOrder
    ) pi(Url)
    WHERE p.ImageUrl IS NULL;
END");

            // Note: we intentionally do not drop ProductImages in Down to avoid data loss.
        }
    }
}
