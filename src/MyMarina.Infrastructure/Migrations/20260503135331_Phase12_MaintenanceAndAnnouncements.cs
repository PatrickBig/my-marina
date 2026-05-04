using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMarina.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase12_MaintenanceAndAnnouncements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "announcements",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarinaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    Audience = table.Column<string>(type: "text", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_announcements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_announcements_marinas_MarinaId",
                        column: x => x.MarinaId,
                        principalSchema: "mymarina",
                        principalTable: "marinas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "maintenance_requests",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarinaId = table.Column<Guid>(type: "uuid", nullable: false),
                    BoaterUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    VesselId = table.Column<Guid>(type: "uuid", nullable: true),
                    SlipId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_maintenance_requests_marinas_MarinaId",
                        column: x => x.MarinaId,
                        principalSchema: "mymarina",
                        principalTable: "marinas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_orders",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarinaId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaintenanceRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_work_orders_maintenance_requests_MaintenanceRequestId",
                        column: x => x.MaintenanceRequestId,
                        principalSchema: "mymarina",
                        principalTable: "maintenance_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_work_orders_marinas_MarinaId",
                        column: x => x.MarinaId,
                        principalSchema: "mymarina",
                        principalTable: "marinas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_announcements_MarinaId",
                schema: "mymarina",
                table: "announcements",
                column: "MarinaId");

            migrationBuilder.CreateIndex(
                name: "IX_announcements_MarinaId_PublishedAt",
                schema: "mymarina",
                table: "announcements",
                columns: new[] { "MarinaId", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_requests_BoaterUserId",
                schema: "mymarina",
                table: "maintenance_requests",
                column: "BoaterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_requests_MarinaId",
                schema: "mymarina",
                table: "maintenance_requests",
                column: "MarinaId");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_requests_MarinaId_Status",
                schema: "mymarina",
                table: "maintenance_requests",
                columns: new[] { "MarinaId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_MaintenanceRequestId",
                schema: "mymarina",
                table: "work_orders",
                column: "MaintenanceRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_MarinaId",
                schema: "mymarina",
                table: "work_orders",
                column: "MarinaId");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_MarinaId_Status",
                schema: "mymarina",
                table: "work_orders",
                columns: new[] { "MarinaId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "announcements",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "work_orders",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "maintenance_requests",
                schema: "mymarina");
        }
    }
}
