namespace BackBase.Application.Tests.Commands.SendChatMessage;

using BackBase.Application.Commands.SendChatMessage;
using BackBase.Application.Constants;
using FluentValidation.TestHelper;

public sealed class SendChatMessageCommandValidatorTests
{
    private readonly SendChatMessageCommandValidator _validator;

    private static readonly Guid ValidSenderUserId = Guid.NewGuid();
    private const string ValidSenderEmail = "player@example.com";
    private const string ValidChannelName = "global:General";
    private const string ValidMessage = "Hello, world!";

    public SendChatMessageCommandValidatorTests()
    {
        _validator = new SendChatMessageCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new SendChatMessageCommand(ValidSenderUserId, ValidSenderEmail, ValidChannelName, ValidMessage);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyMessage_ShouldFail()
    {
        // Arrange
        var command = new SendChatMessageCommand(ValidSenderUserId, ValidSenderEmail, ValidChannelName, "");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Message)
            .WithErrorMessage(ChatConstants.MessageEmpty);
    }

    [Fact]
    public void Validate_MessageExceedsMaxLength_ShouldFail()
    {
        // Arrange
        var longMessage = new string('A', ChatConstants.MaxMessageLength + 1);
        var command = new SendChatMessageCommand(ValidSenderUserId, ValidSenderEmail, ValidChannelName, longMessage);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Message)
            .WithErrorMessage(ChatConstants.MessageTooLong);
    }

    [Fact]
    public void Validate_EmptySenderUserId_ShouldFail()
    {
        // Arrange
        var command = new SendChatMessageCommand(Guid.Empty, ValidSenderEmail, ValidChannelName, ValidMessage);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SenderUserId);
    }

    [Fact]
    public void Validate_EmptySenderEmail_ShouldFail()
    {
        // Arrange
        var command = new SendChatMessageCommand(ValidSenderUserId, "", ValidChannelName, ValidMessage);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SenderEmail);
    }

    [Fact]
    public void Validate_EmptyChannelName_ShouldFail()
    {
        // Arrange
        var command = new SendChatMessageCommand(ValidSenderUserId, ValidSenderEmail, "", ValidMessage);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ChannelName)
            .WithErrorMessage(ChannelConstants.ChannelNameEmpty);
    }

    [Fact]
    public void Validate_ChannelNameExceedsMaxLength_ShouldFail()
    {
        // Arrange
        var longChannelName = new string('A', ChannelConstants.ChannelNameMaxLength + 1);
        var command = new SendChatMessageCommand(ValidSenderUserId, ValidSenderEmail, longChannelName, ValidMessage);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ChannelName)
            .WithErrorMessage(ChannelConstants.ChannelNameTooLong);
    }
}
