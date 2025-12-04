using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace examScheduler.Migrations
{
    /// <inheritdoc />
    public partial class postgresqlcontainer_migration_723 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLog_Classrooms_ClassroomId",
                table: "AuditLog");

            migrationBuilder.DropForeignKey(
                name: "FK_Lesson_CalendarDay_CalendarDayId",
                table: "Lesson");

            migrationBuilder.DropTable(
                name: "CalendarDay");

            migrationBuilder.DropTable(
                name: "ClassroomTeacher");

            migrationBuilder.DropIndex(
                name: "IX_Calendar_ClassroomId",
                table: "Calendar");

            migrationBuilder.DropIndex(
                name: "IX_AuditLog_ClassroomId",
                table: "AuditLog");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "Lesson");

            migrationBuilder.DropColumn(
                name: "IsSecretary",
                table: "Lesson");

            migrationBuilder.DropColumn(
                name: "IsSubtitute",
                table: "Lesson");

            migrationBuilder.DropColumn(
                name: "RegisterId",
                table: "Lesson");

            migrationBuilder.DropColumn(
                name: "TimesScanned",
                table: "Calendar");

            migrationBuilder.DropColumn(
                name: "ClassroomId",
                table: "AuditLog");

            migrationBuilder.RenameColumn(
                name: "CalendarDayId",
                table: "Lesson",
                newName: "CalendarId");

            migrationBuilder.RenameIndex(
                name: "IX_Lesson_CalendarDayId",
                table: "Lesson",
                newName: "IX_Lesson_CalendarId");

            migrationBuilder.RenameColumn(
                name: "LastScanned",
                table: "Calendar",
                newName: "LastsUntil");

            migrationBuilder.AddColumn<Guid>(
                name: "ClassroomId",
                table: "Teachers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset[]>(
                name: "Occurances",
                table: "Lesson",
                type: "timestamp with time zone[]",
                nullable: false,
                defaultValue: new DateTimeOffset[0]);

            migrationBuilder.AlterColumn<Guid>(
                name: "ClassroomId",
                table: "Calendar",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AuditLog",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<Guid>(
                name: "Actor",
                table: "AuditLog",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_ClassroomId",
                table: "Teachers",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_Calendar_ClassroomId",
                table: "Calendar",
                column: "ClassroomId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Lesson_Calendar_CalendarId",
                table: "Lesson",
                column: "CalendarId",
                principalTable: "Calendar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Teachers_Classrooms_ClassroomId",
                table: "Teachers",
                column: "ClassroomId",
                principalTable: "Classrooms",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lesson_Calendar_CalendarId",
                table: "Lesson");

            migrationBuilder.DropForeignKey(
                name: "FK_Teachers_Classrooms_ClassroomId",
                table: "Teachers");

            migrationBuilder.DropIndex(
                name: "IX_Teachers_ClassroomId",
                table: "Teachers");

            migrationBuilder.DropIndex(
                name: "IX_Calendar_ClassroomId",
                table: "Calendar");

            migrationBuilder.DropColumn(
                name: "ClassroomId",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "Occurances",
                table: "Lesson");

            migrationBuilder.RenameColumn(
                name: "CalendarId",
                table: "Lesson",
                newName: "CalendarDayId");

            migrationBuilder.RenameIndex(
                name: "IX_Lesson_CalendarId",
                table: "Lesson",
                newName: "IX_Lesson_CalendarDayId");

            migrationBuilder.RenameColumn(
                name: "LastsUntil",
                table: "Calendar",
                newName: "LastScanned");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Date",
                table: "Lesson",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<bool>(
                name: "IsSecretary",
                table: "Lesson",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSubtitute",
                table: "Lesson",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RegisterId",
                table: "Lesson",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ClassroomId",
                table: "Calendar",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "TimesScanned",
                table: "Calendar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AuditLog",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Actor",
                table: "AuditLog",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "ClassroomId",
                table: "AuditLog",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "CalendarDay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CalendarId = table.Column<Guid>(type: "uuid", nullable: true),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarDay", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarDay_Calendar_CalendarId",
                        column: x => x.CalendarId,
                        principalTable: "Calendar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClassroomTeacher",
                columns: table => new
                {
                    ClassroomsId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeachersId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassroomTeacher", x => new { x.ClassroomsId, x.TeachersId });
                    table.ForeignKey(
                        name: "FK_ClassroomTeacher_Classrooms_ClassroomsId",
                        column: x => x.ClassroomsId,
                        principalTable: "Classrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClassroomTeacher_Teachers_TeachersId",
                        column: x => x.TeachersId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Calendar_ClassroomId",
                table: "Calendar",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_ClassroomId",
                table: "AuditLog",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarDay_CalendarId",
                table: "CalendarDay",
                column: "CalendarId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomTeacher_TeachersId",
                table: "ClassroomTeacher",
                column: "TeachersId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLog_Classrooms_ClassroomId",
                table: "AuditLog",
                column: "ClassroomId",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Lesson_CalendarDay_CalendarDayId",
                table: "Lesson",
                column: "CalendarDayId",
                principalTable: "CalendarDay",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
