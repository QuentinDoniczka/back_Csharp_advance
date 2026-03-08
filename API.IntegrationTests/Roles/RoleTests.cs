namespace BackBase.API.IntegrationTests.Roles;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BackBase.API.DTOs;
using BackBase.API.IntegrationTests.Fixtures;
using BackBase.Domain.Constants;

public sealed class RoleTests : IntegrationTestBase
{
    private const string RolesBaseUrl = "/api/roles";
    private const string ValidPassword = "StrongPass1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RoleTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ChangeRole_SuperAdminPromotesToAdmin_Succeeds()
    {
        // Arrange
        var (_, superAdminLogin) = await CreateUserWithRoleAsync(AppRoles.SuperAdmin);
        var (targetRegistration, _) = await RegisterAndLoginUserAsync(password: ValidPassword);
        var adminClient = CreateAuthenticatedClient(superAdminLogin.AccessToken);

        // Act
        var response = await adminClient.PutAsJsonAsync(
            $"{RolesBaseUrl}/{targetRegistration.UserId}",
            new ChangeUserRoleRequestDto(AppRoles.Admin));

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify role changed
        var roleResponse = await adminClient.GetAsync($"{RolesBaseUrl}/{targetRegistration.UserId}");
        var roleBody = await roleResponse.Content.ReadFromJsonAsync<UserRoleResponseDto>(JsonOptions);
        Assert.NotNull(roleBody);
        Assert.Equal(AppRoles.Admin, roleBody.Role);
    }

    [Fact]
    public async Task ChangeRole_AdminPromotesToModerator_Succeeds()
    {
        // Arrange
        var (_, adminLogin) = await CreateUserWithRoleAsync(AppRoles.SuperAdmin);
        var (targetRegistration, _) = await RegisterAndLoginUserAsync(password: ValidPassword);

        // First, make a separate admin (promoted by superadmin)
        var (adminUserRegistration, _) = await RegisterAndLoginUserAsync(password: ValidPassword);
        var superAdminClient = CreateAuthenticatedClient(adminLogin.AccessToken);
        await superAdminClient.PutAsJsonAsync(
            $"{RolesBaseUrl}/{adminUserRegistration.UserId}",
            new ChangeUserRoleRequestDto(AppRoles.Admin));

        // Login the admin user to get a token with the Admin role
        var adminUserLogin = await LoginUserAsync(adminUserRegistration.Email, ValidPassword);
        var adminClient = CreateAuthenticatedClient(adminUserLogin.AccessToken);

        // Act - Admin promotes member to Moderator
        var response = await adminClient.PutAsJsonAsync(
            $"{RolesBaseUrl}/{targetRegistration.UserId}",
            new ChangeUserRoleRequestDto(AppRoles.Moderator));

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify with superadmin
        var roleResponse = await superAdminClient.GetAsync($"{RolesBaseUrl}/{targetRegistration.UserId}");
        var roleBody = await roleResponse.Content.ReadFromJsonAsync<UserRoleResponseDto>(JsonOptions);
        Assert.NotNull(roleBody);
        Assert.Equal(AppRoles.Moderator, roleBody.Role);
    }

