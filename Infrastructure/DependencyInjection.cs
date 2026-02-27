namespace BackBase.Infrastructure;

using BackBase.Application.Constants;
using BackBase.Application.Interfaces;
using BackBase.Domain.Interfaces;
using BackBase.Infrastructure.Authentication;
using BackBase.Infrastructure.Authorization;
using BackBase.Infrastructure.Chat;
using BackBase.Infrastructure.Data;
using BackBase.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static async Task InitializeInfrastructureAsync(this IServiceProvider serviceProvider)
    {
        await serviceProvider.SeedRolesAsync().ConfigureAwait(false);
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = jwtSettings.CreateTokenValidationParameters();
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments(ChatConstants.HubPath))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());
        services.AddSingleton<IAuthorizationHandler, MinimumRoleHandler>();

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IRevokedTokenRepository, RevokedTokenRepository>();

        services.Configure<GoogleAuthSettings>(configuration.GetSection(GoogleAuthSettings.SectionName));
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();

        services.AddSignalR();
        services.AddScoped<IChatNotificationService, ChatNotificationService>();

        return services;
    }

    public static IEndpointRouteBuilder MapInfrastructureEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<ChatHub>(ChatConstants.HubPath);
        return endpoints;
    }
}
