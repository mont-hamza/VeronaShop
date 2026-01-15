using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeronaShop.Migrations
{
    /// <inheritdoc />
    public partial class OrderPaidFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add columns only if they do not already exist (safe for DBs modified outside EF migrations)
            var sql = @"IF COL_LENGTH('dbo.Orders','IsPaid') IS NULL
BEGIN
    ALTER TABLE dbo.Orders ADD IsPaid bit NOT NULL CONSTRAINT DF_Orders_IsPaid DEFAULT(0);
END
IF COL_LENGTH('dbo.Orders','PaidAt') IS NULL
BEGIN
    ALTER TABLE dbo.Orders ADD PaidAt datetimeoffset NULL;
END";
            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop columns only if they exist
            migrationBuilder.Sql("IF COL_LENGTH('dbo.Orders','IsPaid') IS NOT NULL BEGIN ALTER TABLE dbo.Orders DROP COLUMN IsPaid; END");
            migrationBuilder.Sql("IF COL_LENGTH('dbo.Orders','PaidAt') IS NOT NULL BEGIN ALTER TABLE dbo.Orders DROP COLUMN PaidAt; END");
        }
    }
}
