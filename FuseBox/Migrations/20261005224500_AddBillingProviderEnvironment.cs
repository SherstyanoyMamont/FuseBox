using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuseBox.Migrations
{
    public partial class AddBillingProviderEnvironment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProviderEnvironment",
                table: "UserSubscriptions",
                type: "varchar(16)",
                maxLength: 16,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ProviderEnvironment",
                table: "BillingWebhookEvents",
                type: "varchar(16)",
                maxLength: 16,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // Every Paddle integration before Stage 14 was intentionally
            // configured against Paddle Sandbox. Preserve that provenance so
            // sandbox identifiers/events can never be mistaken for live state.
            migrationBuilder.Sql(
                """
                UPDATE `UserSubscriptions`
                SET `ProviderEnvironment` = 'sandbox'
                WHERE `Provider` = 'Paddle'
                   OR `ProviderCustomerId` IS NOT NULL
                   OR `ProviderSubscriptionId` IS NOT NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE `BillingWebhookEvents`
                SET `ProviderEnvironment` = 'sandbox'
                WHERE `ProviderEnvironment` IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ProviderEnvironment",
                table: "BillingWebhookEvents",
                type: "varchar(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(16)",
                oldMaxLength: 16,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.DropIndex(
                name: "IX_BillingWebhookEvents_ProviderEventId",
                table: "BillingWebhookEvents");

            migrationBuilder.CreateIndex(
                name: "IX_BillingWebhookEvents_ProviderEnvironment_ProviderEventId",
                table: "BillingWebhookEvents",
                columns: new[] { "ProviderEnvironment", "ProviderEventId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BillingWebhookEvents_ProviderEnvironment_ProviderEventId",
                table: "BillingWebhookEvents");

            migrationBuilder.CreateIndex(
                name: "IX_BillingWebhookEvents_ProviderEventId",
                table: "BillingWebhookEvents",
                column: "ProviderEventId",
                unique: true);

            migrationBuilder.DropColumn(
                name: "ProviderEnvironment",
                table: "BillingWebhookEvents");

            migrationBuilder.DropColumn(
                name: "ProviderEnvironment",
                table: "UserSubscriptions");
        }
    }
}
