using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMarina.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase18_MarinaPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "marina_photos",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MarinaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UrlFull = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UrlMedium = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UrlThumbnail = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    Caption = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marina_photos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_marina_photos_marinas_MarinaId",
                        column: x => x.MarinaId,
                        principalSchema: "mymarina",
                        principalTable: "marinas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_marina_photos_marina_logo_banner_unique",
                schema: "mymarina",
                table: "marina_photos",
                columns: new[] { "MarinaId", "Kind" },
                unique: true,
                filter: "\"Kind\" IN (0, 1)");

            migrationBuilder.CreateIndex(
                name: "IX_marina_photos_MarinaId",
                schema: "mymarina",
                table: "marina_photos",
                column: "MarinaId");

            migrationBuilder.CreateIndex(
                name: "IX_marina_photos_MarinaId_Kind_SortOrder",
                schema: "mymarina",
                table: "marina_photos",
                columns: new[] { "MarinaId", "Kind", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "marina_photos",
                schema: "mymarina");
        }
    }
}
