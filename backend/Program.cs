using System.Security.Cryptography;
using System.Text;
using HelpDesk.Api.Data;
using HelpDesk.Api.Hubs;
using HelpDesk.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var provider = builder.Configuration["DatabaseProvider"]?.ToLowerInvariant();
    var conn = builder.Configuration.GetConnectionString("DefaultConnection");
    if (provider == "postgresql")
    {
        options.UseNpgsql(conn);
    }
    else
    {
        options.UseSqlite(conn);
    }
});

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReportExportService, ReportExportService>();
builder.Services.AddScoped<IUploadValidationService, UploadValidationService>();
builder.Services.AddHttpClient<IAiService, AiService>();
builder.Services.AddSignalR();
builder.Services.AddHealthChecks();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("login", limiter =>
    {
        limiter.PermitLimit = 8;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
});

var jwtSection = builder.Configuration.GetSection("Jwt");
var secret = jwtSection["Secret"];
var hasInvalidSecret = string.IsNullOrWhiteSpace(secret) ||
    secret.Length < 32 ||
    secret.StartsWith("REPLACE_WITH_", StringComparison.OrdinalIgnoreCase);
if (hasInvalidSecret && appEnvironmentIsDevelopment(builder.Environment))
{
    throw new InvalidOperationException("Jwt:Secret must be configured with at least 32 characters.");
}

if (hasInvalidSecret)
{
    Console.Error.WriteLine("WARNING: Jwt:Secret is missing or placeholder. Using an ephemeral startup secret. Configure a stable Jwt__Secret for production.");
    secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
}

var signingSecret = secret ?? throw new InvalidOperationException("Jwt:Secret could not be resolved.");
var key = new SymmetricSecurityKey(SHA256.HashData(Encoding.UTF8.GetBytes(signingSecret)));

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
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = key,
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/notifications"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("AgentOrAdmin", policy => policy.RequireRole("Agent", "Admin"));
});

builder.Services.AddCors(options =>
{
    var configuredOrigins = builder.Configuration["Cors:AllowedOrigins"];
    var allowedOrigins = (configuredOrigins ?? string.Empty)
        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    if (allowedOrigins.Length == 0)
    {
        allowedOrigins =
        [
            "http://localhost:5173",
            "http://127.0.0.1:5173",
            "http://localhost:5174",
            "http://127.0.0.1:5174"
        ];
    }

    options.AddPolicy("frontend", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        app.Logger.LogError(exception, "Unhandled API exception for {Path}", context.Request.Path);
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await Results.Problem(
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Unexpected server error",
            detail: app.Environment.IsDevelopment() ? exception?.Message : "The request could not be completed.")
            .ExecuteAsync(context);
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.MapOpenApi();
}
app.UseCors("frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/healthz", async (AppDbContext db, CancellationToken cancellationToken) =>
{
    var databaseOnline = await db.Database.CanConnectAsync(cancellationToken);
    return Results.Ok(new
    {
        status = databaseOnline ? "ok" : "degraded",
        database = databaseOnline ? "ok" : "unavailable",
        timestampUtc = DateTime.UtcNow
    });
}).AllowAnonymous();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    if ((app.Configuration["DatabaseProvider"] ?? "Sqlite").Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        await SchemaInitializer.EnsureAnalyticsSchemaAsync(db);
    }

    await DbSeeder.SeedAsync(db, app.Configuration, app.Logger);
}

app.Run();

static bool appEnvironmentIsDevelopment(IHostEnvironment environment) => environment.IsDevelopment();
