using Microsoft.EntityFrameworkCore.Migrations;
using MyMarina.Domain.ValueObjects;

#nullable disable

namespace MyMarina.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarinaHealthTargets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<HealthTargets>(
                name: "HealthTargets",
                table: "Marinas",
                type: "jsonb",
                nullable: false);
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
