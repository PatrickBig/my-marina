using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMarina.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CascadeDeleteMarinaChildren : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Marinas_MarinaId",
                schema: "mymarina",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_OperatingExpenses_Marinas_MarinaId",
                schema: "mymarina",
                table: "OperatingExpenses");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Marinas_MarinaId",
                schema: "mymarina",
                table: "Invoices",
                column: "MarinaId",
                principalSchema: "mymarina",
                principalTable: "Marinas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OperatingExpenses_Marinas_MarinaId",
                schema: "mymarina",
                table: "OperatingExpenses",
                column: "MarinaId",
                principalSchema: "mymarina",
                principalTable: "Marinas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Marinas_MarinaId",
                schema: "mymarina",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_OperatingExpenses_Marinas_MarinaId",
                schema: "mymarina",
                table: "OperatingExpenses");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Marinas_MarinaId",
                schema: "mymarina",
                table: "Invoices",
                column: "MarinaId",
                principalSchema: "mymarina",
                principalTable: "Marinas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OperatingExpenses_Marinas_MarinaId",
                schema: "mymarina",
                table: "OperatingExpenses",
                column: "MarinaId",
                principalSchema: "mymarina",
                principalTable: "Marinas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
