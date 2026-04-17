using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MyMarina.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AuthorizationRoles",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("00000001-0000-0000-0000-000000000001"), "System administrator with cross-tenant access", "PlatformAdmin" },
                    { new Guid("00000002-0000-0000-0000-000000000001"), "Owns a tenant and sees all marinas within it", "TenantOwner" },
                    { new Guid("00000003-0000-0000-0000-000000000001"), "Manages a specific marina", "MarinaManager" },
                    { new Guid("00000004-0000-0000-0000-000000000001"), "Staff member at a specific marina", "MarinaStaff" },
                    { new Guid("00000005-0000-0000-0000-000000000001"), "Boat owner with portal access", "Customer" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AuthorizationRoles",
                keyColumn: "Id",
                keyValue: new Guid("00000001-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "AuthorizationRoles",
                keyColumn: "Id",
                keyValue: new Guid("00000002-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "AuthorizationRoles",
                keyColumn: "Id",
                keyValue: new Guid("00000003-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "AuthorizationRoles",
                keyColumn: "Id",
                keyValue: new Guid("00000004-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "AuthorizationRoles",
                keyColumn: "Id",
                keyValue: new Guid("00000005-0000-0000-0000-000000000001"));
        }
    }
}
