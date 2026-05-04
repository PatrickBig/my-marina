using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMarina.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase15_LeaseInquiries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DefaultLeaseBaseRate",
                schema: "mymarina",
                table: "slips",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultLeaseRateKind",
                schema: "mymarina",
                table: "slips",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultLeaseTerm",
                schema: "mymarina",
                table: "slips",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultTransientBaseRate",
                schema: "mymarina",
                table: "slips",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultTransientMinCharge",
                schema: "mymarina",
                table: "slips",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultTransientRateKind",
                schema: "mymarina",
                table: "slips",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OnboardingConfig",
                schema: "mymarina",
                table: "marinas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeaseTerm",
                schema: "mymarina",
                table: "availability_windows",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ListingKind",
                schema: "mymarina",
                table: "availability_windows",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "MinCharge",
                schema: "mymarina",
                table: "availability_windows",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RateKind",
                schema: "mymarina",
                table: "availability_windows",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "slip_lease_inquiries",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SlipId = table.Column<Guid>(type: "uuid", nullable: false),
                    MarinaId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestingUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VesselId = table.Column<Guid>(type: "uuid", nullable: true),
                    DesiredTerm = table.Column<string>(type: "text", nullable: false),
                    DesiredStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AgreedRateKind = table.Column<string>(type: "text", nullable: true),
                    AgreedBaseRate = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    AssignmentStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AssignmentEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    MarinaNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeclinedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeclinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SlipAssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    BillingAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_slip_lease_inquiries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_slip_lease_inquiries_slips_SlipId",
                        column: x => x.SlipId,
                        principalSchema: "mymarina",
                        principalTable: "slips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_slip_lease_inquiries_MarinaId",
                schema: "mymarina",
                table: "slip_lease_inquiries",
                column: "MarinaId");

            migrationBuilder.CreateIndex(
                name: "IX_slip_lease_inquiries_RequestingUserId",
                schema: "mymarina",
                table: "slip_lease_inquiries",
                column: "RequestingUserId");

            migrationBuilder.CreateIndex(
                name: "IX_slip_lease_inquiries_SlipId_Status",
                schema: "mymarina",
                table: "slip_lease_inquiries",
                columns: new[] { "SlipId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "slip_lease_inquiries",
                schema: "mymarina");

            migrationBuilder.DropColumn(
                name: "DefaultLeaseBaseRate",
                schema: "mymarina",
                table: "slips");

            migrationBuilder.DropColumn(
                name: "DefaultLeaseRateKind",
                schema: "mymarina",
                table: "slips");

            migrationBuilder.DropColumn(
                name: "DefaultLeaseTerm",
                schema: "mymarina",
                table: "slips");

            migrationBuilder.DropColumn(
                name: "DefaultTransientBaseRate",
                schema: "mymarina",
                table: "slips");

            migrationBuilder.DropColumn(
                name: "DefaultTransientMinCharge",
                schema: "mymarina",
                table: "slips");

            migrationBuilder.DropColumn(
                name: "DefaultTransientRateKind",
                schema: "mymarina",
                table: "slips");

            migrationBuilder.DropColumn(
                name: "OnboardingConfig",
                schema: "mymarina",
                table: "marinas");

            migrationBuilder.DropColumn(
                name: "LeaseTerm",
                schema: "mymarina",
                table: "availability_windows");

            migrationBuilder.DropColumn(
                name: "ListingKind",
                schema: "mymarina",
                table: "availability_windows");

            migrationBuilder.DropColumn(
                name: "MinCharge",
                schema: "mymarina",
                table: "availability_windows");

            migrationBuilder.DropColumn(
                name: "RateKind",
                schema: "mymarina",
                table: "availability_windows");
        }
    }
}
