using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeronaShop.Migrations
{
    /// <inheritdoc />
    public partial class ImageFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Make Products.ImageUrl nullable if the column exists to avoid insert failures
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.Products','ImageUrl') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Products ALTER COLUMN ImageUrl nvarchar(max) NULL;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore NOT NULL on Products.ImageUrl when rolling back (set empty value for NULLs first)
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.Products','ImageUrl') IS NOT NULL
BEGIN
    UPDATE dbo.Products SET ImageUrl = '' WHERE ImageUrl IS NULL;
    ALTER TABLE dbo.Products ALTER COLUMN ImageUrl nvarchar(max) NOT NULL;
END");
        }
    }
}
