using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace examScheduler.Migrations
{
    /// <inheritdoc />
    public partial class postgresqlcontainer_migration_584 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CalendarWeek_Timetables_CalendarId",
                table: "CalendarWeek");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassroomTeacher_Teachers_TeachersId",
                table: "ClassroomTeacher");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamSlot_Schedules_ScheduleId",
                table: "ExamSlot");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamSlotStudent_ExamSlot_ExamSlotsId",
                table: "ExamSlotStudent");

            migrationBuilder.DropForeignKey(
                name: "FK_Lesson_Subjects_SubjectId",
                table: "Lesson");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Classrooms_ClassroomId",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Subjects_SubjectId",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Teachers_TeacherId",
                table: "Subjects");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherProfile_Teachers_TeacherId",
                table: "TeacherProfile");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherProfile_UserProfiles_UserProfileId",
                table: "TeacherProfile");

            migrationBuilder.DropForeignKey(
                name: "FK_Teachers_Schedules_ScheduleId",
                table: "Teachers");

            migrationBuilder.DropForeignKey(
                name: "FK_Timetables_Classrooms_ClassroomId",
                table: "Timetables");

            migrationBuilder.DropTable(
                name: "LessonTeacher");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExamSlotStudent",
                table: "ExamSlotStudent");

            migrationBuilder.DropIndex(
                name: "IX_ExamSlotStudent_ParticipantsId",
                table: "ExamSlotStudent");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Timetables",
                table: "Timetables");

            migrationBuilder.DropIndex(
                name: "IX_Timetables_ClassroomId",
                table: "Timetables");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Teachers",
                table: "Teachers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TeacherProfile",
                table: "TeacherProfile");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Subjects",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_RegisterId_Name",
                table: "Subjects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Schedules",
                table: "Schedules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditLogs",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "AlreadyHappened",
                table: "ExamSlot");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "ExamSlot");

            migrationBuilder.DropColumn(
                name: "MaxParticipants",
                table: "ExamSlot");

            migrationBuilder.DropColumn(
                name: "CreatedAtUTC",
                table: "Classrooms");

            migrationBuilder.DropColumn(
                name: "RegisterUri",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "lockInDate",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "Details",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "TimestampUTC",
                table: "AuditLogs");

            migrationBuilder.RenameTable(
                name: "Timetables",
                newName: "Calendar");

            migrationBuilder.RenameTable(
                name: "Teachers",
                newName: "Teacher");

            migrationBuilder.RenameTable(
                name: "TeacherProfile",
                newName: "TeacherProfiles");

            migrationBuilder.RenameTable(
                name: "Subjects",
                newName: "Subject");

            migrationBuilder.RenameTable(
                name: "Schedules",
                newName: "Schedule");

            migrationBuilder.RenameTable(
                name: "AuditLogs",
                newName: "AuditLog");

            migrationBuilder.RenameColumn(
                name: "ExamSlotsId",
                table: "ExamSlotStudent",
                newName: "ParticipatingExamSlotsId");

            migrationBuilder.RenameColumn(
                name: "RequiredParticipants",
                table: "ExamSlot",
                newName: "GeneratorSlotId");

            migrationBuilder.RenameColumn(
                name: "ScheduleId",
                table: "Teacher",
                newName: "LessonId");

            migrationBuilder.RenameIndex(
                name: "IX_Teachers_ScheduleId",
                table: "Teacher",
                newName: "IX_Teacher_LessonId");

            migrationBuilder.RenameIndex(
                name: "IX_TeacherProfile_UserProfileId",
                table: "TeacherProfiles",
                newName: "IX_TeacherProfiles_UserProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_TeacherProfile_TeacherId",
                table: "TeacherProfiles",
                newName: "IX_TeacherProfiles_TeacherId");

            migrationBuilder.RenameIndex(
                name: "IX_Subjects_TeacherId",
                table: "Subject",
                newName: "IX_Subject_TeacherId");

            migrationBuilder.RenameIndex(
                name: "IX_Schedules_SubjectId",
                table: "Schedule",
                newName: "IX_Schedule_SubjectId");

            migrationBuilder.RenameIndex(
                name: "IX_Schedules_ClassroomId",
                table: "Schedule",
                newName: "IX_Schedule_ClassroomId");

            migrationBuilder.RenameColumn(
                name: "RegisterUri",
                table: "AuditLog",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "PerformedBy",
                table: "AuditLog",
                newName: "Actor");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastScanned",
                table: "Calendar",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "TimesScanned",
                table: "Calendar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Schedule",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FirstExamination",
                table: "Schedule",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "LockInOffset",
                table: "Schedule",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "ClassroomId",
                table: "AuditLog",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExamSlotStudent",
                table: "ExamSlotStudent",
                columns: new[] { "ParticipantsId", "ParticipatingExamSlotsId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Calendar",
                table: "Calendar",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Teacher",
                table: "Teacher",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TeacherProfiles",
                table: "TeacherProfiles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Subject",
                table: "Subject",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Schedule",
                table: "Schedule",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuditLog",
                table: "AuditLog",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ExamSlotStudent1",
                columns: table => new
                {
                    ActuallyParticipatedExamSlotsId = table.Column<int>(type: "integer", nullable: false),
                    ActuallyParticipatedId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSlotStudent1", x => new { x.ActuallyParticipatedExamSlotsId, x.ActuallyParticipatedId });
                    table.ForeignKey(
                        name: "FK_ExamSlotStudent1_ExamSlot_ActuallyParticipatedExamSlotsId",
                        column: x => x.ActuallyParticipatedExamSlotsId,
                        principalTable: "ExamSlot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamSlotStudent1_Students_ActuallyParticipatedId",
                        column: x => x.ActuallyParticipatedId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleGeneratorSlot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Offset = table.Column<int>(type: "integer", nullable: false),
                    MaxParticipants = table.Column<int>(type: "integer", nullable: false),
                    RequiredParticipants = table.Column<int>(type: "integer", nullable: false),
                    ScheduleId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleGeneratorSlot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleGeneratorSlot_Schedule_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "Schedule",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamSlotStudent_ParticipatingExamSlotsId",
                table: "ExamSlotStudent",
                column: "ParticipatingExamSlotsId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSlot_GeneratorSlotId",
                table: "ExamSlot",
                column: "GeneratorSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_Calendar_ClassroomId",
                table: "Calendar",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_ClassroomId",
                table: "AuditLog",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSlotStudent1_ActuallyParticipatedId",
                table: "ExamSlotStudent1",
                column: "ActuallyParticipatedId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleGeneratorSlot_ScheduleId",
                table: "ScheduleGeneratorSlot",
                column: "ScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLog_Classrooms_ClassroomId",
                table: "AuditLog",
                column: "ClassroomId",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Calendar_Classrooms_ClassroomId",
                table: "Calendar",
                column: "ClassroomId",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarWeek_Calendar_CalendarId",
                table: "CalendarWeek",
                column: "CalendarId",
                principalTable: "Calendar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassroomTeacher_Teacher_TeachersId",
                table: "ClassroomTeacher",
                column: "TeachersId",
                principalTable: "Teacher",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamSlot_ScheduleGeneratorSlot_GeneratorSlotId",
                table: "ExamSlot",
                column: "GeneratorSlotId",
                principalTable: "ScheduleGeneratorSlot",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamSlot_Schedule_ScheduleId",
                table: "ExamSlot",
                column: "ScheduleId",
                principalTable: "Schedule",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamSlotStudent_ExamSlot_ParticipatingExamSlotsId",
                table: "ExamSlotStudent",
                column: "ParticipatingExamSlotsId",
                principalTable: "ExamSlot",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Lesson_Subject_SubjectId",
                table: "Lesson",
                column: "SubjectId",
                principalTable: "Subject",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Schedule_Classrooms_ClassroomId",
                table: "Schedule",
                column: "ClassroomId",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Schedule_Subject_SubjectId",
                table: "Schedule",
                column: "SubjectId",
                principalTable: "Subject",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Subject_Teacher_TeacherId",
                table: "Subject",
                column: "TeacherId",
                principalTable: "Teacher",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Teacher_Lesson_LessonId",
                table: "Teacher",
                column: "LessonId",
                principalTable: "Lesson",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherProfiles_Teacher_TeacherId",
                table: "TeacherProfiles",
                column: "TeacherId",
                principalTable: "Teacher",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherProfiles_UserProfiles_UserProfileId",
                table: "TeacherProfiles",
                column: "UserProfileId",
                principalTable: "UserProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLog_Classrooms_ClassroomId",
                table: "AuditLog");

            migrationBuilder.DropForeignKey(
                name: "FK_Calendar_Classrooms_ClassroomId",
                table: "Calendar");

            migrationBuilder.DropForeignKey(
                name: "FK_CalendarWeek_Calendar_CalendarId",
                table: "CalendarWeek");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassroomTeacher_Teacher_TeachersId",
                table: "ClassroomTeacher");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamSlot_ScheduleGeneratorSlot_GeneratorSlotId",
                table: "ExamSlot");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamSlot_Schedule_ScheduleId",
                table: "ExamSlot");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamSlotStudent_ExamSlot_ParticipatingExamSlotsId",
                table: "ExamSlotStudent");

            migrationBuilder.DropForeignKey(
                name: "FK_Lesson_Subject_SubjectId",
                table: "Lesson");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedule_Classrooms_ClassroomId",
                table: "Schedule");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedule_Subject_SubjectId",
                table: "Schedule");

            migrationBuilder.DropForeignKey(
                name: "FK_Subject_Teacher_TeacherId",
                table: "Subject");

            migrationBuilder.DropForeignKey(
                name: "FK_Teacher_Lesson_LessonId",
                table: "Teacher");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherProfiles_Teacher_TeacherId",
                table: "TeacherProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherProfiles_UserProfiles_UserProfileId",
                table: "TeacherProfiles");

            migrationBuilder.DropTable(
                name: "ExamSlotStudent1");

            migrationBuilder.DropTable(
                name: "ScheduleGeneratorSlot");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExamSlotStudent",
                table: "ExamSlotStudent");

            migrationBuilder.DropIndex(
                name: "IX_ExamSlotStudent_ParticipatingExamSlotsId",
                table: "ExamSlotStudent");

            migrationBuilder.DropIndex(
                name: "IX_ExamSlot_GeneratorSlotId",
                table: "ExamSlot");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TeacherProfiles",
                table: "TeacherProfiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Teacher",
                table: "Teacher");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Subject",
                table: "Subject");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Schedule",
                table: "Schedule");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Calendar",
                table: "Calendar");

            migrationBuilder.DropIndex(
                name: "IX_Calendar_ClassroomId",
                table: "Calendar");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditLog",
                table: "AuditLog");

            migrationBuilder.DropIndex(
                name: "IX_AuditLog_ClassroomId",
                table: "AuditLog");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "FirstExamination",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "LockInOffset",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "LastScanned",
                table: "Calendar");

            migrationBuilder.DropColumn(
                name: "TimesScanned",
                table: "Calendar");

            migrationBuilder.DropColumn(
                name: "ClassroomId",
                table: "AuditLog");

            migrationBuilder.RenameTable(
                name: "TeacherProfiles",
                newName: "TeacherProfile");

            migrationBuilder.RenameTable(
                name: "Teacher",
                newName: "Teachers");

            migrationBuilder.RenameTable(
                name: "Subject",
                newName: "Subjects");

            migrationBuilder.RenameTable(
                name: "Schedule",
                newName: "Schedules");

            migrationBuilder.RenameTable(
                name: "Calendar",
                newName: "Timetables");

            migrationBuilder.RenameTable(
                name: "AuditLog",
                newName: "AuditLogs");

            migrationBuilder.RenameColumn(
                name: "ParticipatingExamSlotsId",
                table: "ExamSlotStudent",
                newName: "ExamSlotsId");

            migrationBuilder.RenameColumn(
                name: "GeneratorSlotId",
                table: "ExamSlot",
                newName: "RequiredParticipants");

            migrationBuilder.RenameIndex(
                name: "IX_TeacherProfiles_UserProfileId",
                table: "TeacherProfile",
                newName: "IX_TeacherProfile_UserProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_TeacherProfiles_TeacherId",
                table: "TeacherProfile",
                newName: "IX_TeacherProfile_TeacherId");

            migrationBuilder.RenameColumn(
                name: "LessonId",
                table: "Teachers",
                newName: "ScheduleId");

            migrationBuilder.RenameIndex(
                name: "IX_Teacher_LessonId",
                table: "Teachers",
                newName: "IX_Teachers_ScheduleId");

            migrationBuilder.RenameIndex(
                name: "IX_Subject_TeacherId",
                table: "Subjects",
                newName: "IX_Subjects_TeacherId");

            migrationBuilder.RenameIndex(
                name: "IX_Schedule_SubjectId",
                table: "Schedules",
                newName: "IX_Schedules_SubjectId");

            migrationBuilder.RenameIndex(
                name: "IX_Schedule_ClassroomId",
                table: "Schedules",
                newName: "IX_Schedules_ClassroomId");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "AuditLogs",
                newName: "RegisterUri");

            migrationBuilder.RenameColumn(
                name: "Actor",
                table: "AuditLogs",
                newName: "PerformedBy");

            migrationBuilder.AddColumn<bool>(
                name: "AlreadyHappened",
                table: "ExamSlot",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "ExamSlot",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxParticipants",
                table: "ExamSlot",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUTC",
                table: "Classrooms",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "RegisterUri",
                table: "Teachers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "lockInDate",
                table: "Schedules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Details",
                table: "AuditLogs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "TimestampUTC",
                table: "AuditLogs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExamSlotStudent",
                table: "ExamSlotStudent",
                columns: new[] { "ExamSlotsId", "ParticipantsId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_TeacherProfile",
                table: "TeacherProfile",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Teachers",
                table: "Teachers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Subjects",
                table: "Subjects",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Schedules",
                table: "Schedules",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Timetables",
                table: "Timetables",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuditLogs",
                table: "AuditLogs",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "LessonTeacher",
                columns: table => new
                {
                    LessonsId = table.Column<int>(type: "integer", nullable: false),
                    TeachersId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonTeacher", x => new { x.LessonsId, x.TeachersId });
                    table.ForeignKey(
                        name: "FK_LessonTeacher_Lesson_LessonsId",
                        column: x => x.LessonsId,
                        principalTable: "Lesson",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LessonTeacher_Teachers_TeachersId",
                        column: x => x.TeachersId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamSlotStudent_ParticipantsId",
                table: "ExamSlotStudent",
                column: "ParticipantsId");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_RegisterId_Name",
                table: "Subjects",
                columns: new[] { "RegisterId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Timetables_ClassroomId",
                table: "Timetables",
                column: "ClassroomId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LessonTeacher_TeachersId",
                table: "LessonTeacher",
                column: "TeachersId");

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarWeek_Timetables_CalendarId",
                table: "CalendarWeek",
                column: "CalendarId",
                principalTable: "Timetables",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassroomTeacher_Teachers_TeachersId",
                table: "ClassroomTeacher",
                column: "TeachersId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamSlot_Schedules_ScheduleId",
                table: "ExamSlot",
                column: "ScheduleId",
                principalTable: "Schedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamSlotStudent_ExamSlot_ExamSlotsId",
                table: "ExamSlotStudent",
                column: "ExamSlotsId",
                principalTable: "ExamSlot",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Lesson_Subjects_SubjectId",
                table: "Lesson",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Classrooms_ClassroomId",
                table: "Schedules",
                column: "ClassroomId",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Subjects_SubjectId",
                table: "Schedules",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_Teachers_TeacherId",
                table: "Subjects",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherProfile_Teachers_TeacherId",
                table: "TeacherProfile",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherProfile_UserProfiles_UserProfileId",
                table: "TeacherProfile",
                column: "UserProfileId",
                principalTable: "UserProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Teachers_Schedules_ScheduleId",
                table: "Teachers",
                column: "ScheduleId",
                principalTable: "Schedules",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Timetables_Classrooms_ClassroomId",
                table: "Timetables",
                column: "ClassroomId",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
