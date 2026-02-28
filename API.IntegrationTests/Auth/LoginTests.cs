namespace BackBase.API.IntegrationTests.Auth;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using BackBase.API.DTOs;
using BackBase.API.IntegrationTests.Fixtures;

public sealed class LoginTests : IntegrationTestBase
{
    private const string LoginUrl = "/api/auth/login";
    private const string ValidPassword = "StrongPass1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public LoginTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithTokens()
    {
        // Arrange
        var email = $"login-valid-{Guid.NewGuid():N}@test.com";
        await RegisterUserAsync(email, ValidPassword);

        var request = new LoginRequestDto(email, ValidPassword);

        // Act
        var response = await Client.PostAsJsonAsync(LoginUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(body.RefreshToken));
        Assert.True(body.AccessTokenExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_ValidCredentials_AccessTokenContainsExpectedClaims()
    {
        // Arrange
        var email = $"login-claims-{Guid.NewGuid():N}@test.com";
        var registered = await RegisterUserAsync(email, ValidPassword);

        var request = new LoginRequestDto(email, ValidPassword);

        // Act
        var response = await Client.PostAsJsonAsync(LoginUrl, request);
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);

        // Assert
        Assert.NotNull(body);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(body.AccessToken);

        var userIdClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        var emailClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
        var roleClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);

        Assert.NotNull(userIdClaim);
        Assert.Equal(registered.UserId.ToString(), userIdClaim.Value);
        Assert.NotNull(emailClaim);
        Assert.Equal(email, emailClaim.Value);
        Assert.NotNull(roleClaim);
        Assert.Equal("Member", roleClaim.Value);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        // Arrange
        var email = $"login-wrong-pw-{Guid.NewGuid():N}@test.com";
        await RegisterUserAsync(email, ValidPassword);

        var request = new LoginRequestDto(email, "WrongPassword1");

        // Act
        var response = await Client.PostAsJsonAsync(LoginUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Contains("Invalid credentials", body.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_NonExistentEmail_Returns401()
    {
        // Arrange
        var email = $"login-nonexistent-{Guid.NewGuid():N}@test.com";
        var request = new LoginRequestDto(email, ValidPassword);

        // Act
        var response = await Client.PostAsJsonAsync(LoginUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Contains("Invalid credentials", body.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_EmptyEmail_Returns400WithValidationError()
    {
        // Arrange
        var request = new LoginRequestDto("", ValidPassword);

        // Act
        var response = await Client.PostAsJsonAsync(LoginUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Contains("Email", body.Errors.Keys);
    }

    [Fact]
    public async Task Login_EmptyPassword_Returns400WithValidationError()
    {
        // Arrange
        var email = $"login-empty-pw-{Guid.NewGuid():N}@test.com";
        var request = new LoginRequestDto(email, "");

        // Act
        var response = await Client.PostAsJsonAsync(LoginUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Contains("Password", body.Errors.Keys);
    }

    [Fact]
    public async Task Login_InvalidEmailFormat_Returns400WithValidationError()
    {
        // Arrange
        var request = new LoginRequestDto("not-an-email", ValidPassword);

        // Act
        var response = await Client.PostAsJsonAsync(LoginUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Contains("Email", body.Errors.Keys);
    }

    [Fact]
    public async Task Login_BothFieldsEmpty_Returns400WithMultipleValidationErrors()
    {
        // Arrange
        var request = new LoginRequestDto("", "");

        // Act
        var response = await Client.PostAsJsonAsync(LoginUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Contains("Email", body.Errors.Keys);
        Assert.Contains("Password", body.Errors.Keys);
    }

    [Fact]
    public async Task Login_RegisterThenLogin_TokenIsUsableForAuthenticatedEndpoint()
    {
        // Arrange
        var email = $"login-authed-{Guid.NewGuid():N}@test.com";
        await RegisterUserAsync(email, ValidPassword);
        var loginResult = await LoginUserAsync(email, ValidPassword);

        // Act - use the token to call an authenticated endpoint (health check is anonymous, so
        // we'll try the logout endpoint which requires auth)
        var authenticatedClient = CreateAuthenticatedClient(loginResult.AccessToken);
        var logoutRequest = new LogoutRequestDto(loginResult.RefreshToken);
        var response = await authenticatedClient.PostAsJsonAsync("/api/auth/logout", logoutRequest);

        // Assert - 204 NoContent means the token was accepted
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Login_RefreshTokenIsDifferentFromAccessToken()
    {
        // Arrange
        var email = $"login-token-diff-{Guid.NewGuid():N}@test.com";
        await RegisterUserAsync(email, ValidPassword);

        var request = new LoginRequestDto(email, ValidPassword);

        // Act
        var response = await Client.PostAsJsonAsync(LoginUrl, request);
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);

        // Assert
        Assert.NotNull(body);
        Assert.NotEqual(body.AccessToken, body.RefreshToken);
    }

    private sealed record ValidationErrorResponse(
        string Title,
        int Status,
        Dictionary<string, string[]> Errors);

    private sealed record AuthErrorResponse(
        string Title,
        int Status,
        string Detail);
}
