using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace examScheduler.Migrations
{
    /// <inheritdoc />
    public partial class postgresqlcontainer_migration_224 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LessonTeacher_Lesson_LessonId",
                table: "LessonTeacher");

            migrationBuilder.DropForeignKey(
                name: "FK_Teachers_Classrooms_ClassroomId",
                table: "Teachers");

            migrationBuilder.DropIndex(
                name: "IX_Teachers_ClassroomId",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "ClassroomId",
                table: "Teachers");

            migrationBuilder.RenameColumn(
                name: "LessonId",
                table: "LessonTeacher",
                newName: "LessonsId");

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
                name: "IX_ClassroomTeacher_TeachersId",
                table: "ClassroomTeacher",
                column: "TeachersId");

            migrationBuilder.AddForeignKey(
                name: "FK_LessonTeacher_Lesson_LessonsId",
                table: "LessonTeacher",
                column: "LessonsId",
                principalTable: "Lesson",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LessonTeacher_Lesson_LessonsId",
                table: "LessonTeacher");

            migrationBuilder.DropTable(
                name: "ClassroomTeacher");

            migrationBuilder.RenameColumn(
                name: "LessonsId",
                table: "LessonTeacher",
                newName: "LessonId");

            migrationBuilder.AddColumn<Guid>(
                name: "ClassroomId",
                table: "Teachers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_ClassroomId",
                table: "Teachers",
                column: "ClassroomId");

            migrationBuilder.AddForeignKey(
                name: "FK_LessonTeacher_Lesson_LessonId",
                table: "LessonTeacher",
                column: "LessonId",
                principalTable: "Lesson",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Teachers_Classrooms_ClassroomId",
                table: "Teachers",
                column: "ClassroomId",
                principalTable: "Classrooms",
                principalColumn: "Id");
        }
    }
}