    [Fact]
    public async Task ChangeRole_AdminCannotPromoteToAdmin_Returns403()
    {
        // Arrange — create a SuperAdmin and use them to promote another user to Admin
        var (_, superAdminLogin) = await CreateUserWithRoleAsync(AppRoles.SuperAdmin);
        var superAdminClient = CreateAuthenticatedClient(superAdminLogin.AccessToken);

        var (adminRegistration, _) = await RegisterAndLoginUserAsync(password: ValidPassword);
        await superAdminClient.PutAsJsonAsync(
            $"{RolesBaseUrl}/{adminRegistration.UserId}",
            new ChangeUserRoleRequestDto(AppRoles.Admin));
        var adminLogin = await LoginUserAsync(adminRegistration.Email, ValidPassword);
        var adminClient = CreateAuthenticatedClient(adminLogin.AccessToken);

        var (targetRegistration, _) = await RegisterAndLoginUserAsync(password: ValidPassword);

        // Act — Admin tries to promote member to Admin (same level)
        var response = await adminClient.PutAsJsonAsync(
            $"{RolesBaseUrl}/{targetRegistration.UserId}",
            new ChangeUserRoleRequestDto(AppRoles.Admin));

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Contains("Insufficient", body.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ChangeRole_MemberCannotChangeRoles_Returns403()
    {
        // Arrange
        var (targetRegistration, _) = await RegisterAndLoginUserAsync(password: ValidPassword);
        var (_, memberLogin) = await RegisterAndLoginUserAsync(password: ValidPassword);
        var memberClient = CreateAuthenticatedClient(memberLogin.AccessToken);

        // Act
        var response = await memberClient.PutAsJsonAsync(
            $"{RolesBaseUrl}/{targetRegistration.UserId}",
            new ChangeUserRoleRequestDto(AppRoles.Moderator));

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ChangeRole_CannotChangeOwnRole_Returns403()
    {
        // Arrange
        var (superAdminRegistration, superAdminLogin) = await CreateUserWithRoleAsync(AppRoles.SuperAdmin);
        var superAdminClient = CreateAuthenticatedClient(superAdminLogin.AccessToken);

        // Act — SuperAdmin tries to change their own role
        var response = await superAdminClient.PutAsJsonAsync(
            $"{RolesBaseUrl}/{superAdminRegistration.UserId}",
            new ChangeUserRoleRequestDto(AppRoles.Admin));

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Contains("own role", body.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetUserRole_AdminGetsRole_ReturnsRole()
    {
        // Arrange
        var (_, adminLogin) = await CreateUserWithRoleAsync(AppRoles.SuperAdmin);
        var (targetRegistration, _) = await RegisterAndLoginUserAsync(password: ValidPassword);
        var adminClient = CreateAuthenticatedClient(adminLogin.AccessToken);

        // Act
        var response = await adminClient.GetAsync($"{RolesBaseUrl}/{targetRegistration.UserId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var roleBody = await response.Content.ReadFromJsonAsync<UserRoleResponseDto>(JsonOptions);
        Assert.NotNull(roleBody);
        Assert.Equal(targetRegistration.UserId, roleBody.UserId);
        Assert.Equal(AppRoles.Member, roleBody.Role);
    }

    [Fact]
    public async Task GetUserRole_MemberCannotGetRole_Returns403()
    {
        // Arrange
        var (targetRegistration, _) = await RegisterAndLoginUserAsync(password: ValidPassword);
        var (_, memberLogin) = await RegisterAndLoginUserAsync(password: ValidPassword);
        var memberClient = CreateAuthenticatedClient(memberLogin.AccessToken);

        // Act
        var response = await memberClient.GetAsync($"{RolesBaseUrl}/{targetRegistration.UserId}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUserRole_Unauthenticated_Returns401()
    {
        // Act
        var response = await Client.GetAsync($"{RolesBaseUrl}/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangeRole_AdminCannotDemoteOtherAdmin_Returns403()
    {
        // Arrange — create a SuperAdmin and two admins
        var (_, superAdminLogin) = await CreateUserWithRoleAsync(AppRoles.SuperAdmin);
        var superAdminClient = CreateAuthenticatedClient(superAdminLogin.AccessToken);

        var (admin1Registration, _) = await RegisterAndLoginUserAsync(password: ValidPassword);
        await superAdminClient.PutAsJsonAsync(
            $"{RolesBaseUrl}/{admin1Registration.UserId}",
            new ChangeUserRoleRequestDto(AppRoles.Admin));
        var admin1Login = await LoginUserAsync(admin1Registration.Email, ValidPassword);
        var admin1Client = CreateAuthenticatedClient(admin1Login.AccessToken);

        var (admin2Registration, _) = await RegisterAndLoginUserAsync(password: ValidPassword);
        await superAdminClient.PutAsJsonAsync(
            $"{RolesBaseUrl}/{admin2Registration.UserId}",
            new ChangeUserRoleRequestDto(AppRoles.Admin));

        // Act — Admin1 tries to demote Admin2 to Member (same level target)
        var response = await admin1Client.PutAsJsonAsync(
            $"{RolesBaseUrl}/{admin2Registration.UserId}",
            new ChangeUserRoleRequestDto(AppRoles.Member));

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ChangeRole_SuperAdminDemotesAdminToMember_Succeeds()
    {
        // Arrange
        var (_, superAdminLogin) = await CreateUserWithRoleAsync(AppRoles.SuperAdmin);
        var superAdminClient = CreateAuthenticatedClient(superAdminLogin.AccessToken);

        var (adminRegistration, _) = await RegisterAndLoginUserAsync(password: ValidPassword);
        await superAdminClient.PutAsJsonAsync(
            $"{RolesBaseUrl}/{adminRegistration.UserId}",
            new ChangeUserRoleRequestDto(AppRoles.Admin));

        // Act — SuperAdmin demotes Admin to Member
        var response = await superAdminClient.PutAsJsonAsync(
            $"{RolesBaseUrl}/{adminRegistration.UserId}",
            new ChangeUserRoleRequestDto(AppRoles.Member));

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var roleResponse = await superAdminClient.GetAsync($"{RolesBaseUrl}/{adminRegistration.UserId}");
        var roleBody = await roleResponse.Content.ReadFromJsonAsync<UserRoleResponseDto>(JsonOptions);
        Assert.NotNull(roleBody);
        Assert.Equal(AppRoles.Member, roleBody.Role);
    }

    [Fact]
    public async Task ChangeRole_NonExistentUser_Returns404()
    {
        // Arrange
        var (_, superAdminLogin) = await CreateUserWithRoleAsync(AppRoles.SuperAdmin);
        var superAdminClient = CreateAuthenticatedClient(superAdminLogin.AccessToken);

        // Act
        var response = await superAdminClient.PutAsJsonAsync(
            $"{RolesBaseUrl}/{Guid.NewGuid()}",
            new ChangeUserRoleRequestDto(AppRoles.Moderator));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ChangeRole_InvalidRole_Returns403()
    {
        // Arrange
        var (_, superAdminLogin) = await CreateUserWithRoleAsync(AppRoles.SuperAdmin);
        var (targetRegistration, _) = await RegisterAndLoginUserAsync(password: ValidPassword);
        var superAdminClient = CreateAuthenticatedClient(superAdminLogin.AccessToken);

        // Act
        var response = await superAdminClient.PutAsJsonAsync(
            $"{RolesBaseUrl}/{targetRegistration.UserId}",
            new ChangeUserRoleRequestDto("NonExistentRole"));

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private sealed record ErrorResponse(string Title, int Status, string Detail);
}
