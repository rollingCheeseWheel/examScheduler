using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace examScheduler.Migrations
{
    /// <inheritdoc />
    public partial class postgresqlcontainer_migration_744 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_RegisterUsername_SchoolId_RegisterId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "RegisterId",
                table: "UserProfiles");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_RegisterUsername_SchoolId",
                table: "UserProfiles",
                columns: new[] { "RegisterUsername", "SchoolId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_RegisterUsername_SchoolId",
                table: "UserProfiles");

            migrationBuilder.AddColumn<int>(
                name: "RegisterId",
                table: "UserProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_RegisterUsername_SchoolId_RegisterId",
                table: "UserProfiles",
                columns: new[] { "RegisterUsername", "SchoolId", "RegisterId" },
                unique: true);
        }
    }
}
