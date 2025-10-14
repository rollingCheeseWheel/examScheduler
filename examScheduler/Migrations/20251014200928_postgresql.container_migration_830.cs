using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace examScheduler.Migrations
{
    /// <inheritdoc />
    public partial class postgresqlcontainer_migration_830 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lesson_Day_DayId",
                table: "Lesson");

            migrationBuilder.DropTable(
                name: "Day");

            migrationBuilder.DropTable(
                name: "SubjectTeacher");

            migrationBuilder.DropTable(
                name: "Week");

            migrationBuilder.DropIndex(
                name: "IX_Lesson_DayId",
                table: "Lesson");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Timetables");

            migrationBuilder.DropColumn(
                name: "DurationInHours",
                table: "Lesson");

            migrationBuilder.DropColumn(
                name: "StartHour",
                table: "Lesson");

            migrationBuilder.RenameColumn(
                name: "Start",
                table: "Lesson",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "DayId",
                table: "Lesson",
                newName: "RegisterId");

            migrationBuilder.AddColumn<int>(
                name: "RegisterId",
                table: "Subjects",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TeacherId",
                table: "Subjects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegisterProfileId",
                table: "Students",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClassId",
                table: "Lesson",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ClassName",
                table: "Lesson",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Hour",
                table: "Lesson",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "LinkToPreviousHour",
                table: "Lesson",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TTCID",
                table: "Lesson",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ToHour",
                table: "Lesson",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CalendarWeek",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimetableId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarWeek", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarWeek_Timetables_TimetableId",
                        column: x => x.TimetableId,
                        principalTable: "Timetables",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RegisterProfile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: false),
                    RoleName = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Picture = table.Column<string>(type: "text", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegisterProfile", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Teacher",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RegisterId = table.Column<int>(type: "integer", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    LessonId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teacher", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teacher_Lesson_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lesson",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CalendarDay",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CalendarWeekId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarDay", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarDay_CalendarWeek_CalendarWeekId",
                        column: x => x.CalendarWeekId,
                        principalTable: "CalendarWeek",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HourInDay",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsLesson = table.Column<bool>(type: "boolean", nullable: false),
                    LessonId = table.Column<int>(type: "integer", nullable: false),
                    Hour = table.Column<int>(type: "integer", nullable: false),
                    LinkedHoursCount = table.Column<int>(type: "integer", nullable: false),
                    CalendarDayId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HourInDay", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HourInDay_CalendarDay_CalendarDayId",
                        column: x => x.CalendarDayId,
                        principalTable: "CalendarDay",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HourInDay_Lesson_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lesson",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_TeacherId",
                table: "Subjects",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_RegisterProfileId",
                table: "Students",
                column: "RegisterProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarDay_CalendarWeekId",
                table: "CalendarDay",
                column: "CalendarWeekId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarWeek_TimetableId",
                table: "CalendarWeek",
                column: "TimetableId");

            migrationBuilder.CreateIndex(
                name: "IX_HourInDay_CalendarDayId",
                table: "HourInDay",
                column: "CalendarDayId");

            migrationBuilder.CreateIndex(
                name: "IX_HourInDay_LessonId",
                table: "HourInDay",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_Teacher_LessonId",
                table: "Teacher",
                column: "LessonId");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_RegisterProfile_RegisterProfileId",
                table: "Students",
                column: "RegisterProfileId",
                principalTable: "RegisterProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_Teachers_TeacherId",
                table: "Subjects",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_RegisterProfile_RegisterProfileId",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Teachers_TeacherId",
                table: "Subjects");

            migrationBuilder.DropTable(
                name: "HourInDay");

            migrationBuilder.DropTable(
                name: "RegisterProfile");

            migrationBuilder.DropTable(
                name: "Teacher");

            migrationBuilder.DropTable(
                name: "CalendarDay");

            migrationBuilder.DropTable(
                name: "CalendarWeek");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_TeacherId",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Students_RegisterProfileId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "RegisterId",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "TeacherId",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "RegisterProfileId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "ClassId",
                table: "Lesson");

            migrationBuilder.DropColumn(
                name: "ClassName",
                table: "Lesson");

            migrationBuilder.DropColumn(
                name: "Hour",
                table: "Lesson");

            migrationBuilder.DropColumn(
                name: "LinkToPreviousHour",
                table: "Lesson");

            migrationBuilder.DropColumn(
                name: "TTCID",
                table: "Lesson");

            migrationBuilder.DropColumn(
                name: "ToHour",
                table: "Lesson");

            migrationBuilder.RenameColumn(
                name: "RegisterId",
                table: "Lesson",
                newName: "DayId");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "Lesson",
                newName: "Start");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Timetables",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<byte>(
                name: "DurationInHours",
                table: "Lesson",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "StartHour",
                table: "Lesson",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.CreateTable(
                name: "SubjectTeacher",
                columns: table => new
                {
                    SubjectsId = table.Column<int>(type: "integer", nullable: false),
                    TeachersId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectTeacher", x => new { x.SubjectsId, x.TeachersId });
                    table.ForeignKey(
                        name: "FK_SubjectTeacher_Subjects_SubjectsId",
                        column: x => x.SubjectsId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubjectTeacher_Teachers_TeachersId",
                        column: x => x.TeachersId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Week",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimetableId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Week", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Week_Timetables_TimetableId",
                        column: x => x.TimetableId,
                        principalTable: "Timetables",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Day",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    WeekId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Day", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Day_Week_WeekId",
                        column: x => x.WeekId,
                        principalTable: "Week",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lesson_DayId",
                table: "Lesson",
                column: "DayId");

            migrationBuilder.CreateIndex(
                name: "IX_Day_WeekId",
                table: "Day",
                column: "WeekId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectTeacher_TeachersId",
                table: "SubjectTeacher",
                column: "TeachersId");

            migrationBuilder.CreateIndex(
                name: "IX_Week_TimetableId",
                table: "Week",
                column: "TimetableId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lesson_Day_DayId",
                table: "Lesson",
                column: "DayId",
                principalTable: "Day",
                principalColumn: "Id");
        }
    }
}
