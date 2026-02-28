namespace BackBase.API.IntegrationTests.Fixtures;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using BackBase.API.DTOs;

public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    protected readonly CustomWebApplicationFactory Factory;
    protected readonly HttpClient Client;

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
}
