using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace examScheduler.Migrations
{
    /// <inheritdoc />
    public partial class postgresqlcontainer_migration_302 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "LockedAt",
                table: "ExamSlot");

            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                table: "ExamSlot");

            migrationBuilder.AddColumn<bool>(
                name: "IsPostGenerated",
                table: "ExamSlot",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTeacherConfirmed",
                table: "ExamSlot",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockInDate",
                table: "ExamSlot",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPostGenerated",
                table: "ExamSlot");

            migrationBuilder.DropColumn(
                name: "IsTeacherConfirmed",
                table: "ExamSlot");

            migrationBuilder.DropColumn(
                name: "LockInDate",
                table: "ExamSlot");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndDate",
                table: "Schedule",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockedAt",
                table: "ExamSlot",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProcessedAt",
                table: "ExamSlot",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
