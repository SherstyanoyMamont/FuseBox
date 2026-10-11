using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuseBox.Migrations
{
    public partial class AddConsumerPowerMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CatalogTypeId",
                table: "Consumer",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PowerSource",
                table: "Consumer",
                type: "varchar(16)",
                maxLength: 16,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // Existing projects with an explicitly stored watt value came
            // from the pre-catalog manual-entry UI. Preserve that value and
            // mark only its source. Rows with unknown PowerWatts remain null.
            migrationBuilder.Sql(
                """
                UPDATE `Consumer`
                SET `PowerSource` = 'manual'
                WHERE `PowerWatts` IS NOT NULL
                  AND (`PowerSource` IS NULL OR `PowerSource` = '');
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CatalogTypeId",
                table: "Consumer");

            migrationBuilder.DropColumn(
                name: "PowerSource",
                table: "Consumer");
        }
    }
}
