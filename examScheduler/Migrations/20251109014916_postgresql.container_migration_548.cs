using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace examScheduler.Migrations
{
    /// <inheritdoc />
    public partial class postgresqlcontainer_migration_548 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassroomTeacher_Teachers_TeachersId",
                table: "ClassroomTeacher");

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

            migrationBuilder.RenameTable(
                name: "Teachers",
                newName: "Teacher");

            migrationBuilder.RenameIndex(
                name: "IX_Teachers_LessonId",
                table: "Teacher",
                newName: "IX_Teacher_LessonId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Teacher",
                table: "Teacher",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassroomTeacher_Teacher_TeachersId",
                table: "ClassroomTeacher",
                column: "TeachersId",
                principalTable: "Teacher",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassroomTeacher_Teacher_TeachersId",
                table: "ClassroomTeacher");

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

            migrationBuilder.RenameTable(
                name: "Teacher",
                newName: "Teachers");

            migrationBuilder.RenameIndex(
                name: "IX_Teacher_LessonId",
                table: "Teachers",
                newName: "IX_Teachers_LessonId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Teachers",
                table: "Teachers",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassroomTeacher_Teachers_TeachersId",
                table: "ClassroomTeacher",
                column: "TeachersId",
                principalTable: "Teachers",
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
    }
}
