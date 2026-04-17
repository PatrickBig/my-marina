using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMarina.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarinaHealthTargets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add column as nullable first to backfill existing rows
            migrationBuilder.AddColumn<string>(
                name: "HealthTargets",
                table: "Marinas",
                type: "jsonb",
                nullable: true);

            // Set default values for existing rows (70% occupancy, 30 day AR threshold)
            migrationBuilder.Sql(
                @"UPDATE ""Marinas"" SET ""HealthTargets"" = '{""occupancyRateTarget"": 70, ""overdueARThresholdDays"": 30}'::jsonb WHERE ""HealthTargets"" IS NULL");

            // Now enforce NOT NULL
            migrationBuilder.AlterColumn<string>(
                name: "HealthTargets",
                table: "Marinas",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HealthTargets",
                table: "Marinas");
        }
    }
}
