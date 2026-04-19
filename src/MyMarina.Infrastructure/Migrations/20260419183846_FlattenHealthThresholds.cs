using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMarina.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FlattenHealthThresholds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "HealthTargets", table: "Marinas");

            migrationBuilder.AddColumn<decimal>(
                name: "OccupancyWarningThreshold",
                table: "Marinas",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OccupancyAlertThreshold",
                table: "Marinas",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OverdueWarningDays",
                table: "Marinas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OverdueAlertDays",
                table: "Marinas",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "OccupancyWarningThreshold", table: "Marinas");
            migrationBuilder.DropColumn(name: "OccupancyAlertThreshold", table: "Marinas");
            migrationBuilder.DropColumn(name: "OverdueWarningDays", table: "Marinas");
            migrationBuilder.DropColumn(name: "OverdueAlertDays", table: "Marinas");

            migrationBuilder.AddColumn<string>(
                name: "HealthTargets",
                table: "Marinas",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");
        }
    }
}
