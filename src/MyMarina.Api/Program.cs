using System.Text;
using AspNet.Security.OAuth.Apple;
using Hangfire;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyMarina.Infrastructure;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// --- Controllers + OpenAPI ---
builder.Services.AddControllers();
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
        // options.GenerateClientSecret — wire up private key from config in production
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

// --- Dev: apply migrations and seed platform operator ---
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

    // Ensure PlatformOperator role exists
    if (!await roleManager.RoleExistsAsync("PlatformOperator"))
        await roleManager.CreateAsync(new ApplicationRole("PlatformOperator"));

    // Seed platform operator
    const string adminEmail = "admin@mymarina.org";
    const string adminPassword = "Admin@Marina123!";
    var admin = await userManager.FindByEmailAsync(adminEmail);
    if (admin is null)
    {
        admin = new ApplicationUser
        {
            Id        = Guid.CreateVersion7(),
            UserName  = adminEmail,
            Email     = adminEmail,
            FirstName = "Platform",
            LastName  = "Admin",
            EmailConfirmed = true,
        };
        var result = await userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "PlatformOperator");
    }

    // Seed demo marina owner
    const string ownerEmail = "owner@demo-marina.com";
    const string ownerPassword = "Owner@Marina123!";
    if (await userManager.FindByEmailAsync(ownerEmail) is null)
    {
        var owner = new ApplicationUser
        {
            Id        = Guid.CreateVersion7(),
            UserName  = ownerEmail,
            Email     = ownerEmail,
            FirstName = "Demo",
            LastName  = "Owner",
            EmailConfirmed = true,
        };
        await userManager.CreateAsync(owner, ownerPassword);
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

app.Run();

public partial class Program;
