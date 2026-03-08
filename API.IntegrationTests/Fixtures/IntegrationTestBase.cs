namespace BackBase.API.IntegrationTests.Fixtures;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using BackBase.API.DTOs;
using BackBase.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    protected readonly CustomWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    private static int _userCounter;

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual Task DisposeAsync() => Task.CompletedTask;

    protected HttpClient CreateAuthenticatedClient(string jwtToken)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
        return client;
    }

    protected async Task<RegisterResponseDto> RegisterUserAsync(string email, string password)
    {
        var request = new RegisterRequestDto(email, password);
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RegisterResponseDto>())!;
    }

    protected async Task<LoginResponseDto> LoginUserAsync(string email, string password)
    {
        var request = new LoginRequestDto(email, password);
        var response = await Client.PostAsJsonAsync("/api/auth/login", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponseDto>())!;
    }

    protected string GenerateUniqueEmail(string prefix = "u")
    {
        var counter = Interlocked.Increment(ref _userCounter);
        return $"{prefix}{counter}_{Guid.NewGuid().ToString("N")[..8]}@t.co";
    }

    protected async Task<(RegisterResponseDto Registration, LoginResponseDto Login)> RegisterAndLoginUserAsync(string? email = null, string password = "StrongPass1")
    {
        email ??= GenerateUniqueEmail();
        var registration = await RegisterUserAsync(email, password);
        var login = await LoginUserAsync(email, password);
        return (registration, login);
    }

    protected async Task<(RegisterResponseDto Registration, LoginResponseDto Login)> CreateUserWithRoleAsync(string role, string password = "StrongPass1")
    {
        var email = GenerateUniqueEmail(role.ToLowerInvariant());
        var registration = await RegisterUserAsync(email, password);

        using var scope = Factory.Services.CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();
        await identityService.ReplaceRoleAsync(registration.UserId, role);

        var login = await LoginUserAsync(email, password);
        return (registration, login);
    }
}
