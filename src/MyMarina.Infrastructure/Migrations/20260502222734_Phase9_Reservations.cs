using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMarina.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase9_Reservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reservations",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BoaterUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VesselId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlipId = table.Column<Guid>(type: "uuid", nullable: false),
                    AvailabilityWindowId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArrivesAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DepartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    BasePrice = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Fees = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Taxes = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CancellationPolicySnapshot = table.Column<string>(type: "jsonb", nullable: true),
                    PaymentIntentId = table.Column<string>(type: "text", nullable: true),
                    PaymentStatus = table.Column<string>(type: "text", nullable: false),
                    PlatformFeeAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeclinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    revenue_split_snapshot = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reservations_availability_windows_AvailabilityWindowId",
                        column: x => x.AvailabilityWindowId,
                        principalSchema: "mymarina",
                        principalTable: "availability_windows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reservations_slips_SlipId",
                        column: x => x.SlipId,
                        principalSchema: "mymarina",
                        principalTable: "slips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reservations_vessels_VesselId",
                        column: x => x.VesselId,
                        principalSchema: "mymarina",
                        principalTable: "vessels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_reservations_AvailabilityWindowId",
                schema: "mymarina",
                table: "reservations",
                column: "AvailabilityWindowId");

            migrationBuilder.CreateIndex(
                name: "IX_reservations_BoaterUserId",
                schema: "mymarina",
                table: "reservations",
                column: "BoaterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_reservations_SlipId_ArrivesAt",
                schema: "mymarina",
                table: "reservations",
                columns: new[] { "SlipId", "ArrivesAt" });

            migrationBuilder.CreateIndex(
                name: "IX_reservations_Status",
                schema: "mymarina",
                table: "reservations",
                column: "Status",
                filter: "status IN ('PendingApproval','PendingHostMarinaApproval','Confirmed')");

            migrationBuilder.CreateIndex(
                name: "IX_reservations_VesselId",
                schema: "mymarina",
                table: "reservations",
                column: "VesselId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reservations",
                schema: "mymarina");
        }
    }
}
