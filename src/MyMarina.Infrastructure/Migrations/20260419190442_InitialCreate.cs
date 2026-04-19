using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MyMarina.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "mymarina");

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    MarinaId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Before = table.Column<string>(type: "jsonb", nullable: true),
                    After = table.Column<string>(type: "jsonb", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuthorizationRoles",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorizationRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerAccounts",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BillingEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BillingPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BillingStreet = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BillingCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BillingState = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BillingZip = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BillingCountry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EmergencyContactName = table.Column<string>(type: "text", nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    SubscriptionTier = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDemo = table.Column<bool>(type: "boolean", nullable: false),
                    DemoExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "mymarina",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "mymarina",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                schema: "mymarina",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "mymarina",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                schema: "mymarina",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "mymarina",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "mymarina",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                schema: "mymarina",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "mymarina",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserContexts",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MarinaId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserContexts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserContexts_AuthorizationRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "mymarina",
                        principalTable: "AuthorizationRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Boats",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Make = table.Column<string>(type: "text", nullable: true),
                    Model = table.Column<string>(type: "text", nullable: true),
                    Year = table.Column<int>(type: "integer", nullable: true),
                    Length = table.Column<decimal>(type: "numeric", nullable: false),
                    Beam = table.Column<decimal>(type: "numeric", nullable: false),
                    Draft = table.Column<decimal>(type: "numeric", nullable: false),
                    BoatType = table.Column<int>(type: "integer", nullable: false),
                    HullColor = table.Column<string>(type: "text", nullable: true),
                    RegistrationNumber = table.Column<string>(type: "text", nullable: true),
                    RegistrationState = table.Column<string>(type: "text", nullable: true),
                    InsuranceProvider = table.Column<string>(type: "text", nullable: true),
                    InsurancePolicyNumber = table.Column<string>(type: "text", nullable: true),
                    InsuranceExpiresOn = table.Column<DateOnly>(type: "date", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Boats_CustomerAccounts_CustomerAccountId",
                        column: x => x.CustomerAccountId,
                        principalSchema: "mymarina",
                        principalTable: "CustomerAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerAccountMembers",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAccountMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerAccountMembers_CustomerAccounts_CustomerAccountId",
                        column: x => x.CustomerAccountId,
                        principalSchema: "mymarina",
                        principalTable: "CustomerAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceRequests",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlipId = table.Column<Guid>(type: "uuid", nullable: true),
                    BoatId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceRequests_CustomerAccounts_CustomerAccountId",
                        column: x => x.CustomerAccountId,
                        principalSchema: "mymarina",
                        principalTable: "CustomerAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Marinas",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AddressStreet = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AddressCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AddressState = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AddressZip = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AddressCountry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    TimeZoneId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OccupancyWarningThreshold = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    OccupancyAlertThreshold = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    OverdueWarningDays = table.Column<int>(type: "integer", nullable: true),
                    OverdueAlertDays = table.Column<int>(type: "integer", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marinas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Marinas_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "mymarina",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrders",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaintenanceRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrders_MaintenanceRequests_MaintenanceRequestId",
                        column: x => x.MaintenanceRequestId,
                        principalSchema: "mymarina",
                        principalTable: "MaintenanceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Announcements",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarinaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Announcements_Marinas_MarinaId",
                        column: x => x.MarinaId,
                        principalSchema: "mymarina",
                        principalTable: "Marinas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Docks",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarinaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Docks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Docks_Marinas_MarinaId",
                        column: x => x.MarinaId,
                        principalSchema: "mymarina",
                        principalTable: "Marinas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarinaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IssuedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    AmountPaid = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_CustomerAccounts_CustomerAccountId",
                        column: x => x.CustomerAccountId,
                        principalSchema: "mymarina",
                        principalTable: "CustomerAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Invoices_Marinas_MarinaId",
                        column: x => x.MarinaId,
                        principalSchema: "mymarina",
                        principalTable: "Marinas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OperatingExpenses",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MarinaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    IncurredDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RelatedEntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatingExpenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperatingExpenses_Marinas_MarinaId",
                        column: x => x.MarinaId,
                        principalSchema: "mymarina",
                        principalTable: "Marinas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Slips",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarinaId = table.Column<Guid>(type: "uuid", nullable: false),
                    DockId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SlipType = table.Column<int>(type: "integer", nullable: false),
                    MaxLength = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    MaxBeam = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    MaxDraft = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    HasElectric = table.Column<bool>(type: "boolean", nullable: false),
                    Electric = table.Column<int>(type: "integer", nullable: true),
                    HasWater = table.Column<bool>(type: "boolean", nullable: false),
                    RateType = table.Column<int>(type: "integer", nullable: false),
                    DailyRate = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    MonthlyRate = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    AnnualRate = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(11,7)", precision: 11, scale: 7, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Slips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Slips_Docks_DockId",
                        column: x => x.DockId,
                        principalSchema: "mymarina",
                        principalTable: "Docks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Slips_Marinas_MarinaId",
                        column: x => x.MarinaId,
                        principalSchema: "mymarina",
                        principalTable: "Marinas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLineItems",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    SlipAssignmentId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceLineItems_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "mymarina",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    PaidOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    PaymentProviderId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PaymentProviderReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "mymarina",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlipAssignments",
                schema: "mymarina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SlipId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    BoatId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentType = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RateOverride = table.Column<decimal>(type: "numeric", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlipAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlipAssignments_Boats_BoatId",
                        column: x => x.BoatId,
                        principalSchema: "mymarina",
                        principalTable: "Boats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlipAssignments_CustomerAccounts_CustomerAccountId",
                        column: x => x.CustomerAccountId,
                        principalSchema: "mymarina",
                        principalTable: "CustomerAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlipAssignments_Slips_SlipId",
                        column: x => x.SlipId,
                        principalSchema: "mymarina",
                        principalTable: "Slips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "mymarina",
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

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_MarinaId",
                schema: "mymarina",
                table: "Announcements",
                column: "MarinaId");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_TenantId_MarinaId_PublishedAt",
                schema: "mymarina",
                table: "Announcements",
                columns: new[] { "TenantId", "MarinaId", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                schema: "mymarina",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "mymarina",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                schema: "mymarina",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                schema: "mymarina",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                schema: "mymarina",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "mymarina",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "mymarina",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityId",
                schema: "mymarina",
                table: "AuditLogs",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId",
                schema: "mymarina",
                table: "AuditLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                schema: "mymarina",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationRoles_Name",
                schema: "mymarina",
                table: "AuthorizationRoles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Boats_CustomerAccountId",
                schema: "mymarina",
                table: "Boats",
                column: "CustomerAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccountMembers_CustomerAccountId",
                schema: "mymarina",
                table: "CustomerAccountMembers",
                column: "CustomerAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Docks_MarinaId",
                schema: "mymarina",
                table: "Docks",
                column: "MarinaId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLineItems_InvoiceId",
                schema: "mymarina",
                table: "InvoiceLineItems",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CustomerAccountId",
                schema: "mymarina",
                table: "Invoices",
                column: "CustomerAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_MarinaId",
                schema: "mymarina",
                table: "Invoices",
                column: "MarinaId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_InvoiceNumber",
                schema: "mymarina",
                table: "Invoices",
                columns: new[] { "TenantId", "InvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_CustomerAccountId",
                schema: "mymarina",
                table: "MaintenanceRequests",
                column: "CustomerAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Marinas_TenantId",
                schema: "mymarina",
                table: "Marinas",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OperatingExpenses_MarinaId",
                schema: "mymarina",
                table: "OperatingExpenses",
                column: "MarinaId");

            migrationBuilder.CreateIndex(
                name: "IX_OperatingExpenses_TenantId_MarinaId_IncurredDate",
                schema: "mymarina",
                table: "OperatingExpenses",
                columns: new[] { "TenantId", "MarinaId", "IncurredDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_InvoiceId",
                schema: "mymarina",
                table: "Payments",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Name",
                schema: "mymarina",
                table: "Permissions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlipAssignments_BoatId",
                schema: "mymarina",
                table: "SlipAssignments",
                column: "BoatId");

            migrationBuilder.CreateIndex(
                name: "IX_SlipAssignments_CustomerAccountId",
                schema: "mymarina",
                table: "SlipAssignments",
                column: "CustomerAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SlipAssignments_SlipId",
                schema: "mymarina",
                table: "SlipAssignments",
                column: "SlipId");

            migrationBuilder.CreateIndex(
                name: "IX_Slips_DockId",
                schema: "mymarina",
                table: "Slips",
                column: "DockId");

            migrationBuilder.CreateIndex(
                name: "IX_Slips_MarinaId",
                schema: "mymarina",
                table: "Slips",
                column: "MarinaId");

            migrationBuilder.CreateIndex(
                name: "IX_UserContexts_RoleId",
                schema: "mymarina",
                table: "UserContexts",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserContexts_UserId_TenantId_MarinaId",
                schema: "mymarina",
                table: "UserContexts",
                columns: new[] { "UserId", "TenantId", "MarinaId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_MaintenanceRequestId",
                schema: "mymarina",
                table: "WorkOrders",
                column: "MaintenanceRequestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Announcements",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "CustomerAccountMembers",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "InvoiceLineItems",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "OperatingExpenses",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "Payments",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "Permissions",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "SlipAssignments",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "UserContexts",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "WorkOrders",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "AspNetRoles",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "AspNetUsers",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "Invoices",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "Boats",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "Slips",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "AuthorizationRoles",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "MaintenanceRequests",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "Docks",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "CustomerAccounts",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "Marinas",
                schema: "mymarina");

            migrationBuilder.DropTable(
                name: "Tenants",
                schema: "mymarina");
        }
    }
}
