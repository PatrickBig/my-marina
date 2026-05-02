using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMarina.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase5_CustomersAndGhostVessels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "billing_accounts",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarinaId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BillingEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    BillingPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BillingAddressStreet = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BillingAddressCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BillingAddressState = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BillingAddressZip = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BillingAddressCountry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EmergencyContactName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_billing_accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_billing_accounts_marinas_MarinaId",
                        column: x => x.MarinaId,
                        principalSchema: "mymarina",
                        principalTable: "marinas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "billing_account_members",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    InvitedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    InvitedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_billing_account_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_billing_account_members_billing_accounts_BillingAccountId",
                        column: x => x.BillingAccountId,
                        principalSchema: "mymarina",
                        principalTable: "billing_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "marina_vessel_records",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarinaId = table.Column<Guid>(type: "uuid", nullable: false),
                    VesselId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    InsuranceProvider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InsurancePolicyNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    InsuranceExpiresOn = table.Column<DateOnly>(type: "date", nullable: true),
                    InsuranceVerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    InsuranceVerifiedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marina_vessel_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_marina_vessel_records_billing_accounts_BillingAccountId",
                        column: x => x.BillingAccountId,
                        principalSchema: "mymarina",
                        principalTable: "billing_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_marina_vessel_records_marinas_MarinaId",
                        column: x => x.MarinaId,
                        principalSchema: "mymarina",
                        principalTable: "marinas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_marina_vessel_records_vessels_VesselId",
                        column: x => x.VesselId,
                        principalSchema: "mymarina",
                        principalTable: "vessels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_billing_account_members_BillingAccountId_UserId",
                schema: "mymarina",
                table: "billing_account_members",
                columns: new[] { "BillingAccountId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_billing_account_members_UserId",
                schema: "mymarina",
                table: "billing_account_members",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_billing_accounts_MarinaId",
                schema: "mymarina",
                table: "billing_accounts",
                column: "MarinaId");

            migrationBuilder.CreateIndex(
                name: "IX_billing_accounts_MarinaId_BillingEmail",
                schema: "mymarina",
                table: "billing_accounts",
                columns: new[] { "MarinaId", "BillingEmail" });

            migrationBuilder.CreateIndex(
                name: "IX_marina_vessel_records_BillingAccountId",
                schema: "mymarina",
                table: "marina_vessel_records",
                column: "BillingAccountId",
                filter: "billing_account_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_marina_vessel_records_MarinaId_VesselId",
                schema: "mymarina",
                table: "marina_vessel_records",
                columns: new[] { "MarinaId", "VesselId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_marina_vessel_records_VesselId",
                schema: "mymarina",
                table: "marina_vessel_records",
                column: "VesselId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "billing_account_members",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "marina_vessel_records",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "billing_accounts",
                schema: "mymarina");
        }
    }
}
