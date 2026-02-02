using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace examScheduler.Migrations
{
    /// <inheritdoc />
    public partial class postgresqlcontainer_migration_523 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstActorId",
                table: "AuditLog");

            migrationBuilder.DropColumn(
                name: "FirstActorName",
                table: "AuditLog");

            migrationBuilder.RenameColumn(
                name: "SecondActorName",
                table: "AuditLog",
                newName: "TargetName");

            migrationBuilder.RenameColumn(
                name: "SecondActorId",
                table: "AuditLog",
                newName: "TargetId");

            migrationBuilder.RenameColumn(
                name: "ActorType",
                table: "AuditLog",
                newName: "OriginType");

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

            migrationBuilder.AddColumn<Guid>(
                name: "OriginId",
                table: "AuditLog",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "OriginName",
                table: "AuditLog",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TargetType",
                table: "AuditLog",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LockedAt",
                table: "ExamSlot");

            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                table: "ExamSlot");

            migrationBuilder.DropColumn(
                name: "OriginId",
                table: "AuditLog");

            migrationBuilder.DropColumn(
                name: "OriginName",
                table: "AuditLog");

            migrationBuilder.DropColumn(
                name: "TargetType",
                table: "AuditLog");

            migrationBuilder.RenameColumn(
                name: "TargetName",
                table: "AuditLog",
                newName: "SecondActorName");

            migrationBuilder.RenameColumn(
                name: "TargetId",
                table: "AuditLog",
                newName: "SecondActorId");

            migrationBuilder.RenameColumn(
                name: "OriginType",
                table: "AuditLog",
                newName: "ActorType");

            migrationBuilder.AddColumn<Guid>(
                name: "FirstActorId",
                table: "AuditLog",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstActorName",
                table: "AuditLog",
                type: "text",
                nullable: true);
        }
    }
}
