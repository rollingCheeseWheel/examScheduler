using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace examScheduler.Migrations
{
    /// <inheritdoc />
    public partial class postgresqlcontainer_migration_963 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduleGenerator",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlacklistedDays = table.Column<DateOnly[]>(type: "date[]", maxLength: 20, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleGenerator", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleGenerator__Schedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "_Schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleGeneratorSlot",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    MaxParticipants = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    ScheduleGeneratorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleGeneratorSlot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleGeneratorSlot_ScheduleGenerator_ScheduleGeneratorId",
                        column: x => x.ScheduleGeneratorId,
                        principalTable: "ScheduleGenerator",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleGenerator_ScheduleId",
                table: "ScheduleGenerator",
                column: "ScheduleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleGeneratorSlot_ScheduleGeneratorId",
                table: "ScheduleGeneratorSlot",
                column: "ScheduleGeneratorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduleGeneratorSlot");

            migrationBuilder.DropTable(
                name: "ScheduleGenerator");
        }
    }
}
