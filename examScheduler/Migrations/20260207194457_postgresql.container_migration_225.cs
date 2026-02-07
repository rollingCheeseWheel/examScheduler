using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace examScheduler.Migrations
{
    /// <inheritdoc />
    public partial class postgresqlcontainer_migration_225 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_UserName_SchoolId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "Teachers",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "IsPostGenerated",
                table: "ExamSlot",
                newName: "IsGenerated");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "AspNetUsers",
                newName: "Name");

            migrationBuilder.AddColumn<bool>(
                name: "HasBeenProcessed",
                table: "ExamSlot",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_RegiserId_SchoolId",
                table: "AspNetUsers",
                columns: new[] { "RegiserId", "SchoolId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_RegiserId_SchoolId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "HasBeenProcessed",
                table: "ExamSlot");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Teachers",
                newName: "LastName");

            migrationBuilder.RenameColumn(
                name: "IsGenerated",
                table: "ExamSlot",
                newName: "IsPostGenerated");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "AspNetUsers",
                newName: "LastName");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Teachers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_UserName_SchoolId",
                table: "AspNetUsers",
                columns: new[] { "UserName", "SchoolId" },
                unique: true);
        }
    }
}
