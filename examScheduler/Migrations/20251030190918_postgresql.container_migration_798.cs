using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace examScheduler.Migrations
{
    /// <inheritdoc />
    public partial class postgresqlcontainer_migration_798 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_RegisterProfile_RegisterProfileId",
                table: "Students");

            migrationBuilder.DropTable(
                name: "RegisterProfile");

            migrationBuilder.DropIndex(
                name: "IX_Teachers_FirstName_LastName_RegisterId",
                table: "Teachers");

            migrationBuilder.DropIndex(
                name: "IX_Students_RegisterUsername",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Classrooms_RegisterId",
                table: "Classrooms");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Hash",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Permissions",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "RegisterUri",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "RegisterUsername",
                table: "Students");

            migrationBuilder.RenameColumn(
                name: "RegisterId",
                table: "Teachers",
                newName: "RegisterID");

            migrationBuilder.RenameColumn(
                name: "RegisterProfileId",
                table: "Students",
                newName: "UserProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_Students_RegisterProfileId",
                table: "Students",
                newName: "IX_Students_UserProfileId");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Teachers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Teachers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "RegisterUri",
                table: "Teachers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Subjects",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<bool>(
                name: "AlreadyHappened",
                table: "ExamSlot",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Classrooms",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "RegisterUri",
                table: "Classrooms",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "PerformedBy",
                table: "AuditLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Details",
                table: "AuditLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024);

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "AuditLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "RegisterUri",
                table: "AuditLogs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RegisterId = table.Column<int>(type: "integer", nullable: false),
                    RegisterUri = table.Column<string>(type: "text", nullable: false),
                    RegisterUsername = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Hash = table.Column<string>(type: "text", nullable: false),
                    Permissions = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeacherProfile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserProfileId = table.Column<int>(type: "integer", nullable: false),
                    TeacherId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherProfile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherProfile_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeacherProfile_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Classrooms_RegisterId_RegisterUri",
                table: "Classrooms",
                columns: new[] { "RegisterId", "RegisterUri" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherProfile_TeacherId",
                table: "TeacherProfile",
                column: "TeacherId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherProfile_UserProfileId",
                table: "TeacherProfile",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_RegisterUsername_RegisterUri_RegisterId",
                table: "UserProfiles",
                columns: new[] { "RegisterUsername", "RegisterUri", "RegisterId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_UserProfiles_UserProfileId",
                table: "Students",
                column: "UserProfileId",
                principalTable: "UserProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_UserProfiles_UserProfileId",
                table: "Students");

            migrationBuilder.DropTable(
                name: "TeacherProfile");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Classrooms_RegisterId_RegisterUri",
                table: "Classrooms");

            migrationBuilder.DropColumn(
                name: "RegisterUri",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "AlreadyHappened",
                table: "ExamSlot");

            migrationBuilder.DropColumn(
                name: "RegisterUri",
                table: "Classrooms");

            migrationBuilder.DropColumn(
                name: "RegisterUri",
                table: "AuditLogs");

            migrationBuilder.RenameColumn(
                name: "RegisterID",
                table: "Teachers",
                newName: "RegisterId");

            migrationBuilder.RenameColumn(
                name: "UserProfileId",
                table: "Students",
                newName: "RegisterProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_Students_UserProfileId",
                table: "Students",
                newName: "IX_Students_RegisterProfileId");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Teachers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Teachers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Subjects",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Students",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Hash",
                table: "Students",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Permissions",
                table: "Students",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RegisterUri",
                table: "Students",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RegisterUsername",
                table: "Students",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Classrooms",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "PerformedBy",
                table: "AuditLogs",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Details",
                table: "AuditLogs",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "AuditLogs",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "RegisterProfile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Language = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Picture = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    RoleName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegisterProfile", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_FirstName_LastName_RegisterId",
                table: "Teachers",
                columns: new[] { "FirstName", "LastName", "RegisterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_RegisterUsername",
                table: "Students",
                column: "RegisterUsername",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Classrooms_RegisterId",
                table: "Classrooms",
                column: "RegisterId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_RegisterProfile_RegisterProfileId",
                table: "Students",
                column: "RegisterProfileId",
                principalTable: "RegisterProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
