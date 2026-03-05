namespace BackBase.API.IntegrationTests.Chat;

using BackBase.API.IntegrationTests.Fixtures;
using BackBase.Application.Constants;
using BackBase.Application.DTOs.Output;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

public sealed class GameHubTests : SignalRTestBase
{
    private const string TestMessage = "Hello, world!";
    private const string DefaultSalonName = "General";
    private static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan NegativeTimeout = TimeSpan.FromSeconds(2);

    public GameHubTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Connect_WithValidToken_Succeeds()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginAsync();
        var connection = CreateHubConnection(accessToken);

        // Act
        await connection.StartAsync();

        // Assert
        Assert.Equal(HubConnectionState.Connected, connection.State);
    }

    [Fact]
    public async Task Connect_WithoutToken_Fails()
    {
        // Arrange
        var connection = CreateHubConnection();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => connection.StartAsync());
        Assert.NotEqual(HubConnectionState.Connected, connection.State);
    }

    [Fact]
    public async Task Connect_WithInvalidToken_Fails()
    {
        // Arrange
        var connection = CreateHubConnection("this-is-not-a-valid-jwt-token");

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => connection.StartAsync());
        Assert.NotEqual(HubConnectionState.Connected, connection.State);
    }

    [Fact]
    public async Task JoinSalon_ThenReceiveMessage_FromAnotherUser()
    {
        // Arrange
        var (tokenA, _) = await RegisterAndLoginAsync();
        var (tokenB, emailB) = await RegisterAndLoginAsync();

        var connectionA = CreateHubConnection(tokenA);
        var connectionB = CreateHubConnection(tokenB);

        await connectionA.StartAsync();
        await connectionB.StartAsync();

        var tcs = new TaskCompletionSource<ChatMessageOutput>(TaskCreationOptions.RunContinuationsAsynchronously);
        connectionA.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, msg => tcs.SetResult(msg));

        await connectionA.InvokeAsync(ChatConstants.JoinSalonMethod, DefaultSalonName);
        await connectionB.InvokeAsync(ChatConstants.JoinSalonMethod, DefaultSalonName);

        // Act
        await connectionB.InvokeAsync(ChatConstants.SendMessageMethod, DefaultSalonName, TestMessage);

        // Assert
        var received = await tcs.Task.WaitAsync(ReceiveTimeout);
        Assert.Equal(emailB, received.SenderEmail);
        Assert.Equal(TestMessage, received.Message);
        Assert.NotEqual(Guid.Empty, received.SenderUserId);
        Assert.True(received.SentAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task SendMessage_SenderAlsoReceivesOwnMessage()
    {
        // Arrange
        var (accessToken, email) = await RegisterAndLoginAsync();
        var connection = CreateHubConnection(accessToken);

        await connection.StartAsync();

        var tcs = new TaskCompletionSource<ChatMessageOutput>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, msg => tcs.SetResult(msg));

        await connection.InvokeAsync(ChatConstants.JoinSalonMethod, DefaultSalonName);

        // Act
        await connection.InvokeAsync(ChatConstants.SendMessageMethod, DefaultSalonName, TestMessage);

        // Assert
        var received = await tcs.Task.WaitAsync(ReceiveTimeout);
        Assert.Equal(email, received.SenderEmail);
        Assert.Equal(TestMessage, received.Message);
    }

    [Fact]
    public async Task SendMessage_UserNotInGroup_DoesNotReceiveMessage()
    {
        // Arrange
        var (tokenA, _) = await RegisterAndLoginAsync();
        var (tokenB, _) = await RegisterAndLoginAsync();

        var connectionA = CreateHubConnection(tokenA);
        var connectionB = CreateHubConnection(tokenB);

        await connectionA.StartAsync();
        await connectionB.StartAsync();

        var received = false;
        connectionA.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, _ => received = true);

        // User A is connected but NOT in the salon
        await connectionB.InvokeAsync(ChatConstants.JoinSalonMethod, DefaultSalonName);

        // Act
        await connectionB.InvokeAsync(ChatConstants.SendMessageMethod, DefaultSalonName, TestMessage);

        // Assert - wait and verify no message was received
        await Task.Delay(NegativeTimeout);
        Assert.False(received, "User not in group should not receive messages.");
    }

    [Fact]
    public async Task LeaveSalon_StopsReceivingMessages()
    {
        // Arrange
        var (tokenA, _) = await RegisterAndLoginAsync();
        var (tokenB, _) = await RegisterAndLoginAsync();

        var connectionA = CreateHubConnection(tokenA);
        var connectionB = CreateHubConnection(tokenB);

        await connectionA.StartAsync();
        await connectionB.StartAsync();

        await connectionA.InvokeAsync(ChatConstants.JoinSalonMethod, DefaultSalonName);
        await connectionB.InvokeAsync(ChatConstants.JoinSalonMethod, DefaultSalonName);

        // User A leaves the salon
        await connectionA.InvokeAsync(ChatConstants.LeaveSalonMethod, DefaultSalonName);

        var received = false;
        connectionA.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, _ => received = true);

        // Act
        await connectionB.InvokeAsync(ChatConstants.SendMessageMethod, DefaultSalonName, TestMessage);

        // Assert
        await Task.Delay(NegativeTimeout);
        Assert.False(received, "User who left the salon should not receive messages.");
    }

    [Fact]
    public async Task SendMessage_EmptyMessage_ThrowsHubException()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginAsync();
        var connection = CreateHubConnection(accessToken);

        await connection.StartAsync();
        await connection.InvokeAsync(ChatConstants.JoinSalonMethod, DefaultSalonName);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HubException>(
            () => connection.InvokeAsync(ChatConstants.SendMessageMethod, DefaultSalonName, ""));

        Assert.Contains(ChatConstants.MessageEmpty, exception.Message);
    }

    [Fact]
    public async Task SendMessage_MessageExceedsMaxLength_ThrowsHubException()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginAsync();
        var connection = CreateHubConnection(accessToken);

        await connection.StartAsync();
        await connection.InvokeAsync(ChatConstants.JoinSalonMethod, DefaultSalonName);

        var longMessage = new string('A', ChatConstants.MaxMessageLength + 1);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HubException>(
            () => connection.InvokeAsync(ChatConstants.SendMessageMethod, DefaultSalonName, longMessage));

        Assert.Contains(ChatConstants.MessageTooLong, exception.Message);
    }

    [Fact]
    public async Task Disconnect_ThenReconnect_CanStillSendAndReceive()
    {
        // Arrange
        var (accessToken, email) = await RegisterAndLoginAsync();
        var connection = CreateHubConnection(accessToken);

        await connection.StartAsync();
        await connection.InvokeAsync(ChatConstants.JoinSalonMethod, DefaultSalonName);
        await connection.StopAsync();

        Assert.Equal(HubConnectionState.Disconnected, connection.State);

        // Reconnect
        await connection.StartAsync();
        await connection.InvokeAsync(ChatConstants.JoinSalonMethod, DefaultSalonName);

        var tcs = new TaskCompletionSource<ChatMessageOutput>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, msg => tcs.SetResult(msg));

        // Act
        await connection.InvokeAsync(ChatConstants.SendMessageMethod, DefaultSalonName, TestMessage);

        // Assert
        var received = await tcs.Task.WaitAsync(ReceiveTimeout);
        Assert.Equal(email, received.SenderEmail);
        Assert.Equal(TestMessage, received.Message);
    }

    [Fact]
    public async Task Messages_AreIsolatedBetweenSalons()
    {
        // Arrange
        var (tokenA, _) = await RegisterAndLoginAsync();
        var (tokenB, _) = await RegisterAndLoginAsync();

        var connectionA = CreateHubConnection(tokenA);
        var connectionB = CreateHubConnection(tokenB);

        await connectionA.StartAsync();
        await connectionB.StartAsync();

        // User A joins "General", User B joins "France"
        await connectionA.InvokeAsync(ChatConstants.JoinSalonMethod, "General");
        await connectionB.InvokeAsync(ChatConstants.JoinSalonMethod, "France");

        var received = false;
        connectionA.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, _ => received = true);

        // Act - User B sends to "France"
        await connectionB.InvokeAsync(ChatConstants.SendMessageMethod, "France", TestMessage);

        // Assert - User A in "General" should NOT receive
        await Task.Delay(NegativeTimeout);
        Assert.False(received, "User in a different salon should not receive the message.");
    }

    [Fact]
    public async Task MultipleSalons_UsersInSameSalon_BothReceive()
    {
        // Arrange
        var (tokenA, _) = await RegisterAndLoginAsync();
        var (tokenB, emailB) = await RegisterAndLoginAsync();

        var connectionA = CreateHubConnection(tokenA);
        var connectionB = CreateHubConnection(tokenB);

        await connectionA.StartAsync();
        await connectionB.StartAsync();

        // Both join "France"
        await connectionA.InvokeAsync(ChatConstants.JoinSalonMethod, "France");
        await connectionB.InvokeAsync(ChatConstants.JoinSalonMethod, "France");

        var tcsA = new TaskCompletionSource<ChatMessageOutput>(TaskCreationOptions.RunContinuationsAsynchronously);
        var tcsB = new TaskCompletionSource<ChatMessageOutput>(TaskCreationOptions.RunContinuationsAsynchronously);
        connectionA.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, msg => tcsA.SetResult(msg));
        connectionB.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, msg => tcsB.SetResult(msg));

        // Act - User B sends to "France"
        await connectionB.InvokeAsync(ChatConstants.SendMessageMethod, "France", TestMessage);

        // Assert - Both should receive
        var receivedA = await tcsA.Task.WaitAsync(ReceiveTimeout);
        var receivedB = await tcsB.Task.WaitAsync(ReceiveTimeout);

        Assert.Equal(TestMessage, receivedA.Message);
        Assert.Equal(emailB, receivedA.SenderEmail);
        Assert.Equal(TestMessage, receivedB.Message);
        Assert.Equal(emailB, receivedB.SenderEmail);
    }

    [Fact]
    public async Task LeaveSalon_OnlyLeavesSpecifiedSalon()
    {
        // Arrange
        var (tokenA, _) = await RegisterAndLoginAsync();
        var (tokenB, emailB) = await RegisterAndLoginAsync();

        var connectionA = CreateHubConnection(tokenA);
        var connectionB = CreateHubConnection(tokenB);

        await connectionA.StartAsync();
        await connectionB.StartAsync();

        // User A joins both "General" and "France"
        await connectionA.InvokeAsync(ChatConstants.JoinSalonMethod, "General");
        await connectionA.InvokeAsync(ChatConstants.JoinSalonMethod, "France");

        // User B joins "France" to send a message there
        await connectionB.InvokeAsync(ChatConstants.JoinSalonMethod, "France");

        // User A leaves "General" only
        await connectionA.InvokeAsync(ChatConstants.LeaveSalonMethod, "General");

        var tcs = new TaskCompletionSource<ChatMessageOutput>(TaskCreationOptions.RunContinuationsAsynchronously);
        connectionA.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, msg => tcs.SetResult(msg));

        // Act - User B sends to "France"
        await connectionB.InvokeAsync(ChatConstants.SendMessageMethod, "France", TestMessage);

        // Assert - User A should still receive on "France"
        var received = await tcs.Task.WaitAsync(ReceiveTimeout);
        Assert.Equal(TestMessage, received.Message);
        Assert.Equal(emailB, received.SenderEmail);
    }
}
