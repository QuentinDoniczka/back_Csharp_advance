namespace BackBase.API.IntegrationTests.Chat;

using BackBase.API.IntegrationTests.Fixtures;
using BackBase.Application.Constants;
using BackBase.Application.DTOs.Output;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

public sealed class ChatHubTests : SignalRTestBase
{
    private const string TestMessage = "Hello, world!";
    private static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan NegativeTimeout = TimeSpan.FromSeconds(2);

    public ChatHubTests(CustomWebApplicationFactory factory) : base(factory)
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
    public async Task JoinGlobalChat_ThenReceiveMessage_FromAnotherUser()
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

        await connectionA.InvokeAsync(ChatConstants.JoinGlobalChatMethod);
        await connectionB.InvokeAsync(ChatConstants.JoinGlobalChatMethod);

        // Act
        await connectionB.InvokeAsync(ChatConstants.SendMessageMethod, TestMessage);

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

        await connection.InvokeAsync(ChatConstants.JoinGlobalChatMethod);

        // Act
        await connection.InvokeAsync(ChatConstants.SendMessageMethod, TestMessage);

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

        // User A is connected but NOT in the group
        await connectionB.InvokeAsync(ChatConstants.JoinGlobalChatMethod);

        // Act
        await connectionB.InvokeAsync(ChatConstants.SendMessageMethod, TestMessage);

        // Assert - wait and verify no message was received
        await Task.Delay(NegativeTimeout);
        Assert.False(received, "User not in group should not receive messages.");
    }

    [Fact]
    public async Task LeaveGlobalChat_StopsReceivingMessages()
    {
        // Arrange
        var (tokenA, _) = await RegisterAndLoginAsync();
        var (tokenB, _) = await RegisterAndLoginAsync();

        var connectionA = CreateHubConnection(tokenA);
        var connectionB = CreateHubConnection(tokenB);

        await connectionA.StartAsync();
        await connectionB.StartAsync();

        await connectionA.InvokeAsync(ChatConstants.JoinGlobalChatMethod);
        await connectionB.InvokeAsync(ChatConstants.JoinGlobalChatMethod);

        // User A leaves the group
        await connectionA.InvokeAsync(ChatConstants.LeaveGlobalChatMethod);

        var received = false;
        connectionA.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, _ => received = true);

        // Act
        await connectionB.InvokeAsync(ChatConstants.SendMessageMethod, TestMessage);

        // Assert
        await Task.Delay(NegativeTimeout);
        Assert.False(received, "User who left the group should not receive messages.");
    }

    [Fact]
    public async Task SendMessage_EmptyMessage_ThrowsHubException()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginAsync();
        var connection = CreateHubConnection(accessToken);

        await connection.StartAsync();
        await connection.InvokeAsync(ChatConstants.JoinGlobalChatMethod);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HubException>(
            () => connection.InvokeAsync(ChatConstants.SendMessageMethod, ""));

        Assert.Contains(ChatConstants.MessageEmpty, exception.Message);
    }

    [Fact]
    public async Task SendMessage_MessageExceedsMaxLength_ThrowsHubException()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginAsync();
        var connection = CreateHubConnection(accessToken);

        await connection.StartAsync();
        await connection.InvokeAsync(ChatConstants.JoinGlobalChatMethod);

        var longMessage = new string('A', ChatConstants.MaxMessageLength + 1);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HubException>(
            () => connection.InvokeAsync(ChatConstants.SendMessageMethod, longMessage));

        Assert.Contains(ChatConstants.MessageTooLong, exception.Message);
    }

    [Fact]
    public async Task Disconnect_ThenReconnect_CanStillSendAndReceive()
    {
        // Arrange
        var (accessToken, email) = await RegisterAndLoginAsync();
        var connection = CreateHubConnection(accessToken);

        await connection.StartAsync();
        await connection.InvokeAsync(ChatConstants.JoinGlobalChatMethod);
        await connection.StopAsync();

        Assert.Equal(HubConnectionState.Disconnected, connection.State);

        // Reconnect
        await connection.StartAsync();
        await connection.InvokeAsync(ChatConstants.JoinGlobalChatMethod);

        var tcs = new TaskCompletionSource<ChatMessageOutput>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, msg => tcs.SetResult(msg));

        // Act
        await connection.InvokeAsync(ChatConstants.SendMessageMethod, TestMessage);

        // Assert
        var received = await tcs.Task.WaitAsync(ReceiveTimeout);
        Assert.Equal(email, received.SenderEmail);
        Assert.Equal(TestMessage, received.Message);
    }
}
