using System.Text;
using AspNet.Security.OAuth.Apple;
using Hangfire;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyMarina.Infrastructure;
using MyMarina.Infrastructure.Invoicing;
using MyMarina.Infrastructure.Storage;
using MyMarina.Infrastructure.Persistence;
using MyMarina.Infrastructure.Setup;
using Scalar.AspNetCore;

// ── Setup mode ──────────────────────────────────────────────────────────────
// Invoked as a one-shot pre-deploy job (Helm hook / docker-compose api-setup).
// Runs migrations, seeds required accounts, registers Hangfire schedules, exits.
if (args.Contains("--setup"))
{
    var setupBuilder = WebApplication.CreateBuilder(args);
    setupBuilder.Services.AddInfrastructure(setupBuilder.Configuration, registerHangfireServer: false);
    setupBuilder.Services.Configure<SetupOptions>(setupBuilder.Configuration.GetSection("Setup"));
    setupBuilder.Services.AddScoped<AppSetupRunner>();

    var setupApp = setupBuilder.Build();

    using (var scope = setupApp.Services.CreateScope())
    {
        await scope.ServiceProvider.GetRequiredService<AppSetupRunner>().RunAsync();
        RegisterRecurringJobs(scope.ServiceProvider.GetRequiredService<IRecurringJobManager>());
    }

    return;
}

// ── Normal startup ───────────────────────────────────────────────────────────
var builder = WebApplication.CreateBuilder(args);

// --- Controllers + OpenAPI ---
builder.Services.AddControllers(options =>
    options.Filters.AddService<MyMarina.Api.Infrastructure.DemoWriteBlockFilter>());
builder.Services.AddScoped<MyMarina.Api.Infrastructure.DemoWriteBlockFilter>();
builder.Services.AddOpenApi();

// --- Infrastructure (EF Core, Identity, Redis, Hangfire, user context) ---
builder.Services.AddInfrastructure(builder.Configuration);

// --- JWT Bearer authentication ---
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is required.");

var authBuilder = builder.Services.AddAuthentication(options =>
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
            NameClaimType = "sub",
        };

        if (builder.Environment.IsDevelopment())
        {
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = ctx =>
                {
                    ctx.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("JwtAuth")
                        .LogWarning("JWT auth failed for {Path}: {Error}",
                            ctx.HttpContext.Request.Path, ctx.Exception.Message);
                    return Task.CompletedTask;
                },
            };
        }
    });

// --- Social login providers (only registered when credentials are configured) ---
var googleClientId = builder.Configuration["Auth:Google:ClientId"];
if (!string.IsNullOrWhiteSpace(googleClientId))
{
    authBuilder.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.ClientId     = googleClientId;
        options.ClientSecret = builder.Configuration["Auth:Google:ClientSecret"]!;
        options.CallbackPath = "/signin-google";
    });
}

var facebookAppId = builder.Configuration["Auth:Facebook:AppId"];
if (!string.IsNullOrWhiteSpace(facebookAppId))
{
    authBuilder.AddFacebook(FacebookDefaults.AuthenticationScheme, options =>
    {
        options.AppId     = facebookAppId;
        options.AppSecret = builder.Configuration["Auth:Facebook:AppSecret"]!;
        options.CallbackPath = "/signin-facebook";
    });
}

var appleClientId = builder.Configuration["Auth:Apple:ClientId"];
if (!string.IsNullOrWhiteSpace(appleClientId))
{
    authBuilder.AddApple(AppleAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.ClientId  = appleClientId;
        options.KeyId     = builder.Configuration["Auth:Apple:KeyId"]!;
        options.TeamId    = builder.Configuration["Auth:Apple:TeamId"]!;
        options.CallbackPath = "/signin-apple";
    });
}

builder.Services.AddAuthorization();

// --- SignalR ---
builder.Services.AddSignalR();

// --- Health checks ---
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")!)
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!);

// --- CORS ---
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

// --- Dev: auto-migrate so `dotnet watch` works without running --setup ---
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
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

RegisterRecurringJobs(app.Services.GetRequiredService<IRecurringJobManager>());

app.Run();

// ── Shared helpers ───────────────────────────────────────────────────────────
static void RegisterRecurringJobs(IRecurringJobManager jobs)
{
    jobs.AddOrUpdate<InvoiceOverdueJob>(
        "invoice-overdue-check",
        job => job.CheckOverdueAsync(),
        Cron.Daily(2));
    jobs.AddOrUpdate<OrphanPhotoCleanupJob>(
        "orphan-photo-cleanup",
        "photos",
        job => job.ExecuteAsync(),
        Cron.Daily(3),
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
}

public partial class Program;
