namespace BackBase.Application.Tests.Commands.SendChatMessage;

using BackBase.Application.Commands.SendChatMessage;
using BackBase.Application.Constants;
using BackBase.Application.DTOs.Output;
using BackBase.Application.Interfaces;
using NSubstitute;

public sealed class SendChatMessageCommandHandlerTests
{
    private readonly IChatNotificationService _chatNotificationService;
    private readonly SendChatMessageCommandHandler _handler;

    private static readonly Guid ValidSenderUserId = Guid.NewGuid();
    private const string ValidSenderEmail = "player@example.com";
    private const string ValidMessage = "Hello, world!";

    public SendChatMessageCommandHandlerTests()
    {
        _chatNotificationService = Substitute.For<IChatNotificationService>();
        _handler = new SendChatMessageCommandHandler(_chatNotificationService);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsChatMessageOutputWithCorrectData()
    {
        // Arrange
        var command = new SendChatMessageCommand(ValidSenderUserId, ValidSenderEmail, ValidMessage);
        var beforeUtc = DateTime.UtcNow;

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        var afterUtc = DateTime.UtcNow;
        Assert.Equal(ValidSenderUserId, result.SenderUserId);
        Assert.Equal(ValidSenderEmail, result.SenderEmail);
        Assert.Equal(ValidMessage, result.Message);
        Assert.InRange(result.SentAt, beforeUtc, afterUtc);
    }

    [Fact]
    public async Task Handle_ValidCommand_BroadcastsToGlobalChatGroup()
    {
        // Arrange
        var command = new SendChatMessageCommand(ValidSenderUserId, ValidSenderEmail, ValidMessage);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _chatNotificationService
            .Received(1)
            .BroadcastToGroupAsync(
                ChatConstants.GlobalChatGroup,
                Arg.Any<ChatMessageOutput>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_BroadcastsCorrectMessage()
    {
        // Arrange
        var command = new SendChatMessageCommand(ValidSenderUserId, ValidSenderEmail, ValidMessage);
        ChatMessageOutput? broadcastedMessage = null;

        _chatNotificationService
            .BroadcastToGroupAsync(
                Arg.Any<string>(),
                Arg.Do<ChatMessageOutput>(msg => broadcastedMessage = msg),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(broadcastedMessage);
        Assert.Equal(result.SenderUserId, broadcastedMessage.SenderUserId);
        Assert.Equal(result.SenderEmail, broadcastedMessage.SenderEmail);
        Assert.Equal(result.Message, broadcastedMessage.Message);
        Assert.Equal(result.SentAt, broadcastedMessage.SentAt);
    }
}
