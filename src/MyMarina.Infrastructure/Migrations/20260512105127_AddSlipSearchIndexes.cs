using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMarina.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSlipSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_slips_Status",
                schema: "mymarina",
                table: "slips",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_marinas_Latitude_Longitude",
                schema: "mymarina",
                table: "marinas",
                columns: new[] { "Latitude", "Longitude" },
                filter: "\"Latitude\" IS NOT NULL AND \"Longitude\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_slips_Status",
                schema: "mymarina",
                table: "slips");

            migrationBuilder.DropIndex(
                name: "IX_marinas_Latitude_Longitude",
                schema: "mymarina",
                table: "marinas");
        }
    }
}
