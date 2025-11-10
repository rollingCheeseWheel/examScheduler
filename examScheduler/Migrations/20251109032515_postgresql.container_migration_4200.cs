using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace examScheduler.Migrations
{
    /// <inheritdoc />
    public partial class postgresqlcontainer_migration_4200 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Calendar_Classrooms_ClassroomId",
                table: "Calendar");

            migrationBuilder.DropForeignKey(
                name: "FK_CalendarDay_CalendarWeek_CalendarWeekId",
                table: "CalendarDay");

            migrationBuilder.DropForeignKey(
                name: "FK_CalendarWeek_Calendar_CalendarId",
                table: "CalendarWeek");

            migrationBuilder.DropForeignKey(
                name: "FK_HourInDay_CalendarDay_CalendarDayId",
                table: "HourInDay");

            migrationBuilder.AddForeignKey(
                name: "FK_Calendar_Classrooms_ClassroomId",
                table: "Calendar",
                column: "ClassroomId",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarDay_CalendarWeek_CalendarWeekId",
                table: "CalendarDay",
                column: "CalendarWeekId",
                principalTable: "CalendarWeek",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarWeek_Calendar_CalendarId",
                table: "CalendarWeek",
                column: "CalendarId",
                principalTable: "Calendar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HourInDay_CalendarDay_CalendarDayId",
                table: "HourInDay",
                column: "CalendarDayId",
                principalTable: "CalendarDay",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Calendar_Classrooms_ClassroomId",
                table: "Calendar");

            migrationBuilder.DropForeignKey(
                name: "FK_CalendarDay_CalendarWeek_CalendarWeekId",
                table: "CalendarDay");

            migrationBuilder.DropForeignKey(
                name: "FK_CalendarWeek_Calendar_CalendarId",
                table: "CalendarWeek");

            migrationBuilder.DropForeignKey(
                name: "FK_HourInDay_CalendarDay_CalendarDayId",
                table: "HourInDay");

            migrationBuilder.AddForeignKey(
                name: "FK_Calendar_Classrooms_ClassroomId",
                table: "Calendar",
                column: "ClassroomId",
                principalTable: "Classrooms",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarDay_CalendarWeek_CalendarWeekId",
                table: "CalendarDay",
                column: "CalendarWeekId",
                principalTable: "CalendarWeek",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarWeek_Calendar_CalendarId",
                table: "CalendarWeek",
                column: "CalendarId",
                principalTable: "Calendar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HourInDay_CalendarDay_CalendarDayId",
                table: "HourInDay",
                column: "CalendarDayId",
                principalTable: "CalendarDay",
                principalColumn: "Id");
        }
    }
}
