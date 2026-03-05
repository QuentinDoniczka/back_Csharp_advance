namespace BackBase.API.IntegrationTests.Chat;

using BackBase.API.IntegrationTests.Fixtures;
using BackBase.Application.Constants;
using BackBase.Application.DTOs.Output;
using BackBase.Domain.Enums;
using Microsoft.AspNetCore.SignalR.Client;

[Trait("Category", "Integration")]
public sealed class ChannelScenarioTests : SignalRTestBase
{
    private const string TestMessage = "scenario-test-message";
    private static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan NegativeTimeout = TimeSpan.FromSeconds(2);

    public ChannelScenarioTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task MultiUserGroupChat_AllUsersReceiveMessagesFromEachOther()
    {
        // Arrange
        var channelId = $"multi-user-{Guid.NewGuid():N}";

        var (tokenA, emailA) = await RegisterAndLoginAsync();
        var (tokenB, emailB) = await RegisterAndLoginAsync();
        var (tokenC, emailC) = await RegisterAndLoginAsync();

        var connectionA = CreateHubConnection(tokenA);
        var connectionB = CreateHubConnection(tokenB);
        var connectionC = CreateHubConnection(tokenC);

        await connectionA.StartAsync();
        await connectionB.StartAsync();
        await connectionC.StartAsync();

        await connectionA.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, channelId);
        await connectionB.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, channelId);
        await connectionC.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, channelId);

        // Act & Assert: User A sends a message - B and C receive it
        var tcsB1 = CreateMessageCompletionSource();
        var tcsC1 = CreateMessageCompletionSource();
        connectionB.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, msg => tcsB1.TrySetResult(msg));
        connectionC.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, msg => tcsC1.TrySetResult(msg));

        const string messageFromA = "Hello from A";
        await connectionA.InvokeAsync(ChatConstants.SendMessageMethod, ChannelType.Global, channelId, messageFromA);

        var receivedByB = await tcsB1.Task.WaitAsync(ReceiveTimeout);
        var receivedByC = await tcsC1.Task.WaitAsync(ReceiveTimeout);

        Assert.Equal(emailA, receivedByB.SenderEmail);
        Assert.Equal(messageFromA, receivedByB.Message);
        Assert.Equal(emailA, receivedByC.SenderEmail);
        Assert.Equal(messageFromA, receivedByC.Message);

        // Reset handlers for next round
        ClearMessageHandlers(connectionB);
        ClearMessageHandlers(connectionC);

        // Act & Assert: User B sends a message - A and C receive it
        var tcsA2 = CreateMessageCompletionSource();
        var tcsC2 = CreateMessageCompletionSource();
        connectionA.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, msg => tcsA2.TrySetResult(msg));
        connectionC.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, msg => tcsC2.TrySetResult(msg));

        const string messageFromB = "Hello from B";
        await connectionB.InvokeAsync(ChatConstants.SendMessageMethod, ChannelType.Global, channelId, messageFromB);

        var receivedByA = await tcsA2.Task.WaitAsync(ReceiveTimeout);
        var receivedByC2 = await tcsC2.Task.WaitAsync(ReceiveTimeout);

        Assert.Equal(emailB, receivedByA.SenderEmail);
        Assert.Equal(messageFromB, receivedByA.Message);
        Assert.Equal(emailB, receivedByC2.SenderEmail);
        Assert.Equal(messageFromB, receivedByC2.Message);

        // Reset handlers for next round
        ClearMessageHandlers(connectionA);
        ClearMessageHandlers(connectionC);

        // Act & Assert: User C sends a message - A and B receive it
        var tcsA3 = CreateMessageCompletionSource();
        var tcsB3 = CreateMessageCompletionSource();
        connectionA.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, msg => tcsA3.TrySetResult(msg));
        connectionB.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, msg => tcsB3.TrySetResult(msg));

        const string messageFromC = "Hello from C";
        await connectionC.InvokeAsync(ChatConstants.SendMessageMethod, ChannelType.Global, channelId, messageFromC);

        var receivedByA3 = await tcsA3.Task.WaitAsync(ReceiveTimeout);
        var receivedByB3 = await tcsB3.Task.WaitAsync(ReceiveTimeout);

        Assert.Equal(emailC, receivedByA3.SenderEmail);
        Assert.Equal(messageFromC, receivedByA3.Message);
        Assert.Equal(emailC, receivedByB3.SenderEmail);
        Assert.Equal(messageFromC, receivedByB3.Message);
    }

    [Fact]
    public async Task UserLeavesChannel_StopsReceivingButOthersStillDo()
    {
        // Arrange
        var channelId = $"leave-channel-{Guid.NewGuid():N}";

        var (tokenA, emailA) = await RegisterAndLoginAsync();
        var (tokenB, _) = await RegisterAndLoginAsync();
        var (tokenC, _) = await RegisterAndLoginAsync();

        var connectionA = CreateHubConnection(tokenA);
        var connectionB = CreateHubConnection(tokenB);
        var connectionC = CreateHubConnection(tokenC);

        await connectionA.StartAsync();
        await connectionB.StartAsync();
        await connectionC.StartAsync();

        await connectionA.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, channelId);
        await connectionB.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, channelId);
        await connectionC.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, channelId);

        // User C leaves the channel but stays connected
        await connectionC.InvokeAsync(ChannelConstants.LeaveChannelMethod, ChannelType.Global, channelId);
        Assert.Equal(HubConnectionState.Connected, connectionC.State);

        // Set up listeners
        var tcsB = CreateMessageCompletionSource();
        var receivedByC = false;

        connectionB.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, msg => tcsB.TrySetResult(msg));
        connectionC.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, _ => receivedByC = true);

        // Act - User A sends a message
        await connectionA.InvokeAsync(ChatConstants.SendMessageMethod, ChannelType.Global, channelId, TestMessage);

        // Assert - User B receives it
        var messageForB = await tcsB.Task.WaitAsync(ReceiveTimeout);
        Assert.Equal(emailA, messageForB.SenderEmail);
        Assert.Equal(TestMessage, messageForB.Message);

        // Assert - User C does NOT receive it (wait to be sure)
        await Task.Delay(NegativeTimeout);
        Assert.False(receivedByC, "User who left the channel should not receive messages.");
    }

    [Fact]
    public async Task UserDisconnectsCompletely_OthersStillReceiveMessages()
    {
        // Arrange
        var channelId = $"disconnect-{Guid.NewGuid():N}";

        var (tokenA, emailA) = await RegisterAndLoginAsync();
        var (tokenB, _) = await RegisterAndLoginAsync();
        var (tokenC, _) = await RegisterAndLoginAsync();

        var connectionA = CreateHubConnection(tokenA);
        var connectionB = CreateHubConnection(tokenB);
        var connectionC = CreateHubConnection(tokenC);

        await connectionA.StartAsync();
        await connectionB.StartAsync();
        await connectionC.StartAsync();

        await connectionA.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, channelId);
        await connectionB.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, channelId);
        await connectionC.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, channelId);

        // User C disconnects completely
        await connectionC.StopAsync();
        Assert.Equal(HubConnectionState.Disconnected, connectionC.State);

        // Set up listener on B
        var tcsB = CreateMessageCompletionSource();
        connectionB.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, msg => tcsB.TrySetResult(msg));

        // Act - User A sends a message (should not error despite disconnected member)
        await connectionA.InvokeAsync(ChatConstants.SendMessageMethod, ChannelType.Global, channelId, TestMessage);

        // Assert - User B still receives the message
        var messageForB = await tcsB.Task.WaitAsync(ReceiveTimeout);
        Assert.Equal(emailA, messageForB.SenderEmail);
        Assert.Equal(TestMessage, messageForB.Message);
    }

    [Fact]
    public async Task MultipleChannelsIsolation_MessagesDoNotLeakBetweenChannels()
    {
        // Arrange
        var lobbyChannel = $"lobby-{Guid.NewGuid():N}";
        var arenaChannel = $"arena-{Guid.NewGuid():N}";

        var (tokenA, emailA) = await RegisterAndLoginAsync();
        var (tokenB, emailB) = await RegisterAndLoginAsync();

        var connectionA = CreateHubConnection(tokenA);
        var connectionB = CreateHubConnection(tokenB);

        await connectionA.StartAsync();
        await connectionB.StartAsync();

        // User A joins lobby only, User B joins arena only
        await connectionA.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, lobbyChannel);
        await connectionB.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, arenaChannel);

        // Set up listeners
        var receivedByA = false;
        var receivedByB = false;
        connectionA.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, _ => receivedByA = true);
        connectionB.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, _ => receivedByB = true);

        // Act - User A sends to lobby
        await connectionA.InvokeAsync(ChatConstants.SendMessageMethod, ChannelType.Global, lobbyChannel, "Lobby message");

        // Assert - User B should NOT receive message from lobby
        await Task.Delay(NegativeTimeout);
        Assert.False(receivedByB, "User in arena should not receive lobby messages.");

        // Reset
        receivedByA = false;
        receivedByB = false;

        // Act - User B sends to arena
        await connectionB.InvokeAsync(ChatConstants.SendMessageMethod, ChannelType.Global, arenaChannel, "Arena message");

        // Assert - User A should NOT receive message from arena
        await Task.Delay(NegativeTimeout);
        Assert.False(receivedByA, "User in lobby should not receive arena messages.");
    }

    [Fact]
    public async Task UserInMultipleChannels_ReceivesOnlyFromJoinedChannels()
    {
        // Arrange
        var lobbyChannel = $"lobby-multi-{Guid.NewGuid():N}";
        var arenaChannel = $"arena-multi-{Guid.NewGuid():N}";

        var (tokenA, _) = await RegisterAndLoginAsync();
        var (tokenB, emailB) = await RegisterAndLoginAsync();
        var (tokenC, emailC) = await RegisterAndLoginAsync();

        var connectionA = CreateHubConnection(tokenA);
        var connectionB = CreateHubConnection(tokenB);
        var connectionC = CreateHubConnection(tokenC);

        await connectionA.StartAsync();
        await connectionB.StartAsync();
        await connectionC.StartAsync();

        // User A joins both channels
        await connectionA.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, lobbyChannel);
        await connectionA.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, arenaChannel);
        // User B joins lobby only
        await connectionB.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, lobbyChannel);
        // User C joins arena only
        await connectionC.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, arenaChannel);

        // --- Test: Message sent to lobby - A and B receive, C does not ---
        var tcsA_lobby = CreateMessageCompletionSource();
        var tcsB_lobby = CreateMessageCompletionSource();
        var receivedByC_lobby = false;

        connectionA.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, msg => tcsA_lobby.TrySetResult(msg));
        connectionB.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, msg => tcsB_lobby.TrySetResult(msg));
        connectionC.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, _ => receivedByC_lobby = true);

        const string lobbyMessage = "Hello lobby";
        await connectionB.InvokeAsync(ChatConstants.SendMessageMethod, ChannelType.Global, lobbyChannel, lobbyMessage);

        var receivedByA = await tcsA_lobby.Task.WaitAsync(ReceiveTimeout);
        var receivedByB = await tcsB_lobby.Task.WaitAsync(ReceiveTimeout);

        Assert.Equal(emailB, receivedByA.SenderEmail);
        Assert.Equal(lobbyMessage, receivedByA.Message);
        Assert.Equal(emailB, receivedByB.SenderEmail);
        Assert.Equal(lobbyMessage, receivedByB.Message);

        await Task.Delay(NegativeTimeout);
        Assert.False(receivedByC_lobby, "User C in arena only should not receive lobby messages.");

        // Reset handlers
        ClearMessageHandlers(connectionA);
        ClearMessageHandlers(connectionB);
        ClearMessageHandlers(connectionC);

        // --- Test: Message sent to arena - A and C receive, B does not ---
        var tcsA_arena = CreateMessageCompletionSource();
        var tcsC_arena = CreateMessageCompletionSource();
        var receivedByB_arena = false;

        connectionA.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, msg => tcsA_arena.TrySetResult(msg));
        connectionC.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, msg => tcsC_arena.TrySetResult(msg));
        connectionB.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, _ => receivedByB_arena = true);

        const string arenaMessage = "Hello arena";
        await connectionC.InvokeAsync(ChatConstants.SendMessageMethod, ChannelType.Global, arenaChannel, arenaMessage);

        var receivedByA2 = await tcsA_arena.Task.WaitAsync(ReceiveTimeout);
        var receivedByC2 = await tcsC_arena.Task.WaitAsync(ReceiveTimeout);

        Assert.Equal(emailC, receivedByA2.SenderEmail);
        Assert.Equal(arenaMessage, receivedByA2.Message);
        Assert.Equal(emailC, receivedByC2.SenderEmail);
        Assert.Equal(arenaMessage, receivedByC2.Message);

        await Task.Delay(NegativeTimeout);
        Assert.False(receivedByB_arena, "User B in lobby only should not receive arena messages.");
    }

    [Fact]
    public async Task RejoinAfterLeave_UserReceivesMessagesAgain()
    {
        // Arrange
        var channelId = $"rejoin-{Guid.NewGuid():N}";

        var (tokenA, _) = await RegisterAndLoginAsync();
        var (tokenB, emailB) = await RegisterAndLoginAsync();

        var connectionA = CreateHubConnection(tokenA);
        var connectionB = CreateHubConnection(tokenB);

        await connectionA.StartAsync();
        await connectionB.StartAsync();

        // User A joins, then leaves, then rejoins
        await connectionA.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, channelId);
        await connectionA.InvokeAsync(ChannelConstants.LeaveChannelMethod, ChannelType.Global, channelId);

        // Verify user A does NOT receive while not in channel
        var receivedWhileLeft = false;
        connectionA.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, _ => receivedWhileLeft = true);

        await connectionB.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, channelId);
        await connectionB.InvokeAsync(ChatConstants.SendMessageMethod, ChannelType.Global, channelId, "should not arrive");

        await Task.Delay(NegativeTimeout);
        Assert.False(receivedWhileLeft, "User who left should not receive messages before rejoining.");

        // User A rejoins
        ClearMessageHandlers(connectionA);
        await connectionA.InvokeAsync(ChannelConstants.JoinChannelMethod, ChannelType.Global, channelId);

        var tcsA = CreateMessageCompletionSource();
        connectionA.On<ChatMessageOutput>(ChatConstants.ReceiveMessageMethod, msg => tcsA.TrySetResult(msg));

        // Act - User B sends a message after User A has rejoined
        const string rejoinMessage = "Welcome back!";
        await connectionB.InvokeAsync(ChatConstants.SendMessageMethod, ChannelType.Global, channelId, rejoinMessage);

        // Assert - User A receives the message
        var received = await tcsA.Task.WaitAsync(ReceiveTimeout);
        Assert.Equal(emailB, received.SenderEmail);
        Assert.Equal(rejoinMessage, received.Message);
    }

    private static TaskCompletionSource<ChatMessageOutput> CreateMessageCompletionSource()
    {
        return new TaskCompletionSource<ChatMessageOutput>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private static void ClearMessageHandlers(HubConnection connection)
    {
        connection.Remove(ChatConstants.ReceiveMessageMethod);
    }
}
