using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuseBox.Migrations
{
    /// <inheritdoc />
    public partial class MakePortCableOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ports_Cables_CableId",
                table: "Ports");

            migrationBuilder.AlterColumn<int>(
                name: "CableId",
                table: "Ports",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Ports_Cables_CableId",
                table: "Ports",
                column: "CableId",
                principalTable: "Cables",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ports_Cables_CableId",
                table: "Ports");

            migrationBuilder.AlterColumn<int>(
                name: "CableId",
                table: "Ports",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Ports_Cables_CableId",
                table: "Ports",
                column: "CableId",
                principalTable: "Cables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
