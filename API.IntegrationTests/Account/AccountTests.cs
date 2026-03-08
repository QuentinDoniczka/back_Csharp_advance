namespace BackBase.API.IntegrationTests.Account;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BackBase.API.DTOs;
using BackBase.API.IntegrationTests.Fixtures;
using BackBase.Domain.Constants;

public sealed class AccountTests : IntegrationTestBase
{
    private const string MyProfileUrl = "/api/account/me";
    private const string UpdateProfileUrl = "/api/account/me";
    private const string ChangePasswordUrl = "/api/account/me/change-password";
    private const string DeactivateUrl = "/api/account/me/deactivate";
    private const string LoginUrl = "/api/auth/login";
    private const string ValidPassword = "StrongPass1";
    private const string NewPassword = "NewStrongPass2";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AccountTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetMyProfile_AuthenticatedUser_ReturnsProfileWithLocalPartAsDisplayName()
    {
        // Arrange
        var email = GenerateUniqueEmail("profile-me");
        var expectedDisplayName = email.Split('@')[0];
        var (registration, login) = await RegisterAndLoginUserAsync(email, ValidPassword);
        var client = CreateAuthenticatedClient(login.AccessToken);

        // Act
        var response = await client.GetAsync(MyProfileUrl);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponseDto>(JsonOptions);
        Assert.NotNull(profile);
        Assert.Equal(registration.UserId, profile.UserId);
        Assert.Equal(expectedDisplayName, profile.DisplayName);
        Assert.False(profile.IsDeactivated);
    }

