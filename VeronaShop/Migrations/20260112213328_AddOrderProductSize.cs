using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeronaShop.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderProductSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SizeName",
                table: "OrderProducts",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SizeName",
                table: "OrderProducts");
        }
    }
}
