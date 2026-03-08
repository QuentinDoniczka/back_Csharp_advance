namespace BackBase.API.IntegrationTests.Notifications;

using BackBase.API.IntegrationTests.Fixtures;
using BackBase.Application.Constants;
using BackBase.Application.DTOs.Output;
using BackBase.Application.Interfaces;
using BackBase.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR.Client;

public sealed class PersonalNotificationTests : SignalRTestBase
{
    private static int _notificationUserCounter;
    private static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan NegativeTimeout = TimeSpan.FromSeconds(2);

    public PersonalNotificationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task SendNotification_TargetUserReceives()
    {
        // Arrange
        var (userId, token, _) = await RegisterAndLoginWithUserIdAsync();

        var connection = CreateHubConnection(token);
        await connection.StartAsync();

        var tcs = new TaskCompletionSource<NotificationOutput>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<NotificationOutput>(NotificationConstants.ReceiveNotificationMethod, msg => tcs.SetResult(msg));

        var notification = new NotificationOutput(
            NotificationType.LevelUp,
            Guid.NewGuid(),
            5,
            DateTime.UtcNow);

        // Act
        using var scope = Factory.Services.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<IPersonalNotificationService>();
        await notificationService.SendToUserAsync(userId, notification);

        // Assert
        var received = await tcs.Task.WaitAsync(ReceiveTimeout);
        Assert.Equal(NotificationType.LevelUp, received.Type);
    }

    [Fact]
    public async Task SendNotification_OtherUserDoesNotReceive()
    {
        // Arrange
        var userA = await RegisterAndLoginWithUserIdAsync();
        var userB = await RegisterAndLoginWithUserIdAsync();

        var connectionA = CreateHubConnection(userA.AccessToken);
        var connectionB = CreateHubConnection(userB.AccessToken);

        await connectionA.StartAsync();
        await connectionB.StartAsync();

        var received = false;
        connectionB.On<NotificationOutput>(NotificationConstants.ReceiveNotificationMethod, _ => received = true);

        var notification = new NotificationOutput(
            NotificationType.QuestCompleted,
            Guid.NewGuid(),
            1,
            DateTime.UtcNow);

        // Act - send to user A only
        using var scope = Factory.Services.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<IPersonalNotificationService>();
        await notificationService.SendToUserAsync(userA.UserId, notification);

        // Assert - user B should NOT receive
        await Task.Delay(NegativeTimeout);
        Assert.False(received, "User B should not receive a notification sent to User A.");
    }

    [Fact]
    public async Task SendNotification_PayloadIsCorrect()
    {
        // Arrange
        var user = await RegisterAndLoginWithUserIdAsync();
        var connection = CreateHubConnection(user.AccessToken);
        await connection.StartAsync();

        var tcs = new TaskCompletionSource<NotificationOutput>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<NotificationOutput>(NotificationConstants.ReceiveNotificationMethod, msg => tcs.SetResult(msg));

        var referenceId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;
        var notification = new NotificationOutput(
            NotificationType.ItemReceived,
            referenceId,
            3,
            occurredAt);

        // Act
        using var scope = Factory.Services.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<IPersonalNotificationService>();
        await notificationService.SendToUserAsync(user.UserId, notification);

        // Assert
        var received = await tcs.Task.WaitAsync(ReceiveTimeout);
        Assert.Equal(NotificationType.ItemReceived, received.Type);
        Assert.Equal(referenceId, received.ReferenceId);
        Assert.Equal(3, received.Count);
        Assert.Equal(occurredAt, received.OccurredAt);
    }

    [Fact]
    public async Task SendNotification_UserNotConnected_NoError()
    {
        // Arrange
        var disconnectedUserId = Guid.NewGuid();
        var notification = new NotificationOutput(
            NotificationType.SystemMessage,
            null,
            null,
            DateTime.UtcNow);

        // Act & Assert - should not throw
        using var scope = Factory.Services.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<IPersonalNotificationService>();
        await notificationService.SendToUserAsync(disconnectedUserId, notification);
    }

    private async Task<(Guid UserId, string AccessToken, string Email)> RegisterAndLoginWithUserIdAsync()
    {
        var counter = Interlocked.Increment(ref _notificationUserCounter);
        var email = $"notification_test_{counter}_{Guid.NewGuid():N}@test.com";
        const string password = "TestPassword1";

        var registerResponse = await RegisterUserAsync(email, password);
        var loginResponse = await LoginUserAsync(email, password);

        return (registerResponse.UserId, loginResponse.AccessToken, email);
    }
}
