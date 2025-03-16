using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuseBox.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectFuseBoxStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Username",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Users",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Ports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    connectionsCount = table.Column<int>(type: "int", nullable: false),
                    portOut = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PortIn = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ports", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TotalPower = table.Column<double>(type: "double", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FloorGroupings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IndividualFloorGrouping = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SeparateUZOPerFloor = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FloorGroupings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FloorGroupings_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Floors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Floors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Floors_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FuseBoxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MainBreaker = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Main3PN = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SurgeProtection = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LoadSwitch2P = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ModularContactor = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RailMeter = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FireUZO = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    VoltageRelay = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ThreePRelay = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RailSocket = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    NDiscLine = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LoadSwitch = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CrossModule = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DINLines = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuseBoxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuseBoxes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GlobalGroupings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Sockets = table.Column<int>(type: "int", nullable: false),
                    Lighting = table.Column<int>(type: "int", nullable: false),
                    Conditioners = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalGroupings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GlobalGroupings_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InitialSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PhasesCount = table.Column<int>(type: "int", nullable: false),
                    MainAmperage = table.Column<int>(type: "int", nullable: false),
                    ShieldWidth = table.Column<int>(type: "int", nullable: false),
                    VoltageStandard = table.Column<int>(type: "int", nullable: false),
                    PowerCoefficient = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InitialSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InitialSettings_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FloorId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rooms_Floors_FloorId",
                        column: x => x.FloorId,
                        principalTable: "Floors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Connections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FuseBoxId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Connections_FuseBoxes_FuseBoxId",
                        column: x => x.FuseBoxId,
                        principalTable: "FuseBoxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FuseBoxComponentGroup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    FuseBoxId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuseBoxComponentGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuseBoxComponentGroup_FuseBoxes_FuseBoxId",
                        column: x => x.FuseBoxId,
                        principalTable: "FuseBoxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FuseBoxComponentGroup_FuseBoxes_Id",
                        column: x => x.Id,
                        principalTable: "FuseBoxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Cables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Length = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Section = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Сolour = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConnectionId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cables_Connections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "Connections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IndexStart = table.Column<int>(type: "int", nullable: false),
                    IndexFinish = table.Column<int>(type: "int", nullable: false),
                    ConnectionId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Positions_Connections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "Connections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BaseElectrical",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Amper = table.Column<double>(type: "double", nullable: false),
                    Discriminator = table.Column<string>(type: "varchar(21)", maxLength: 21, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Price = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    Slots = table.Column<int>(type: "int", nullable: true),
                    ContactorId = table.Column<int>(type: "int", nullable: true),
                    FuseBoxUnitId = table.Column<int>(type: "int", nullable: true),
                    FuseBoxUnitId1 = table.Column<int>(type: "int", nullable: true),
                    FuseBoxUnitId2 = table.Column<int>(type: "int", nullable: true),
                    FuseId = table.Column<int>(type: "int", nullable: true),
                    RCDId = table.Column<int>(type: "int", nullable: true),
                    RoomId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Capacity = table.Column<int>(type: "int", nullable: true),
                    TotalLoad = table.Column<double>(type: "double", nullable: true),
                    RCDFire_Capacity = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseElectrical", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaseElectrical_BaseElectrical_ContactorId",
                        column: x => x.ContactorId,
                        principalTable: "BaseElectrical",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BaseElectrical_BaseElectrical_FuseId",
                        column: x => x.FuseId,
                        principalTable: "BaseElectrical",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BaseElectrical_BaseElectrical_RCDId",
                        column: x => x.RCDId,
                        principalTable: "BaseElectrical",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BaseElectrical_FuseBoxComponentGroup_Id",
                        column: x => x.Id,
                        principalTable: "FuseBoxComponentGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BaseElectrical_FuseBoxes_FuseBoxUnitId",
                        column: x => x.FuseBoxUnitId,
                        principalTable: "FuseBoxes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BaseElectrical_FuseBoxes_FuseBoxUnitId1",
                        column: x => x.FuseBoxUnitId1,
                        principalTable: "FuseBoxes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BaseElectrical_FuseBoxes_FuseBoxUnitId2",
                        column: x => x.FuseBoxUnitId2,
                        principalTable: "FuseBoxes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BaseElectrical_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_BaseElectrical_ContactorId",
                table: "BaseElectrical",
                column: "ContactorId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseElectrical_FuseBoxUnitId",
                table: "BaseElectrical",
                column: "FuseBoxUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseElectrical_FuseBoxUnitId1",
                table: "BaseElectrical",
                column: "FuseBoxUnitId1");

            migrationBuilder.CreateIndex(
                name: "IX_BaseElectrical_FuseBoxUnitId2",
                table: "BaseElectrical",
                column: "FuseBoxUnitId2");

            migrationBuilder.CreateIndex(
                name: "IX_BaseElectrical_FuseId",
                table: "BaseElectrical",
                column: "FuseId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseElectrical_RCDId",
                table: "BaseElectrical",
                column: "RCDId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseElectrical_RoomId",
                table: "BaseElectrical",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Cables_ConnectionId",
                table: "Cables",
                column: "ConnectionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Connections_FuseBoxId",
                table: "Connections",
                column: "FuseBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_FloorGroupings_ProjectId",
                table: "FloorGroupings",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Floors_ProjectId",
                table: "Floors",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_FuseBoxComponentGroup_FuseBoxId",
                table: "FuseBoxComponentGroup",
                column: "FuseBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_FuseBoxes_ProjectId",
                table: "FuseBoxes",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GlobalGroupings_ProjectId",
                table: "GlobalGroupings",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InitialSettings_ProjectId",
                table: "InitialSettings",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Positions_ConnectionId",
                table: "Positions",
                column: "ConnectionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_UserId",
                table: "Projects",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_FloorId",
                table: "Rooms",
                column: "FloorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BaseElectrical");

            migrationBuilder.DropTable(
                name: "Cables");

            migrationBuilder.DropTable(
                name: "FloorGroupings");

            migrationBuilder.DropTable(
                name: "GlobalGroupings");

            migrationBuilder.DropTable(
                name: "InitialSettings");

            migrationBuilder.DropTable(
                name: "Ports");

            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.DropTable(
                name: "FuseBoxComponentGroup");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "Connections");

            migrationBuilder.DropTable(
                name: "Floors");

            migrationBuilder.DropTable(
                name: "FuseBoxes");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Users",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
