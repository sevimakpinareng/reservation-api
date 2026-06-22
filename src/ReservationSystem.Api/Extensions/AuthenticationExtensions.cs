using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ReservationSystem.Application.Authentication;
using ReservationSystem.Domain.Enums;

namespace ReservationSystem.Api.Extensions;

/// <summary>
/// Wires up JWT bearer authentication and role-based authorization policies.
/// All JWT settings (including the signing secret) are read from configuration;
/// the secret must be supplied via user-secrets or environment variables.
/// </summary>
public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        services.Configure<JwtOptions>(jwtSection);

        var jwt = jwtSection.Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration section 'Jwt' is missing.");

        if (string.IsNullOrWhiteSpace(jwt.Secret))
        {
            throw new InvalidOperationException(
                "JWT secret is not configured. Set 'Jwt:Secret' via user-secrets or the " +
                "Jwt__Secret environment variable — it must never be committed to the repo.");
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Keep claim types as emitted (e.g. "sub", "role") instead of remapping.
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                    RoleClaimType = "role",
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("Customer", policy => policy.RequireRole(nameof(UserRole.Customer)));
            options.AddPolicy("BusinessOwner", policy => policy.RequireRole(nameof(UserRole.BusinessOwner)));
            options.AddPolicy("Admin", policy => policy.RequireRole(nameof(UserRole.Admin)));
        });

        return services;
    }
}
