using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuseBox.Migrations
{
    public partial class AddBillingFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CancelAtPeriodEnd",
                table: "UserSubscriptions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "UserSubscriptions",
                type: "varchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Internal")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ProviderCustomerId",
                table: "UserSubscriptions",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ProviderSubscriptionId",
                table: "UserSubscriptions",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_ProviderCustomerId",
                table: "UserSubscriptions",
                column: "ProviderCustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_ProviderSubscriptionId",
                table: "UserSubscriptions",
                column: "ProviderSubscriptionId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_ProviderCustomerId",
                table: "UserSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_ProviderSubscriptionId",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "CancelAtPeriodEnd",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "ProviderCustomerId",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "ProviderSubscriptionId",
                table: "UserSubscriptions");
        }
    }
}
