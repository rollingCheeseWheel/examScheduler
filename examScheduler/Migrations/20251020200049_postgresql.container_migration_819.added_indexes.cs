using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace examScheduler.Migrations
{
    /// <inheritdoc />
    public partial class postgresqlcontainer_migration_819added_indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLesson",
                table: "HourInDay");

            migrationBuilder.AddColumn<int>(
                name: "RegisterId",
                table: "Classrooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_FirstName_LastName_RegisterId",
                table: "Teachers",
                columns: new[] { "FirstName", "LastName", "RegisterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_RegisterId_Name",
                table: "Subjects",
                columns: new[] { "RegisterId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Classrooms_RegisterId",
                table: "Classrooms",
                column: "RegisterId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Teachers_FirstName_LastName_RegisterId",
                table: "Teachers");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_RegisterId_Name",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Classrooms_RegisterId",
                table: "Classrooms");

            migrationBuilder.DropColumn(
                name: "RegisterId",
                table: "Classrooms");

            migrationBuilder.AddColumn<bool>(
                name: "IsLesson",
                table: "HourInDay",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
