using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuseBox.Migrations
{
    public partial class AddSchemaEditorState : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SchemaEditorStates",
                columns: table => new
                {
                    ProjectId = table.Column<int>(
                        type: "int",
                        nullable: false),
                    Mode = table.Column<string>(
                        type: "varchar(16)",
                        maxLength: 16,
                        nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Revision = table.Column<int>(
                        type: "int",
                        nullable: false),
                    DocumentJson = table.Column<string>(
                        type: "longtext",
                        nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAtUtc = table.Column<DateTime>(
                        type: "datetime(6)",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_SchemaEditorStates",
                        x => x.ProjectId);
                    table.ForeignKey(
                        name: "FK_SchemaEditorStates_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SchemaEditorStates");
        }
    }
}
