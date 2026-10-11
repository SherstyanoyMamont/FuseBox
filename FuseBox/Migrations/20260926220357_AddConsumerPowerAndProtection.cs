using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuseBox.Migrations
{
    /// <inheritdoc />
    public partial class AddConsumerPowerAndProtection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BreakerAmperage",
                table: "Consumer",
                type: "int",
                nullable: false,
                defaultValue: 16);

            migrationBuilder.AddColumn<double>(
                name: "PowerWatts",
                table: "Consumer",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RcdMilliAmps",
                table: "Consumer",
                type: "int",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.UpdateData(
                table: "Cables",
                keyColumn: "Сolour",
                keyValue: null,
                column: "Сolour",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Сolour",
                table: "Cables",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<decimal>(
                name: "Section",
                table: "Cables",
                type: "decimal(65,30)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AlterColumn<int>(
                name: "ConnectionCableId",
                table: "Cables",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BreakerAmperage",
                table: "Consumer");

            migrationBuilder.DropColumn(
                name: "PowerWatts",
                table: "Consumer");

            migrationBuilder.DropColumn(
                name: "RcdMilliAmps",
                table: "Consumer");

            migrationBuilder.AlterColumn<string>(
                name: "Сolour",
                table: "Cables",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<decimal>(
                name: "Section",
                table: "Cables",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ConnectionCableId",
                table: "Cables",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
