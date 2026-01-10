using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeronaShop.Migrations
{
    /// <inheritdoc />
    public partial class carriesAndDeleveries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add column only if it doesn't exist (environment already ensured it may exist)
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.Deliveries', 'CarrierId') IS NULL BEGIN ALTER TABLE [dbo].[Deliveries] ADD [CarrierId] INT NULL; END");

            // Create Carriers table only if it doesn't exist (to coexist with bootstrap DDL)
            migrationBuilder.Sql(@"IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'Carriers' AND s.name = 'dbo')
BEGIN
    CREATE TABLE [dbo].[Carriers](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(200) NOT NULL,
        [Phone] NVARCHAR(100) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT DF_Carriers_IsActive DEFAULT(1)
    );
END");

            // Create index if missing
            migrationBuilder.Sql(@"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Deliveries_CarrierId' AND object_id = OBJECT_ID('dbo.Deliveries'))
CREATE INDEX IX_Deliveries_CarrierId ON dbo.Deliveries (CarrierId);");

            // Add FK if not exists
            migrationBuilder.Sql(@"IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Deliveries_Carriers_CarrierId' AND parent_object_id = OBJECT_ID('dbo.Deliveries'))
ALTER TABLE dbo.Deliveries WITH CHECK ADD CONSTRAINT FK_Deliveries_Carriers_CarrierId FOREIGN KEY (CarrierId) REFERENCES dbo.Carriers(Id);");

            // Data migration: map existing Deliveries.Carrier (string) values to Carriers and set CarrierId
            // 1) Insert distinct carrier names from Deliveries into Carriers if not already present
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Deliveries') AND name = 'Carrier')
BEGIN
    ;WITH DistinctCarriers AS (
        SELECT DISTINCT RTRIM(LTRIM(Carrier)) AS Name
        FROM dbo.Deliveries
        WHERE Carrier IS NOT NULL AND LEN(RTRIM(LTRIM(Carrier))) > 0
    )
    INSERT INTO dbo.Carriers (Name, Phone, IsActive)
    SELECT dc.Name, NULL, 1
    FROM DistinctCarriers dc
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Carriers c WHERE c.Name = dc.Name);

    -- 2) Update Deliveries.CarrierId by matching on name
    UPDATE d SET d.CarrierId = c.Id
    FROM dbo.Deliveries d
    JOIN dbo.Carriers c ON RTRIM(LTRIM(d.Carrier)) = c.Name
    WHERE d.CarrierId IS NULL AND d.Carrier IS NOT NULL AND LEN(RTRIM(LTRIM(d.Carrier))) > 0;

    -- 3) Drop old string column after backfill
    ALTER TABLE dbo.Deliveries DROP COLUMN Carrier;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deliveries_Carriers_CarrierId",
                table: "Deliveries");

            migrationBuilder.DropTable(
                name: "Carriers");

            migrationBuilder.DropIndex(
                name: "IX_Deliveries_CarrierId",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "CarrierId",
                table: "Deliveries");

            migrationBuilder.AddColumn<string>(
                name: "Carrier",
                table: "Deliveries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
