using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeronaShop.Migrations
{
    /// <inheritdoc />
    public partial class NotificationReads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create table only if it does not already exist (safe for environments where table was created manually)
            var sql = @"IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'NotificationReads' AND s.name = 'dbo')
BEGIN
    CREATE TABLE [dbo].[NotificationReads](
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [NotificationId] int NOT NULL,
        [UserId] int NULL,
        [RecipientEmail] nvarchar(max) NULL,
        [ReadAt] datetimeoffset NOT NULL
    );
    CREATE INDEX IX_NotificationReads_NotificationId_UserId ON [dbo].[NotificationReads]([NotificationId],[UserId]);
END";
            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop table only if it exists
            migrationBuilder.Sql("IF OBJECT_ID('dbo.NotificationReads') IS NOT NULL DROP TABLE [dbo].[NotificationReads];");
        }
    }
}
