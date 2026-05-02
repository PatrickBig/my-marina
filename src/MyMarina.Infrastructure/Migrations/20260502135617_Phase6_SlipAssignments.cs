using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMarina.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase6_SlipAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "slip_assignments",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SlipId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    VesselId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentType = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    BaseRate = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    AllowOwnerSubletWhenAway = table.Column<bool>(type: "boolean", nullable: false),
                    AllowHolderSublet = table.Column<bool>(type: "boolean", nullable: false),
                    OwnerSubletShareToHolder = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    HolderSubletShareToOwner = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_slip_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_slip_assignments_billing_accounts_BillingAccountId",
                        column: x => x.BillingAccountId,
                        principalSchema: "mymarina",
                        principalTable: "billing_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_slip_assignments_slips_SlipId",
                        column: x => x.SlipId,
                        principalSchema: "mymarina",
                        principalTable: "slips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_slip_assignments_vessels_VesselId",
                        column: x => x.VesselId,
                        principalSchema: "mymarina",
                        principalTable: "vessels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_slip_assignments_BillingAccountId",
                schema: "mymarina",
                table: "slip_assignments",
                column: "BillingAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_slip_assignments_SlipId",
                schema: "mymarina",
                table: "slip_assignments",
                column: "SlipId");

            migrationBuilder.CreateIndex(
                name: "IX_slip_assignments_SlipId_StartDate",
                schema: "mymarina",
                table: "slip_assignments",
                columns: new[] { "SlipId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_slip_assignments_VesselId",
                schema: "mymarina",
                table: "slip_assignments",
                column: "VesselId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "slip_assignments",
                schema: "mymarina");
        }
    }
}
