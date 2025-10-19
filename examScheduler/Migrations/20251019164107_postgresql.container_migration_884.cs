using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace examScheduler.Migrations
{
    /// <inheritdoc />
    public partial class postgresqlcontainer_migration_884 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Students");

            migrationBuilder.AlterColumn<string>(
                name: "RegisterUsername",
                table: "Students",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "RegisterProfile",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateTable(
                name: "ExamSlotStudent",
                columns: table => new
                {
                    ExamSlotsId = table.Column<int>(type: "integer", nullable: false),
                    ParticipantsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSlotStudent", x => new { x.ExamSlotsId, x.ParticipantsId });
                    table.ForeignKey(
                        name: "FK_ExamSlotStudent_ExamSlot_ExamSlotsId",
                        column: x => x.ExamSlotsId,
                        principalTable: "ExamSlot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamSlotStudent_Students_ParticipantsId",
                        column: x => x.ParticipantsId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamSlotStudent_ParticipantsId",
                table: "ExamSlotStudent",
                column: "ParticipantsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamSlotStudent");

            migrationBuilder.AlterColumn<string>(
                name: "RegisterUsername",
                table: "Students",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "ExamSlotId",
                table: "Students",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Students",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "RegisterProfile",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

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
    }
}
