using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace examScheduler.Migrations
{
    /// <inheritdoc />
    public partial class postgresqlcontainer_migration_391 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teachers_Timetables_TimetableId",
                table: "Teachers");

            migrationBuilder.DropIndex(
                name: "IX_Teachers_TimetableId",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "TimetableId",
                table: "Teachers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TimetableId",
                table: "Teachers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_TimetableId",
                table: "Teachers",
                column: "TimetableId");

            migrationBuilder.AddForeignKey(
                name: "FK_Teachers_Timetables_TimetableId",
                table: "Teachers",
                column: "TimetableId",
                principalTable: "Timetables",
                principalColumn: "Id");
        }
    }
}
