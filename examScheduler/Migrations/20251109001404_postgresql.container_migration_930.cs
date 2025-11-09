using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace examScheduler.Migrations
{
    /// <inheritdoc />
    public partial class postgresqlcontainer_migration_930 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Calendar_Classrooms_ClassroomId",
                table: "Calendar");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassroomTeacher_Teacher_TeachersId",
                table: "ClassroomTeacher");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamSlotStudentProfile_Students_ParticipantsId",
                table: "ExamSlotStudentProfile");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamSlotStudentProfile1_Students_ActuallyParticipatedId",
                table: "ExamSlotStudentProfile1");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_AspNetUsers_Id",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_Classrooms_ClassroomId",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_Subject_Teacher_TeacherId",
                table: "Subject");

            migrationBuilder.DropForeignKey(
                name: "FK_Teacher_Lesson_LessonId",
                table: "Teacher");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherProfiles_Teacher_TeacherId",
                table: "TeacherProfiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Teacher",
                table: "Teacher");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Students",
                table: "Students");

            migrationBuilder.RenameTable(
                name: "Teacher",
                newName: "Teachers");

            migrationBuilder.RenameTable(
                name: "Students",
                newName: "StudentProfiles");

            migrationBuilder.RenameIndex(
                name: "IX_Teacher_LessonId",
                table: "Teachers",
                newName: "IX_Teachers_LessonId");

            migrationBuilder.RenameIndex(
                name: "IX_Students_ClassroomId",
                table: "StudentProfiles",
                newName: "IX_StudentProfiles_ClassroomId");

            migrationBuilder.AlterColumn<int>(
                name: "ClassroomId",
                table: "Calendar",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Teachers",
                table: "Teachers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StudentProfiles",
                table: "StudentProfiles",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Calendar_Classrooms_ClassroomId",
                table: "Calendar",
                column: "ClassroomId",
                principalTable: "Classrooms",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassroomTeacher_Teachers_TeachersId",
                table: "ClassroomTeacher",
                column: "TeachersId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamSlotStudentProfile_StudentProfiles_ParticipantsId",
                table: "ExamSlotStudentProfile",
                column: "ParticipantsId",
                principalTable: "StudentProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamSlotStudentProfile1_StudentProfiles_ActuallyParticipate~",
                table: "ExamSlotStudentProfile1",
                column: "ActuallyParticipatedId",
                principalTable: "StudentProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentProfiles_AspNetUsers_Id",
                table: "StudentProfiles",
                column: "Id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentProfiles_Classrooms_ClassroomId",
                table: "StudentProfiles",
                column: "ClassroomId",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Subject_Teachers_TeacherId",
                table: "Subject",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherProfiles_Teachers_TeacherId",
                table: "TeacherProfiles",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Teachers_Lesson_LessonId",
                table: "Teachers",
                column: "LessonId",
                principalTable: "Lesson",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Calendar_Classrooms_ClassroomId",
                table: "Calendar");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassroomTeacher_Teachers_TeachersId",
                table: "ClassroomTeacher");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamSlotStudentProfile_StudentProfiles_ParticipantsId",
                table: "ExamSlotStudentProfile");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamSlotStudentProfile1_StudentProfiles_ActuallyParticipate~",
                table: "ExamSlotStudentProfile1");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentProfiles_AspNetUsers_Id",
                table: "StudentProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentProfiles_Classrooms_ClassroomId",
                table: "StudentProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Subject_Teachers_TeacherId",
                table: "Subject");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherProfiles_Teachers_TeacherId",
                table: "TeacherProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Teachers_Lesson_LessonId",
                table: "Teachers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Teachers",
                table: "Teachers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StudentProfiles",
                table: "StudentProfiles");

            migrationBuilder.RenameTable(
                name: "Teachers",
                newName: "Teacher");

            migrationBuilder.RenameTable(
                name: "StudentProfiles",
                newName: "Students");

            migrationBuilder.RenameIndex(
                name: "IX_Teachers_LessonId",
                table: "Teacher",
                newName: "IX_Teacher_LessonId");

            migrationBuilder.RenameIndex(
                name: "IX_StudentProfiles_ClassroomId",
                table: "Students",
                newName: "IX_Students_ClassroomId");

            migrationBuilder.AlterColumn<int>(
                name: "ClassroomId",
                table: "Calendar",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Teacher",
                table: "Teacher",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Students",
                table: "Students",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Calendar_Classrooms_ClassroomId",
                table: "Calendar",
                column: "ClassroomId",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassroomTeacher_Teacher_TeachersId",
                table: "ClassroomTeacher",
                column: "TeachersId",
                principalTable: "Teacher",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamSlotStudentProfile_Students_ParticipantsId",
                table: "ExamSlotStudentProfile",
                column: "ParticipantsId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamSlotStudentProfile1_Students_ActuallyParticipatedId",
                table: "ExamSlotStudentProfile1",
                column: "ActuallyParticipatedId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_AspNetUsers_Id",
                table: "Students",
                column: "Id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Classrooms_ClassroomId",
                table: "Students",
                column: "ClassroomId",
                principalTable: "Classrooms",
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
        }
    }
}
