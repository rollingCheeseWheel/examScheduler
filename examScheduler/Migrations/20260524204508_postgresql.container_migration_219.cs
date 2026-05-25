using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace examScheduler.Migrations
{
    /// <inheritdoc />
    public partial class postgresqlcontainer_migration_219 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__TeacherProfiles_Teachers_TeacherId",
                table: "_TeacherProfiles");

            migrationBuilder.DropIndex(
                name: "IX__TeacherProfiles_TeacherId",
                table: "_TeacherProfiles");

            migrationBuilder.AddColumn<Guid>(
                name: "TeacherProfileId",
                table: "Teachers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_TeacherProfileId",
                table: "Teachers",
                column: "TeacherProfileId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Teachers__TeacherProfiles_TeacherProfileId",
                table: "Teachers",
                column: "TeacherProfileId",
                principalTable: "_TeacherProfiles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teachers__TeacherProfiles_TeacherProfileId",
                table: "Teachers");

            migrationBuilder.DropIndex(
                name: "IX_Teachers_TeacherProfileId",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "TeacherProfileId",
                table: "Teachers");

            migrationBuilder.CreateIndex(
                name: "IX__TeacherProfiles_TeacherId",
                table: "_TeacherProfiles",
                column: "TeacherId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK__TeacherProfiles_Teachers_TeacherId",
                table: "_TeacherProfiles",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id");
        }
    }
}
