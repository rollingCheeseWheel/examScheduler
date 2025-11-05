using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace examScheduler.Migrations
{
    /// <inheritdoc />
    public partial class postgresqlcontainer_migration_673hopefully_final : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_RegisterUsername_RegisterUri_RegisterId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Classrooms_RegisterId_RegisterUri",
                table: "Classrooms");

            migrationBuilder.DropColumn(
                name: "RegisterUri",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "TTCID",
                table: "Lesson");

            migrationBuilder.DropColumn(
                name: "RegisterUri",
                table: "Classrooms");

            migrationBuilder.AddColumn<int>(
                name: "SchoolId",
                table: "UserProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SchoolId",
                table: "Classrooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Schools",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    RegisterUri = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schools", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_RegisterUsername_SchoolId_RegisterId",
                table: "UserProfiles",
                columns: new[] { "RegisterUsername", "SchoolId", "RegisterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_SchoolId",
                table: "UserProfiles",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_Classrooms_RegisterId_SchoolId",
                table: "Classrooms",
                columns: new[] { "RegisterId", "SchoolId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Classrooms_SchoolId",
                table: "Classrooms",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_Schools_RegisterUri",
                table: "Schools",
                column: "RegisterUri",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Classrooms_Schools_SchoolId",
                table: "Classrooms",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProfiles_Schools_SchoolId",
                table: "UserProfiles",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classrooms_Schools_SchoolId",
                table: "Classrooms");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProfiles_Schools_SchoolId",
                table: "UserProfiles");

            migrationBuilder.DropTable(
                name: "Schools");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_RegisterUsername_SchoolId_RegisterId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_SchoolId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Classrooms_RegisterId_SchoolId",
                table: "Classrooms");

            migrationBuilder.DropIndex(
                name: "IX_Classrooms_SchoolId",
                table: "Classrooms");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "Classrooms");

            migrationBuilder.AddColumn<string>(
                name: "RegisterUri",
                table: "UserProfiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TTCID",
                table: "Lesson",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RegisterUri",
                table: "Classrooms",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_RegisterUsername_RegisterUri_RegisterId",
                table: "UserProfiles",
                columns: new[] { "RegisterUsername", "RegisterUri", "RegisterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Classrooms_RegisterId_RegisterUri",
                table: "Classrooms",
                columns: new[] { "RegisterId", "RegisterUri" },
                unique: true);
        }
    }
}
