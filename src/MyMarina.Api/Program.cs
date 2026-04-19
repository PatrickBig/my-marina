using System.Text;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using MyMarina.Infrastructure.Demo;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyMarina.Domain.Entities;
using MyMarina.Infrastructure;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;
using MyMarina.Infrastructure.Setup;
using Scalar.AspNetCore;

// --setup mode: apply migrations + seed users, then exit.
// Designed to run as a Kubernetes pre-install/pre-upgrade Job or Helm hook.
// Config comes from the "Setup" config section; supply secrets via env vars:
//   Setup__PlatformOperator__Email, Setup__PlatformOperator__Password
//   Setup__InitialMarina__TenantName, Setup__InitialMarina__TenantSlug,
//   Setup__InitialMarina__OwnerEmail, Setup__InitialMarina__OwnerPassword
var isSetupMode = args.Contains("--setup");

var builder = WebApplication.CreateBuilder(args);

// --- Controllers + OpenAPI ---
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// --- Infrastructure (EF Core, Identity, Redis, Hangfire, multi-tenancy) ---
// In setup mode the Hangfire server is skipped so no Redis connection is required.
builder.Services.AddInfrastructure(builder.Configuration, registerHangfireServer: !isSetupMode);

// --- Setup mode: bind options and register the hosted service ---
if (isSetupMode)
{
    builder.Services.Configure<SetupOptions>(
        builder.Configuration.GetSection(SetupOptions.Section));
    builder.Services.AddHostedService<SetupHostedService>();
}

// --- Demo options ---
builder.Services.Configure<DemoOptions>(builder.Configuration.GetSection(DemoOptions.Section));

// --- Rate limiting (for demo session endpoint) ---
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("demo-session", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromHours(1);
        limiterOptions.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

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
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = "role",
            NameClaimType = "sub",
        };

        if (builder.Environment.IsDevelopment())
        {
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = ctx =>
                {
                    var log = ctx.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("JwtAuth");
                    log.LogWarning("JWT authentication failed for {Path}: {Error}",
                        ctx.HttpContext.Request.Path, ctx.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = ctx =>
                {
                    var log = ctx.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("JwtAuth");
                    if (log.IsEnabled(LogLevel.Debug))
                    {
                        var claims = ctx.Principal?.Claims.Select(c => $"{c.Type}={c.Value}");
                        log.LogDebug("JWT validated for {Path}. Claims: {Claims}",
                            ctx.HttpContext.Request.Path, string.Join(", ", claims ?? []));
                    }
                    return Task.CompletedTask;
                },
                OnForbidden = ctx =>
                {
                    var log = ctx.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("JwtAuth");
                    var role = ctx.HttpContext.User.FindFirst("role")?.Value ?? "(none)";
                    log.LogWarning("403 Forbidden for {Path}. role claim={Role}",
                        ctx.HttpContext.Request.Path, role);
                    return Task.CompletedTask;
                },
            };
        }
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

// --- Dev seed (test users with multi-context scenarios) ---
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var seedService = scope.ServiceProvider.GetRequiredService<SeedDataService>();
    await seedService.SeedAsync();
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

//app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
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

// Recurring jobs are registered by the --setup pass on each deployment,
// not at application startup. Hangfire stores them in its backend (Redis/Postgres)
// so they survive restarts without re-registration.

app.Run();

// Needed for WebApplicationFactory in integration tests
public partial class Program;
