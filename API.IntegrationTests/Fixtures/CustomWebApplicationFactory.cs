namespace BackBase.API.IntegrationTests.Fixtures;

using BackBase.Application.Interfaces;
using BackBase.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Testcontainers.PostgreSql;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestJwtSecret = "IntegrationTestSecretKeyThatIsAtLeast32Characters!";
    private const string TestJwtIssuer = "BackBase.IntegrationTests";
    private const string TestJwtAudience = "BackBase.IntegrationTests";

    public static string JwtSecret => TestJwtSecret;
    public static string JwtIssuer => TestJwtIssuer;
    public static string JwtAudience => TestJwtAudience;

    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .WithDatabase("backbase_integration_tests")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            RemoveService<DbContextOptions<AppDbContext>>(services);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_dbContainer.GetConnectionString()));

            var googleTokenValidator = Substitute.For<IGoogleTokenValidator>();
            RemoveService<IGoogleTokenValidator>(services);
            services.AddScoped(_ => googleTokenValidator);
        });

        builder.UseSetting("JwtSettings:Secret", TestJwtSecret);
        builder.UseSetting("JwtSettings:Issuer", TestJwtIssuer);
        builder.UseSetting("JwtSettings:Audience", TestJwtAudience);
        builder.UseSetting("JwtSettings:AccessTokenExpirationMinutes", "30");
        builder.UseSetting("JwtSettings:RefreshTokenExpirationDays", "30");
        builder.UseSetting("AllowedOrigins:0", "http://localhost:3000");
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        // Apply migrations BEFORE the host is built (before Services is accessed).
        // This is critical because Program.cs calls InitializeInfrastructureAsync()
        // which seeds roles — the tables must exist before that runs.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_dbContainer.GetConnectionString())
            .Options;

        await using var dbContext = new AppDbContext(options);
        await dbContext.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
    }

    private static void RemoveService<T>(IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }
    }
}
