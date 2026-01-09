using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace examScheduler.Migrations
{
    /// <inheritdoc />
    public partial class postgresqlcontainer_migration_712 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "Schools",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "Schools");
        }
    }
}
