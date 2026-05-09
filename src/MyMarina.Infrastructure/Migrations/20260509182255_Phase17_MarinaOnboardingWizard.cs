using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMarina.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase17_MarinaOnboardingWizard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Amenities",
                schema: "mymarina",
                table: "slips",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<bool>(
                name: "HasPumpOut",
                schema: "mymarina",
                table: "slips",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCovered",
                schema: "mymarina",
                table: "slips",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsIndoor",
                schema: "mymarina",
                table: "slips",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSetupComplete",
                schema: "mymarina",
                table: "marinas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SetupStep",
                schema: "mymarina",
                table: "marinas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "mymarina",
                table: "marinas",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_marinas_IsSetupComplete",
                schema: "mymarina",
                table: "marinas",
                column: "IsSetupComplete");

            // Backfill: mark all existing marinas as setup-complete so they aren't hidden by the draft filter
            migrationBuilder.Sql(
                "UPDATE mymarina.\"marinas\" SET \"IsSetupComplete\" = true, \"UpdatedAt\" = NOW() WHERE \"IsSetupComplete\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_marinas_IsSetupComplete",
                schema: "mymarina",
                table: "marinas");

            migrationBuilder.DropColumn(
                name: "Amenities",
                schema: "mymarina",
                table: "slips");

            migrationBuilder.DropColumn(
                name: "HasPumpOut",
                schema: "mymarina",
                table: "slips");

            migrationBuilder.DropColumn(
                name: "IsCovered",
                schema: "mymarina",
                table: "slips");

            migrationBuilder.DropColumn(
                name: "IsIndoor",
                schema: "mymarina",
                table: "slips");

            migrationBuilder.DropColumn(
                name: "IsSetupComplete",
                schema: "mymarina",
                table: "marinas");

            migrationBuilder.DropColumn(
                name: "SetupStep",
                schema: "mymarina",
                table: "marinas");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "mymarina",
                table: "marinas");
        }
    }
}
