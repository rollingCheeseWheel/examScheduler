using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace examScheduler.Migrations
{
    /// <inheritdoc />
    public partial class postgresqlcontainer_migration_857 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RequiredParticipants",
                table: "ScheduleGeneratorSlot",
                newName: "MinParticipants");

            migrationBuilder.RenameColumn(
                name: "ClassName",
                table: "Lesson",
                newName: "LessonName");

            migrationBuilder.RenameColumn(
                name: "ClassId",
                table: "Lesson",
                newName: "LessonId");

            migrationBuilder.AddColumn<string>(
                name: "ActorName",
                table: "AuditLog",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActorType",
                table: "AuditLog",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActorName",
                table: "AuditLog");

            migrationBuilder.DropColumn(
                name: "ActorType",
                table: "AuditLog");

            migrationBuilder.RenameColumn(
                name: "MinParticipants",
                table: "ScheduleGeneratorSlot",
                newName: "RequiredParticipants");

            migrationBuilder.RenameColumn(
                name: "LessonName",
                table: "Lesson",
                newName: "ClassName");

            migrationBuilder.RenameColumn(
                name: "LessonId",
                table: "Lesson",
                newName: "ClassId");
        }
    }
}
