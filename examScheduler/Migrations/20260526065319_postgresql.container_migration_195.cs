using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace examScheduler.Migrations
{
    /// <inheritdoc />
    public partial class postgresqlcontainer_migration_195 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset[]>(
                name: "BlacklistedDays",
                table: "ScheduleGenerator",
                type: "timestamp with time zone[]",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(DateOnly[]),
                oldType: "date[]",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "StartDate",
                table: "_Schedules",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<DateTimeOffset[]>(
                name: "Occurances",
                table: "_Lessons",
                type: "timestamp with time zone[]",
                nullable: false,
                oldClrType: typeof(DateOnly[]),
                oldType: "date[]");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Date",
                table: "_ExamSlots",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateOnly[]>(
                name: "BlacklistedDays",
                table: "ScheduleGenerator",
                type: "date[]",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(DateTimeOffset[]),
                oldType: "timestamp with time zone[]",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "StartDate",
                table: "_Schedules",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateOnly[]>(
                name: "Occurances",
                table: "_Lessons",
                type: "date[]",
                nullable: false,
                oldClrType: typeof(DateTimeOffset[]),
                oldType: "timestamp with time zone[]");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "Date",
                table: "_ExamSlots",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");
        }
    }
}
