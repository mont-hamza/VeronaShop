using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeronaShop.Migrations
{
    /// <inheritdoc />
    public partial class ProductSizes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SizesCsv",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SizeId",
                table: "CartItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SizeName",
                table: "CartItems",
                type: "nvarchar(max)",
                nullable: true);

            // Add carrier hours only if they don't already exist (some environments add them at runtime)
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.Carriers','DailyEnd') IS NULL ALTER TABLE dbo.Carriers ADD [DailyEnd] time NULL;");
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.Carriers','DailyStart') IS NULL ALTER TABLE dbo.Carriers ADD [DailyStart] time NULL;");

            // Ensure existing Products.ImageUrl column (from older migrations) allows NULL so inserts without ImageUrl succeed.
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.Products','ImageUrl') IS NOT NULL ALTER TABLE dbo.Products ALTER COLUMN ImageUrl nvarchar(max) NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SizesCsv",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SizeId",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "SizeName",
                table: "CartItems");

            // Drop carrier hours only if they exist
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.Carriers','DailyEnd') IS NOT NULL ALTER TABLE dbo.Carriers DROP COLUMN [DailyEnd];");
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.Carriers','DailyStart') IS NOT NULL ALTER TABLE dbo.Carriers DROP COLUMN [DailyStart];");

            // When rolling back, ensure Products.ImageUrl is non-nullable again (set empty value for existing NULLs first)
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Products','ImageUrl') IS NOT NULL
BEGIN
    UPDATE dbo.Products SET ImageUrl = '' WHERE ImageUrl IS NULL;
    ALTER TABLE dbo.Products ALTER COLUMN ImageUrl nvarchar(max) NOT NULL;
END
");
        }
    }
}
