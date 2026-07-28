using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace App.Api.Security;

public static class ApiAuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddApiAuthenticationAndAuthorization(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var authority = configuration["Authentication:Authority"];
        var audience = configuration["Authentication:Audience"];

        if (!string.IsNullOrWhiteSpace(authority) && !string.IsNullOrWhiteSpace(audience))
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = authority;
                    options.Audience = audience;
                    options.RequireHttpsMetadata = !environment.IsDevelopment();
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ClockSkew = TimeSpan.FromMinutes(2)
                    };
                });
        }
        else
        {
            services.AddAuthentication(UnavailableAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, UnavailableAuthenticationHandler>(
                    UnavailableAuthenticationHandler.SchemeName,
                    _ => { });
        }

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.AuthenticatedUser, policy => policy.RequireAuthenticatedUser());
        });

        return services;
    }
}
