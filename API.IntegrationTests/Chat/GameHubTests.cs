namespace BackBase.API.IntegrationTests.Chat;

using BackBase.API.IntegrationTests.Fixtures;
using BackBase.Application.Constants;
using BackBase.Application.DTOs.Output;
using BackBase.Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

public sealed class GameHubTests : SignalRTestBase
{
    private const string TestMessage = "Hello, world!";
    private const string DefaultChannelId = "General";
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
    public async Task JoinChannel_ThenReceiveMessage_FromAnotherUser()
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

        await connectionA.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, DefaultChannelId);
        await connectionB.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, DefaultChannelId);

        // Act
        await connectionB.InvokeAsync(ChatConstants.SendMessageMethod, ChannelType.Global, DefaultChannelId, TestMessage);

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

        await connection.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, DefaultChannelId);

        // Act
        await connection.InvokeAsync(ChatConstants.SendMessageMethod, ChannelType.Global, DefaultChannelId, TestMessage);

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

        // User A is connected but NOT in the channel
        await connectionB.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, DefaultChannelId);

        // Act
        await connectionB.InvokeAsync(ChatConstants.SendMessageMethod, ChannelType.Global, DefaultChannelId, TestMessage);

        // Assert - wait and verify no message was received
        await Task.Delay(NegativeTimeout);
        Assert.False(received, "User not in group should not receive messages.");
    }

    [Fact]
    public async Task LeaveChannel_StopsReceivingMessages()
    {
        // Arrange
        var (tokenA, _) = await RegisterAndLoginAsync();
        var (tokenB, _) = await RegisterAndLoginAsync();

        var connectionA = CreateHubConnection(tokenA);
        var connectionB = CreateHubConnection(tokenB);

        await connectionA.StartAsync();
        await connectionB.StartAsync();

        await connectionA.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, DefaultChannelId);
        await connectionB.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, DefaultChannelId);

        // User A leaves the channel
        await connectionA.InvokeAsync(ChannelConstants.LeaveChannelMethod, ChannelType.Global, DefaultChannelId);

        var received = false;
        connectionA.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, _ => received = true);

        // Act
        await connectionB.InvokeAsync(ChatConstants.SendMessageMethod, ChannelType.Global, DefaultChannelId, TestMessage);

        // Assert
        await Task.Delay(NegativeTimeout);
        Assert.False(received, "User who left the channel should not receive messages.");
    }

    [Fact]
    public async Task SendMessage_EmptyMessage_ThrowsHubException()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginAsync();
        var connection = CreateHubConnection(accessToken);

        await connection.StartAsync();
        await connection.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, DefaultChannelId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HubException>(
            () => connection.InvokeAsync(ChatConstants.SendMessageMethod, ChannelType.Global, DefaultChannelId, ""));

        Assert.Contains(ChatConstants.MessageEmpty, exception.Message);
    }

    [Fact]
    public async Task SendMessage_MessageExceedsMaxLength_ThrowsHubException()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginAsync();
        var connection = CreateHubConnection(accessToken);

        await connection.StartAsync();
        await connection.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, DefaultChannelId);

        var longMessage = new string('A', ChatConstants.MaxMessageLength + 1);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HubException>(
            () => connection.InvokeAsync(ChatConstants.SendMessageMethod, ChannelType.Global, DefaultChannelId, longMessage));

        Assert.Contains(ChatConstants.MessageTooLong, exception.Message);
    }

    [Fact]
    public async Task Disconnect_ThenReconnect_CanStillSendAndReceive()
    {
        // Arrange
        var (accessToken, email) = await RegisterAndLoginAsync();
        var connection = CreateHubConnection(accessToken);

        await connection.StartAsync();
        await connection.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, DefaultChannelId);
        await connection.StopAsync();

        Assert.Equal(HubConnectionState.Disconnected, connection.State);

        // Reconnect
        await connection.StartAsync();
        await connection.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, DefaultChannelId);

        var tcs = new TaskCompletionSource<ChatMessageOutput>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, msg => tcs.SetResult(msg));

        // Act
        await connection.InvokeAsync(ChatConstants.SendMessageMethod, ChannelType.Global, DefaultChannelId, TestMessage);

        // Assert
        var received = await tcs.Task.WaitAsync(ReceiveTimeout);
        Assert.Equal(email, received.SenderEmail);
        Assert.Equal(TestMessage, received.Message);
    }

    [Fact]
    public async Task Messages_AreIsolatedBetweenChannels()
    {
        // Arrange
        var (tokenA, _) = await RegisterAndLoginAsync();
        var (tokenB, _) = await RegisterAndLoginAsync();

        var connectionA = CreateHubConnection(tokenA);
        var connectionB = CreateHubConnection(tokenB);

        await connectionA.StartAsync();
        await connectionB.StartAsync();

        // User A joins "General", User B joins "France"
        await connectionA.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, "General");
        await connectionB.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, "France");

        var received = false;
        connectionA.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, _ => received = true);

        // Act - User B sends to "France"
        await connectionB.InvokeAsync(ChatConstants.SendMessageMethod, ChannelType.Global, "France", TestMessage);

        // Assert - User A in "General" should NOT receive
        await Task.Delay(NegativeTimeout);
        Assert.False(received, "User in a different channel should not receive the message.");
    }

    [Fact]
    public async Task MultipleChannels_UsersInSameChannel_BothReceive()
    {
        // Arrange
        var (tokenA, _) = await RegisterAndLoginAsync();
        var (tokenB, emailB) = await RegisterAndLoginAsync();

        var connectionA = CreateHubConnection(tokenA);
        var connectionB = CreateHubConnection(tokenB);

        await connectionA.StartAsync();
        await connectionB.StartAsync();

        // Both join "France"
        await connectionA.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, "France");
        await connectionB.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, "France");

        var tcsA = new TaskCompletionSource<ChatMessageOutput>(TaskCreationOptions.RunContinuationsAsynchronously);
        var tcsB = new TaskCompletionSource<ChatMessageOutput>(TaskCreationOptions.RunContinuationsAsynchronously);
        connectionA.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, msg => tcsA.SetResult(msg));
        connectionB.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, msg => tcsB.SetResult(msg));

        // Act - User B sends to "France"
        await connectionB.InvokeAsync(ChatConstants.SendMessageMethod, ChannelType.Global, "France", TestMessage);

        // Assert - Both should receive
        var receivedA = await tcsA.Task.WaitAsync(ReceiveTimeout);
        var receivedB = await tcsB.Task.WaitAsync(ReceiveTimeout);

        Assert.Equal(TestMessage, receivedA.Message);
        Assert.Equal(emailB, receivedA.SenderEmail);
        Assert.Equal(TestMessage, receivedB.Message);
        Assert.Equal(emailB, receivedB.SenderEmail);
    }

    [Fact]
    public async Task LeaveChannel_OnlyLeavesSpecifiedChannel()
    {
        // Arrange
        var (tokenA, _) = await RegisterAndLoginAsync();
        var (tokenB, emailB) = await RegisterAndLoginAsync();

        var connectionA = CreateHubConnection(tokenA);
        var connectionB = CreateHubConnection(tokenB);

        await connectionA.StartAsync();
        await connectionB.StartAsync();

        // User A joins both "General" and "France"
        await connectionA.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, "General");
        await connectionA.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, "France");

        // User B joins "France" to send a message there
        await connectionB.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, "France");

        // User A leaves "General" only
        await connectionA.InvokeAsync(ChannelConstants.LeaveChannelMethod, ChannelType.Global, "General");

        var tcs = new TaskCompletionSource<ChatMessageOutput>(TaskCreationOptions.RunContinuationsAsynchronously);
        connectionA.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, msg => tcs.SetResult(msg));

        // Act - User B sends to "France"
        await connectionB.InvokeAsync(ChatConstants.SendMessageMethod, ChannelType.Global, "France", TestMessage);

        // Assert - User A should still receive on "France"
        var received = await tcs.Task.WaitAsync(ReceiveTimeout);
        Assert.Equal(TestMessage, received.Message);
        Assert.Equal(emailB, received.SenderEmail);
    }
}
