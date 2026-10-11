using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuseBox.Migrations
{
    public partial class AddManagedComponentCatalog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManagedComponentCatalogItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CatalogTypeId = table.Column<string>(type: "varchar(96)", maxLength: 96, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TemplateCatalogTypeId = table.Column<string>(type: "varchar(96)", maxLength: 96, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsBuiltIn = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisplayName = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Manufacturer = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Mpn = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReferencePriceEur = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    DatasheetUrl = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImageUrl = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Model3dUrl = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModelScale = table.Column<double>(type: "double", nullable: false, defaultValue: 1.0),
                    ModelRotationX = table.Column<double>(type: "double", nullable: false),
                    ModelRotationY = table.Column<double>(type: "double", nullable: false),
                    ModelRotationZ = table.Column<double>(type: "double", nullable: false),
                    ModelOffsetX = table.Column<double>(type: "double", nullable: false),
                    ModelOffsetY = table.Column<double>(type: "double", nullable: false),
                    ModelOffsetZ = table.Column<double>(type: "double", nullable: false),
                    DefaultAmperage = table.Column<double>(type: "double", nullable: true),
                    DefaultRcdMilliAmps = table.Column<int>(type: "int", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagedComponentCatalogItems", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ManagedComponentCatalogItems_CatalogTypeId",
                table: "ManagedComponentCatalogItems",
                column: "CatalogTypeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManagedComponentCatalogItems_Category",
                table: "ManagedComponentCatalogItems",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ManagedComponentCatalogItems_IsDeleted_IsActive",
                table: "ManagedComponentCatalogItems",
                columns: new[] { "IsDeleted", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ManagedComponentCatalogItems_SortOrder",
                table: "ManagedComponentCatalogItems",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ManagedComponentCatalogItems_TemplateCatalogTypeId",
                table: "ManagedComponentCatalogItems",
                column: "TemplateCatalogTypeId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ManagedComponentCatalogItems");
        }
    }
}
