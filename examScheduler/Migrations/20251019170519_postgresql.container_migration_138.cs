using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace examScheduler.Migrations
{
    /// <inheritdoc />
    public partial class postgresqlcontainer_migration_138 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExamSlot_Schedules_ScheduleId",
                table: "ExamSlot");

            migrationBuilder.AlterColumn<int>(
                name: "ScheduleId",
                table: "ExamSlot",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamSlot_Schedules_ScheduleId",
                table: "ExamSlot",
                column: "ScheduleId",
                principalTable: "Schedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExamSlot_Schedules_ScheduleId",
                table: "ExamSlot");

            migrationBuilder.AlterColumn<int>(
                name: "ScheduleId",
                table: "ExamSlot",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamSlot_Schedules_ScheduleId",
                table: "ExamSlot",
                column: "ScheduleId",
                principalTable: "Schedules",
                principalColumn: "Id");
        }
    }
}
