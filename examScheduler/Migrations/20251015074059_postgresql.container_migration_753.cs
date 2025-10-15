using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace examScheduler.Migrations
{
    /// <inheritdoc />
    public partial class postgresqlcontainer_migration_753 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExamSlot_Classrooms_ClassroomId",
                table: "ExamSlot");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamSlot_Lesson_PeriodId",
                table: "ExamSlot");

            migrationBuilder.DropIndex(
                name: "IX_ExamSlot_ClassroomId",
                table: "ExamSlot");

            migrationBuilder.DropIndex(
                name: "IX_ExamSlot_PeriodId",
                table: "ExamSlot");

            migrationBuilder.RenameColumn(
                name: "PeriodId",
                table: "ExamSlot",
                newName: "RequiredParticipants");

            migrationBuilder.RenameColumn(
                name: "ClassroomId",
                table: "ExamSlot",
                newName: "MaxParticipants");

            migrationBuilder.AddColumn<int>(
                name: "ExamSlotId",
                table: "Students",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_ExamSlotId",
                table: "Students",
                column: "ExamSlotId");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_ExamSlot_ExamSlotId",
                table: "Students",
                column: "ExamSlotId",
                principalTable: "ExamSlot",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_ExamSlot_ExamSlotId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_ExamSlotId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "ExamSlotId",
                table: "Students");

            migrationBuilder.RenameColumn(
                name: "RequiredParticipants",
                table: "ExamSlot",
                newName: "PeriodId");

            migrationBuilder.RenameColumn(
                name: "MaxParticipants",
                table: "ExamSlot",
                newName: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSlot_ClassroomId",
                table: "ExamSlot",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSlot_PeriodId",
                table: "ExamSlot",
                column: "PeriodId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamSlot_Classrooms_ClassroomId",
                table: "ExamSlot",
                column: "ClassroomId",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamSlot_Lesson_PeriodId",
                table: "ExamSlot",
                column: "PeriodId",
                principalTable: "Lesson",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
