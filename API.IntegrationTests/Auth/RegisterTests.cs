namespace BackBase.API.IntegrationTests.Auth;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BackBase.API.DTOs;
using BackBase.API.IntegrationTests.Fixtures;

public sealed class RegisterTests : IntegrationTestBase
{
    private const string RegisterUrl = "/api/auth/register";
    private const string ValidPassword = "StrongPass1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RegisterTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Register_ValidCredentials_ReturnsOkWithUserIdAndEmail()
    {
        // Arrange
        var email = $"register-valid-{Guid.NewGuid():N}@test.com";
        var request = new RegisterRequestDto(email, ValidPassword);

        // Act
        var response = await Client.PostAsJsonAsync(RegisterUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RegisterResponseDto>(JsonOptions);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.UserId);
        Assert.Equal(email, body.Email);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns401WithError()
    {
        // Arrange
        var email = $"register-dup-{Guid.NewGuid():N}@test.com";
        var request = new RegisterRequestDto(email, ValidPassword);

        // Register the first user
        var firstResponse = await Client.PostAsJsonAsync(RegisterUrl, request);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        // Act - register the same email again
        var response = await Client.PostAsJsonAsync(RegisterUrl, request);

        // Assert - Identity throws AuthenticationException which maps to 401
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("already taken", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_EmptyEmail_Returns400WithValidationError()
    {
        // Arrange
        var request = new RegisterRequestDto("", ValidPassword);

        // Act
        var response = await Client.PostAsJsonAsync(RegisterUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Contains("Email", body.Errors.Keys);
    }

    [Fact]
    public async Task Register_InvalidEmailFormat_Returns400WithValidationError()
    {
        // Arrange
        var request = new RegisterRequestDto("not-an-email", ValidPassword);

        // Act
        var response = await Client.PostAsJsonAsync(RegisterUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Contains("Email", body.Errors.Keys);
        Assert.Contains(body.Errors["Email"], msg => msg.Contains("not valid", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Register_EmptyPassword_Returns400WithValidationError()
    {
        // Arrange
        var email = $"register-empty-pw-{Guid.NewGuid():N}@test.com";
        var request = new RegisterRequestDto(email, "");

        // Act
        var response = await Client.PostAsJsonAsync(RegisterUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Contains("Password", body.Errors.Keys);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400WithValidationError()
    {
        // Arrange
        var email = $"register-short-pw-{Guid.NewGuid():N}@test.com";
        var request = new RegisterRequestDto(email, "Aa1");

        // Act
        var response = await Client.PostAsJsonAsync(RegisterUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Contains("Password", body.Errors.Keys);
        Assert.Contains(body.Errors["Password"], msg => msg.Contains("8 characters", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Register_PasswordMissingUppercase_Returns400WithValidationError()
    {
        // Arrange
        var email = $"register-no-upper-{Guid.NewGuid():N}@test.com";
        var request = new RegisterRequestDto(email, "lowercase1");

        // Act
        var response = await Client.PostAsJsonAsync(RegisterUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Contains("Password", body.Errors.Keys);
        Assert.Contains(body.Errors["Password"], msg => msg.Contains("uppercase", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Register_PasswordMissingLowercase_Returns400WithValidationError()
    {
        // Arrange
        var email = $"register-no-lower-{Guid.NewGuid():N}@test.com";
        var request = new RegisterRequestDto(email, "UPPERCASE1");

        // Act
        var response = await Client.PostAsJsonAsync(RegisterUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Contains("Password", body.Errors.Keys);
        Assert.Contains(body.Errors["Password"], msg => msg.Contains("lowercase", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Register_PasswordMissingDigit_Returns400WithValidationError()
    {
        // Arrange
        var email = $"register-no-digit-{Guid.NewGuid():N}@test.com";
        var request = new RegisterRequestDto(email, "NoDigitsHere");

        // Act
        var response = await Client.PostAsJsonAsync(RegisterUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Contains("Password", body.Errors.Keys);
        Assert.Contains(body.Errors["Password"], msg => msg.Contains("digit", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Register_BothFieldsEmpty_Returns400WithMultipleValidationErrors()
    {
        // Arrange
        var request = new RegisterRequestDto("", "");

        // Act
        var response = await Client.PostAsJsonAsync(RegisterUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Contains("Email", body.Errors.Keys);
        Assert.Contains("Password", body.Errors.Keys);
    }

    private sealed record ValidationErrorResponse(
        string Title,
        int Status,
        Dictionary<string, string[]> Errors);
}
