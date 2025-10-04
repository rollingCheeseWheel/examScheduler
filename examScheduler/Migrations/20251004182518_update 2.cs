using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace examScheduler.Migrations
{
    /// <inheritdoc />
    public partial class update2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Teachers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Students_RegisterUsername",
                table: "Students",
                column: "RegisterUsername",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_Salt",
                table: "Students",
                column: "Salt",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Students_RegisterUsername",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_Salt",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Teachers");
        }
    }
}
