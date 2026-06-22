using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using ReservationSystem.Infrastructure;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- Serilog: structured logging, configured from appsettings + code defaults ---
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// --- Application & Infrastructure services ---
builder.Services.AddInfrastructure(builder.Configuration);

// Serialize enums as strings in API responses.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// --- OpenAPI document (served at /openapi/v1.json) ---
builder.Services.AddOpenApi();

// --- Health checks (PostgreSQL connectivity) ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var healthChecks = builder.Services.AddHealthChecks();
if (!string.IsNullOrWhiteSpace(connectionString))
{
    healthChecks.AddNpgSql(connectionString, name: "postgres");
}

var app = builder.Build();

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
