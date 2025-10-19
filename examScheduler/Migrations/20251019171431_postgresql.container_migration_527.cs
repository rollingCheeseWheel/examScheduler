using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace examScheduler.Migrations
{
    /// <inheritdoc />
    public partial class postgresqlcontainer_migration_527 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teachers_Lesson_LessonId",
                table: "Teachers");

            migrationBuilder.DropIndex(
                name: "IX_Teachers_LessonId",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "LessonId",
                table: "Teachers");

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
                name: "IX_LessonTeacher_TeachersId",
                table: "LessonTeacher",
                column: "TeachersId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LessonTeacher");

            migrationBuilder.AddColumn<int>(
                name: "LessonId",
                table: "Teachers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_LessonId",
                table: "Teachers",
                column: "LessonId");

            migrationBuilder.AddForeignKey(
                name: "FK_Teachers_Lesson_LessonId",
                table: "Teachers",
                column: "LessonId",
                principalTable: "Lesson",
                principalColumn: "Id");
        }
    }
}