    [Fact]
    public async Task GetMyProfile_Unauthenticated_Returns401()
    {
        // Act
        var response = await Client.GetAsync(MyProfileUrl);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserProfile_AnotherUsersProfile_ReturnsTheirProfile()
    {
        // Arrange
        var emailA = GenerateUniqueEmail("user-a");
        var emailB = GenerateUniqueEmail("user-b");
        var expectedDisplayNameB = emailB.Split('@')[0];
        var (_, loginA) = await RegisterAndLoginUserAsync(emailA, ValidPassword);
        var (registrationB, _) = await RegisterAndLoginUserAsync(emailB, ValidPassword);
        var clientA = CreateAuthenticatedClient(loginA.AccessToken);

        // Act
        var response = await clientA.GetAsync($"/api/account/{registrationB.UserId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponseDto>(JsonOptions);
        Assert.NotNull(profile);
        Assert.Equal(registrationB.UserId, profile.UserId);
        Assert.Equal(expectedDisplayNameB, profile.DisplayName);
    }

    [Fact]
    public async Task UpdateProfile_ValidData_ReturnsUpdatedProfile()
    {
        // Arrange
        var (_, login) = await RegisterAndLoginUserAsync(password: ValidPassword);
        var client = CreateAuthenticatedClient(login.AccessToken);
        var updateRequest = new UpdateProfileRequestDto("NewDisplayName", "https://example.com/avatar.png");

        // Act
        var response = await client.PutAsJsonAsync(UpdateProfileUrl, updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedProfile = await response.Content.ReadFromJsonAsync<UserProfileResponseDto>(JsonOptions);
        Assert.NotNull(updatedProfile);
        Assert.Equal("NewDisplayName", updatedProfile.DisplayName);
        Assert.Equal("https://example.com/avatar.png", updatedProfile.AvatarUrl);
    }

    [Fact]
    public async Task UpdateProfile_ChangesPersist_GetMyProfileReturnsUpdatedData()
    {
        // Arrange
        var (_, login) = await RegisterAndLoginUserAsync(password: ValidPassword);
        var client = CreateAuthenticatedClient(login.AccessToken);
        var updateRequest = new UpdateProfileRequestDto("PersistTest", "https://example.com/persist.png");

        // Act
        await client.PutAsJsonAsync(UpdateProfileUrl, updateRequest);
        var getResponse = await client.GetAsync(MyProfileUrl);

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var profile = await getResponse.Content.ReadFromJsonAsync<UserProfileResponseDto>(JsonOptions);
        Assert.NotNull(profile);
        Assert.Equal("PersistTest", profile.DisplayName);
        Assert.Equal("https://example.com/persist.png", profile.AvatarUrl);
    }

    [Fact]
    public async Task UpdateProfile_EmptyDisplayName_Returns400()
    {
        // Arrange
        var (_, login) = await RegisterAndLoginUserAsync(password: ValidPassword);
        var client = CreateAuthenticatedClient(login.AccessToken);
        var updateRequest = new UpdateProfileRequestDto("", null);

        // Act
        var response = await client.PutAsJsonAsync(UpdateProfileUrl, updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_DisplayNameTooShort_Returns400()
    {
        // Arrange
        var (_, login) = await RegisterAndLoginUserAsync(password: ValidPassword);
        var client = CreateAuthenticatedClient(login.AccessToken);
        var updateRequest = new UpdateProfileRequestDto("A", null);

        // Act
        var response = await client.PutAsJsonAsync(UpdateProfileUrl, updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_InvalidAvatarUrl_Returns400()
    {
        // Arrange
        var (_, login) = await RegisterAndLoginUserAsync(password: ValidPassword);
        var client = CreateAuthenticatedClient(login.AccessToken);
        var updateRequest = new UpdateProfileRequestDto("ValidName", "not-a-url");

        // Act
        var response = await client.PutAsJsonAsync(UpdateProfileUrl, updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_NullAvatarUrl_Succeeds()
    {
        // Arrange
        var (_, login) = await RegisterAndLoginUserAsync(password: ValidPassword);
        var client = CreateAuthenticatedClient(login.AccessToken);
        var updateRequest = new UpdateProfileRequestDto("ValidName", null);

        // Act
        var response = await client.PutAsJsonAsync(UpdateProfileUrl, updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponseDto>(JsonOptions);
        Assert.NotNull(profile);
        Assert.Equal("ValidName", profile.DisplayName);
        Assert.Null(profile.AvatarUrl);
    }

    [Fact]
    public async Task ChangePassword_ValidPasswords_LoginWithNewPasswordSucceeds()
    {
        // Arrange
        var email = GenerateUniqueEmail("changepw");
        var (_, login) = await RegisterAndLoginUserAsync(email, ValidPassword);
        var client = CreateAuthenticatedClient(login.AccessToken);
        var changeRequest = new ChangePasswordRequestDto(ValidPassword, NewPassword);

        // Act
        var response = await client.PostAsJsonAsync(ChangePasswordUrl, changeRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify new password works
        var newLoginResponse = await Client.PostAsJsonAsync(LoginUrl, new LoginRequestDto(email, NewPassword));
        Assert.Equal(HttpStatusCode.OK, newLoginResponse.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ValidPasswords_LoginWithOldPasswordFails()
    {
        // Arrange
        var email = GenerateUniqueEmail("changepw-old");
        var (_, login) = await RegisterAndLoginUserAsync(email, ValidPassword);
        var client = CreateAuthenticatedClient(login.AccessToken);
        var changeRequest = new ChangePasswordRequestDto(ValidPassword, NewPassword);

        await client.PostAsJsonAsync(ChangePasswordUrl, changeRequest);

        // Act
        var oldLoginResponse = await Client.PostAsJsonAsync(LoginUrl, new LoginRequestDto(email, ValidPassword));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, oldLoginResponse.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ReturnsError()
    {
        // Arrange
        var (_, login) = await RegisterAndLoginUserAsync(password: ValidPassword);
        var client = CreateAuthenticatedClient(login.AccessToken);
        var changeRequest = new ChangePasswordRequestDto("WrongCurrent1", NewPassword);

        // Act
        var response = await client.PostAsJsonAsync(ChangePasswordUrl, changeRequest);

        // Assert
        Assert.NotEqual(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_SameAsCurrentPassword_Returns400()
    {
        // Arrange
        var (_, login) = await RegisterAndLoginUserAsync(password: ValidPassword);
        var client = CreateAuthenticatedClient(login.AccessToken);
        var changeRequest = new ChangePasswordRequestDto(ValidPassword, ValidPassword);

        // Act
        var response = await client.PostAsJsonAsync(ChangePasswordUrl, changeRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateAccount_AuthenticatedUser_LoginRejected()
    {
        // Arrange
        var email = GenerateUniqueEmail("deactivate");
        var (_, login) = await RegisterAndLoginUserAsync(email, ValidPassword);
        var client = CreateAuthenticatedClient(login.AccessToken);

        // Act
        var deactivateResponse = await client.PostAsync(DeactivateUrl, null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deactivateResponse.StatusCode);

        // Verify login is rejected
        var loginResponse = await Client.PostAsJsonAsync(LoginUrl, new LoginRequestDto(email, ValidPassword));
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);

        var body = await loginResponse.Content.ReadAsStringAsync();
        Assert.Contains("deactivated", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeactivateAccount_AlreadyDeactivated_Returns409()
    {
        // Arrange
        var email = GenerateUniqueEmail("deactivate-twice");
        var (_, login) = await RegisterAndLoginUserAsync(email, ValidPassword);
        var client = CreateAuthenticatedClient(login.AccessToken);

        // First deactivation
        await client.PostAsync(DeactivateUrl, null);

        // Act - second deactivation (using same token which is still valid)
        var secondResponse = await client.PostAsync(DeactivateUrl, null);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task ReactivateAccount_AdminReactivates_UserCanLoginAgain()
    {
        // Arrange
        var email = GenerateUniqueEmail("reactivate");
        var (userRegistration, userLogin) = await RegisterAndLoginUserAsync(email, ValidPassword);
        var userClient = CreateAuthenticatedClient(userLogin.AccessToken);

        // Deactivate the user account
        await userClient.PostAsync(DeactivateUrl, null);

        // Verify login is rejected after deactivation
        var rejectedLogin = await Client.PostAsJsonAsync(LoginUrl, new LoginRequestDto(email, ValidPassword));
        Assert.Equal(HttpStatusCode.Unauthorized, rejectedLogin.StatusCode);

        // Create an admin user
        var (_, adminLogin) = await CreateUserWithRoleAsync(AppRoles.SuperAdmin);
        var adminClient = CreateAuthenticatedClient(adminLogin.AccessToken);

        // Act - admin reactivates the user
        var reactivateResponse = await adminClient.PostAsync($"/api/account/{userRegistration.UserId}/reactivate", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, reactivateResponse.StatusCode);

        // Verify user can login again
        var loginResponse = await Client.PostAsJsonAsync(LoginUrl, new LoginRequestDto(email, ValidPassword));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task ReactivateAccount_NonAdmin_Returns403()
    {
        // Arrange
        var (userRegistration, _) = await RegisterAndLoginUserAsync(password: ValidPassword);
        var (_, memberLogin) = await RegisterAndLoginUserAsync(password: ValidPassword);
        var memberClient = CreateAuthenticatedClient(memberLogin.AccessToken);

        // Act - member tries to reactivate
        var response = await memberClient.PostAsync($"/api/account/{userRegistration.UserId}/reactivate", null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_DeactivatedUser_StillReturnsProfile()
    {
        // Arrange
        var email = GenerateUniqueEmail("deactivated-visible");
        var (registration, login) = await RegisterAndLoginUserAsync(email, ValidPassword);
        var client = CreateAuthenticatedClient(login.AccessToken);

        // Deactivate
        await client.PostAsync(DeactivateUrl, null);

        // Use another user to view the deactivated user's profile
        var (_, otherLogin) = await RegisterAndLoginUserAsync(password: ValidPassword);
        var otherClient = CreateAuthenticatedClient(otherLogin.AccessToken);

        // Act
        var response = await otherClient.GetAsync($"/api/account/{registration.UserId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponseDto>(JsonOptions);
        Assert.NotNull(profile);
        Assert.Equal(registration.UserId, profile.UserId);
        Assert.True(profile.IsDeactivated);
    }

    [Fact]
    public async Task FullLifecycle_RegisterUpdateDeactivateReactivateLogin()
    {
        // Arrange
        var email = GenerateUniqueEmail("lifecycle");

        // Step 1: Register user
        var (registration, login) = await RegisterAndLoginUserAsync(email, ValidPassword);
        var client = CreateAuthenticatedClient(login.AccessToken);

        // Step 2: Update profile
        var updateRequest = new UpdateProfileRequestDto("LifecycleUser", "https://example.com/lifecycle.png");
        var updateResponse = await client.PutAsJsonAsync(UpdateProfileUrl, updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // Step 3: Verify profile updated
        var profileResponse = await client.GetAsync(MyProfileUrl);
        var profile = await profileResponse.Content.ReadFromJsonAsync<UserProfileResponseDto>(JsonOptions);
        Assert.NotNull(profile);
        Assert.Equal("LifecycleUser", profile.DisplayName);

        // Step 4: Create admin and change user's role to Moderator
        var (_, adminLogin) = await CreateUserWithRoleAsync(AppRoles.SuperAdmin);
        var adminClient = CreateAuthenticatedClient(adminLogin.AccessToken);
        var roleChangeResponse = await adminClient.PutAsJsonAsync(
            $"/api/roles/{registration.UserId}",
            new ChangeUserRoleRequestDto(AppRoles.Moderator));
        Assert.Equal(HttpStatusCode.NoContent, roleChangeResponse.StatusCode);

        // Step 5: Verify role changed
        var roleResponse = await adminClient.GetAsync($"/api/roles/{registration.UserId}");
        var roleBody = await roleResponse.Content.ReadFromJsonAsync<UserRoleResponseDto>(JsonOptions);
        Assert.NotNull(roleBody);
        Assert.Equal(AppRoles.Moderator, roleBody.Role);

        // Step 6: User deactivates their account
        await client.PostAsync(DeactivateUrl, null);

        // Step 7: Login fails
        var rejectedLogin = await Client.PostAsJsonAsync(LoginUrl, new LoginRequestDto(email, ValidPassword));
        Assert.Equal(HttpStatusCode.Unauthorized, rejectedLogin.StatusCode);

        // Step 8: Admin reactivates the user
        var reactivateResponse = await adminClient.PostAsync($"/api/account/{registration.UserId}/reactivate", null);
        Assert.Equal(HttpStatusCode.NoContent, reactivateResponse.StatusCode);

        // Step 9: User can login again
        var reLogin = await LoginUserAsync(email, ValidPassword);
        Assert.False(string.IsNullOrWhiteSpace(reLogin.AccessToken));

        // Step 10: Profile still has updated data and role is preserved
        var reClient = CreateAuthenticatedClient(reLogin.AccessToken);
        var finalProfile = await reClient.GetAsync(MyProfileUrl);
        var finalBody = await finalProfile.Content.ReadFromJsonAsync<UserProfileResponseDto>(JsonOptions);
        Assert.NotNull(finalBody);
        Assert.Equal("LifecycleUser", finalBody.DisplayName);
        Assert.False(finalBody.IsDeactivated);
    }

    private sealed record ErrorResponse(string Title, int Status, string Detail);
}
