using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace examScheduler.Migrations
{
    /// <inheritdoc />
    public partial class postgresqlcontainer_migration_564 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokenSession_AspNetUsers_UserProfileId",
                table: "RefreshTokenSession");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RefreshTokenSession",
                table: "RefreshTokenSession");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokenSession_UserProfileId",
                table: "RefreshTokenSession");

            migrationBuilder.RenameTable(
                name: "RefreshTokenSession",
                newName: "RefreshSessions");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RefreshSessions",
                table: "RefreshSessions",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshSessions_TokenValue",
                table: "RefreshSessions",
                column: "TokenValue",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_RefreshSessions",
                table: "RefreshSessions");

            migrationBuilder.DropIndex(
                name: "IX_RefreshSessions_TokenValue",
                table: "RefreshSessions");

            migrationBuilder.RenameTable(
                name: "RefreshSessions",
                newName: "RefreshTokenSession");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RefreshTokenSession",
                table: "RefreshTokenSession",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokenSession_UserProfileId",
                table: "RefreshTokenSession",
                column: "UserProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokenSession_AspNetUsers_UserProfileId",
                table: "RefreshTokenSession",
                column: "UserProfileId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
