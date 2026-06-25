using System.Text.Json.Serialization;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using ReservationSystem.Api.Authorization;
using ReservationSystem.Api.Extensions;
using ReservationSystem.Api.Middleware;
using ReservationSystem.Api.OpenApi;
using ReservationSystem.Application;
using ReservationSystem.Application.Common.Interfaces;
using ReservationSystem.Infrastructure;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Honour a PORT environment variable when present (e.g. Render, Heroku, Cloud Run
// inject it). No-op otherwise, so local/compose behaviour is unchanged.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Honour a platform-provided DATABASE_URL (postgresql://...) by translating it to
// the Npgsql connection string. No-op otherwise, so the existing
// ConnectionStrings:DefaultConnection path is unchanged for local dev and tests.
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrWhiteSpace(databaseUrl))
{
    builder.Configuration["ConnectionStrings:DefaultConnection"] =
        ConnectionStringConverter.FromDatabaseUrl(databaseUrl);
}

// --- Serilog: structured logging, configured from appsettings + code defaults ---
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// --- Application & Infrastructure services ---
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// --- Authentication & authorization (JWT bearer + role policies) ---
builder.Services.AddJwtAuthentication(builder.Configuration);

// --- Current user accessor (resolves identity from the request's claims) ---
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// --- MVC controllers + FluentValidation auto-validation ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddFluentValidationAutoValidation();

// --- Consistent error responses (RFC 7807 ProblemDetails) ---
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// --- OpenAPI document (with JWT Bearer scheme for Scalar) ---
builder.Services.AddOpenApi(options =>
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

// --- Health checks (PostgreSQL connectivity) ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var healthChecks = builder.Services.AddHealthChecks();
if (!string.IsNullOrWhiteSpace(connectionString))
{
    healthChecks.AddNpgSql(connectionString, name: "postgres");
}

var app = builder.Build();

// Optional, opt-in: apply EF Core migrations on startup (off by default).
await app.ApplyDatabaseMigrationsAsync();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

// --- API documentation: OpenAPI JSON + Scalar interactive UI (dev only) ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options
        .WithTitle("Reservation System API")
        .WithTheme(ScalarTheme.Purple));
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// --- Health endpoint ---
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
            }),
        });
    },
});

app.Run();

/// <summary>
/// Exposed so the integration test project can bootstrap the real application
/// with <c>WebApplicationFactory&lt;Program&gt;</c>. Behaviour is unchanged.
/// </summary>
public partial class Program;
