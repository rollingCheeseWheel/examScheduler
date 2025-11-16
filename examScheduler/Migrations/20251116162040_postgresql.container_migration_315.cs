using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace examScheduler.Migrations
{
    /// <inheritdoc />
    public partial class postgresqlcontainer_migration_315 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CalendarDay_CalendarWeek_CalendarWeekId",
                table: "CalendarDay");

            migrationBuilder.DropTable(
                name: "CalendarWeek");

            migrationBuilder.RenameColumn(
                name: "CalendarWeekId",
                table: "CalendarDay",
                newName: "CalendarId");

            migrationBuilder.RenameIndex(
                name: "IX_CalendarDay_CalendarWeekId",
                table: "CalendarDay",
                newName: "IX_CalendarDay_CalendarId");

            migrationBuilder.AddColumn<string>(
                name: "ClientID",
                table: "Schools",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SchoolId",
                table: "Schools",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarDay_Calendar_CalendarId",
                table: "CalendarDay",
                column: "CalendarId",
                principalTable: "Calendar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CalendarDay_Calendar_CalendarId",
                table: "CalendarDay");

            migrationBuilder.DropColumn(
                name: "ClientID",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "Schools");

            migrationBuilder.RenameColumn(
                name: "CalendarId",
                table: "CalendarDay",
                newName: "CalendarWeekId");

            migrationBuilder.RenameIndex(
                name: "IX_CalendarDay_CalendarId",
                table: "CalendarDay",
                newName: "IX_CalendarDay_CalendarWeekId");

            migrationBuilder.CreateTable(
                name: "CalendarWeek",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CalendarId = table.Column<int>(type: "integer", nullable: true),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarWeek", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarWeek_Calendar_CalendarId",
                        column: x => x.CalendarId,
                        principalTable: "Calendar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarWeek_CalendarId",
                table: "CalendarWeek",
                column: "CalendarId");

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarDay_CalendarWeek_CalendarWeekId",
                table: "CalendarDay",
                column: "CalendarWeekId",
                principalTable: "CalendarWeek",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
