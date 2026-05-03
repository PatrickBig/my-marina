using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMarina.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase10_SubletFlows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "owner_absences",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SlipAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlipId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_owner_absences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_owner_absences_slip_assignments_SlipAssignmentId",
                        column: x => x.SlipAssignmentId,
                        principalSchema: "mymarina",
                        principalTable: "slip_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_owner_absences_SlipAssignmentId",
                schema: "mymarina",
                table: "owner_absences",
                column: "SlipAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_owner_absences_SlipId",
                schema: "mymarina",
                table: "owner_absences",
                column: "SlipId");

            migrationBuilder.CreateIndex(
                name: "IX_owner_absences_SlipId_StartsOn_EndsOn",
                schema: "mymarina",
                table: "owner_absences",
                columns: new[] { "SlipId", "StartsOn", "EndsOn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "owner_absences",
                schema: "mymarina");
        }
    }
}
