using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMarina.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase7_MarketplaceListings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "availability_windows",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SlipId = table.Column<Guid>(type: "uuid", nullable: false),
                    ListedByKind = table.Column<string>(type: "text", nullable: false),
                    ListedByMarinaId = table.Column<Guid>(type: "uuid", nullable: true),
                    ListedByBillingAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedAssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    InstantBook = table.Column<bool>(type: "boolean", nullable: false),
                    MinNights = table.Column<int>(type: "integer", nullable: true),
                    MaxNights = table.Column<int>(type: "integer", nullable: true),
                    BasePricePerNight = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    WeeklyDiscount = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    MonthlyDiscount = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    CleaningFee = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revenue_split = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_availability_windows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_availability_windows_slips_SlipId",
                        column: x => x.SlipId,
                        principalSchema: "mymarina",
                        principalTable: "slips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_availability_windows_ListedByMarinaId",
                schema: "mymarina",
                table: "availability_windows",
                column: "ListedByMarinaId",
                filter: "listed_by_marina_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_availability_windows_SlipId_StartsAt",
                schema: "mymarina",
                table: "availability_windows",
                columns: new[] { "SlipId", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_availability_windows_Status",
                schema: "mymarina",
                table: "availability_windows",
                column: "Status",
                filter: "status IN ('Open', 'Paused')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "availability_windows",
                schema: "mymarina");
        }
    }
}
