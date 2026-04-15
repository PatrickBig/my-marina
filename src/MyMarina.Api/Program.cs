using System.Text;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using MyMarina.Infrastructure.Jobs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// --- Controllers + OpenAPI ---
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// --- Infrastructure (EF Core, Identity, Redis, Hangfire, multi-tenancy) ---
builder.Services.AddInfrastructure(builder.Configuration);

// --- JWT Bearer authentication ---
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is required.");

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// --- SignalR ---
builder.Services.AddSignalR();

// --- Health checks ---
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")!)
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!);

// --- CORS (dev: allow Vite dev server; prod: tighten via config) ---
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

// --- Dev seed ---
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var db          = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // 1. Platform operator
    const string adminEmail = "admin@mymarina.org";
    if (await userManager.FindByEmailAsync(adminEmail) is null)
    {
        var admin = new ApplicationUser
        {
            UserName  = adminEmail,
            Email     = adminEmail,
            FirstName = "Platform",
            LastName  = "Admin",
            Role      = UserRole.PlatformOperator,
        };
        var r = await userManager.CreateAsync(admin, "Admin@Marina123!");
        if (!r.Succeeded)
            throw new InvalidOperationException(
                $"Dev seed (platform operator) failed: {string.Join(", ", r.Errors.Select(e => e.Description))}");
    }

    // 2. Demo marina tenant + owner (lets you log in and test the full operator workflow)
    const string ownerEmail = "owner@demo-marina.com";
    if (await userManager.FindByEmailAsync(ownerEmail) is null)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Slug == "demo-marina");
        if (tenant is null)
        {
            tenant = new Tenant { Name = "Demo Marina", Slug = "demo-marina", SubscriptionTier = SubscriptionTier.Free };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }

        var owner = new ApplicationUser
        {
            UserName  = ownerEmail,
            Email     = ownerEmail,
            FirstName = "Demo",
            LastName  = "Owner",
            Role      = UserRole.MarinaOwner,
            TenantId  = tenant.Id,
        };
        var r = await userManager.CreateAsync(owner, "Owner@Marina123!");
        if (!r.Succeeded)
            throw new InvalidOperationException(
                $"Dev seed (marina owner) failed: {string.Join(", ", r.Errors.Select(e => e.Description))}");
    }
}

// --- Middleware pipeline ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "MyMarina API";
        options.AddHttpAuthentication("Bearer", scheme => { });
    });
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/ready");

// --- Hangfire dashboard (platform operators only) ---
app.UseHangfireDashboard("/jobs", new DashboardOptions
{
    Authorization = [new MyMarina.Api.Infrastructure.HangfireAuthFilter()]
});

// --- Recurring jobs ---
RecurringJob.AddOrUpdate<MarkOverdueInvoicesJob>(
    "mark-overdue-invoices",
    job => job.ExecuteAsync(),
    Cron.Daily);

app.Run();

// Needed for WebApplicationFactory in integration tests
public partial class Program;
