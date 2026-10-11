using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuseBox.Migrations
{
    /// <inheritdoc />
    public partial class AddConsumerContactorLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ContactorId",
                table: "Consumer",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Consumer_ContactorId",
                table: "Consumer",
                column: "ContactorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Consumer_Component_ContactorId",
                table: "Consumer",
                column: "ContactorId",
                principalTable: "Component",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Consumer_Component_ContactorId",
                table: "Consumer");

            migrationBuilder.DropIndex(
                name: "IX_Consumer_ContactorId",
                table: "Consumer");

            migrationBuilder.DropColumn(
                name: "ContactorId",
                table: "Consumer");
        }
    }
}
