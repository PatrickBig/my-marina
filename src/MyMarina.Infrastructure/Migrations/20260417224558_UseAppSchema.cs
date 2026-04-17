using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMarina.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UseAppSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "mymarina");

            migrationBuilder.RenameTable(
                name: "WorkOrders",
                newName: "WorkOrders",
                newSchema: "mymarina");

            migrationBuilder.RenameTable(
                name: "UserContexts",
                newName: "UserContexts",
                newSchema: "mymarina");

            migrationBuilder.RenameTable(
                name: "Tenants",
                newName: "Tenants",
                newSchema: "mymarina");

            migrationBuilder.RenameTable(
                name: "Slips",
                newName: "Slips",
                newSchema: "mymarina");

            migrationBuilder.RenameTable(
                name: "SlipAssignments",
                newName: "SlipAssignments",
                newSchema: "mymarina");

            migrationBuilder.RenameTable(
                name: "Permissions",
                newName: "Permissions",
                newSchema: "mymarina");

            migrationBuilder.RenameTable(
                name: "Payments",
                newName: "Payments",
                newSchema: "mymarina");

            migrationBuilder.RenameTable(
                name: "OperatingExpenses",
                newName: "OperatingExpenses",
                newSchema: "mymarina");

            migrationBuilder.RenameTable(
                name: "Marinas",
                newName: "Marinas",
                newSchema: "mymarina");

            migrationBuilder.RenameTable(
                name: "MaintenanceRequests",
                newName: "MaintenanceRequests",
                newSchema: "mymarina");

            migrationBuilder.RenameTable(
                name: "Invoices",
                newName: "Invoices",
                newSchema: "mymarina");

            migrationBuilder.RenameTable(
                name: "InvoiceLineItems",
                newName: "InvoiceLineItems",
                newSchema: "mymarina");

            migrationBuilder.RenameTable(
                name: "Docks",
                newName: "Docks",
                newSchema: "mymarina");

            migrationBuilder.RenameTable(
                name: "CustomerAccounts",
                newName: "CustomerAccounts",
                newSchema: "mymarina");

            migrationBuilder.RenameTable(
                name: "CustomerAccountMembers",
                newName: "CustomerAccountMembers",
                newSchema: "mymarina");

            migrationBuilder.RenameTable(
                name: "Boats",
                newName: "Boats",
                newSchema: "mymarina");

            migrationBuilder.RenameTable(
                name: "AuthorizationRoles",
                newName: "AuthorizationRoles",
                newSchema: "mymarina");

            migrationBuilder.RenameTable(
                name: "AuditLogs",
                newName: "AuditLogs",
                newSchema: "mymarina");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                newName: "AspNetUserTokens",
                newSchema: "mymarina");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                newName: "AspNetUsers",
                newSchema: "mymarina");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                newName: "AspNetUserRoles",
                newSchema: "mymarina");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                newName: "AspNetUserLogins",
                newSchema: "mymarina");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                newName: "AspNetUserClaims",
                newSchema: "mymarina");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                newName: "AspNetRoles",
                newSchema: "mymarina");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                newName: "AspNetRoleClaims",
                newSchema: "mymarina");

            migrationBuilder.RenameTable(
                name: "Announcements",
                newName: "Announcements",
                newSchema: "mymarina");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "WorkOrders",
                schema: "mymarina",
                newName: "WorkOrders");

            migrationBuilder.RenameTable(
                name: "UserContexts",
                schema: "mymarina",
                newName: "UserContexts");

            migrationBuilder.RenameTable(
                name: "Tenants",
                schema: "mymarina",
                newName: "Tenants");

            migrationBuilder.RenameTable(
                name: "Slips",
                schema: "mymarina",
                newName: "Slips");

            migrationBuilder.RenameTable(
                name: "SlipAssignments",
                schema: "mymarina",
                newName: "SlipAssignments");

            migrationBuilder.RenameTable(
                name: "Permissions",
                schema: "mymarina",
                newName: "Permissions");

            migrationBuilder.RenameTable(
                name: "Payments",
                schema: "mymarina",
                newName: "Payments");

            migrationBuilder.RenameTable(
                name: "OperatingExpenses",
                schema: "mymarina",
                newName: "OperatingExpenses");

            migrationBuilder.RenameTable(
                name: "Marinas",
                schema: "mymarina",
                newName: "Marinas");

            migrationBuilder.RenameTable(
                name: "MaintenanceRequests",
                schema: "mymarina",
                newName: "MaintenanceRequests");

            migrationBuilder.RenameTable(
                name: "Invoices",
                schema: "mymarina",
                newName: "Invoices");

            migrationBuilder.RenameTable(
                name: "InvoiceLineItems",
                schema: "mymarina",
                newName: "InvoiceLineItems");

            migrationBuilder.RenameTable(
                name: "Docks",
                schema: "mymarina",
                newName: "Docks");

            migrationBuilder.RenameTable(
                name: "CustomerAccounts",
                schema: "mymarina",
                newName: "CustomerAccounts");

            migrationBuilder.RenameTable(
                name: "CustomerAccountMembers",
                schema: "mymarina",
                newName: "CustomerAccountMembers");

            migrationBuilder.RenameTable(
                name: "Boats",
                schema: "mymarina",
                newName: "Boats");

            migrationBuilder.RenameTable(
                name: "AuthorizationRoles",
                schema: "mymarina",
                newName: "AuthorizationRoles");

            migrationBuilder.RenameTable(
                name: "AuditLogs",
                schema: "mymarina",
                newName: "AuditLogs");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                schema: "mymarina",
                newName: "AspNetUserTokens");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                schema: "mymarina",
                newName: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                schema: "mymarina",
                newName: "AspNetUserRoles");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                schema: "mymarina",
                newName: "AspNetUserLogins");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                schema: "mymarina",
                newName: "AspNetUserClaims");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                schema: "mymarina",
                newName: "AspNetRoles");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                schema: "mymarina",
                newName: "AspNetRoleClaims");

            migrationBuilder.RenameTable(
                name: "Announcements",
                schema: "mymarina",
                newName: "Announcements");
        }
    }
}
