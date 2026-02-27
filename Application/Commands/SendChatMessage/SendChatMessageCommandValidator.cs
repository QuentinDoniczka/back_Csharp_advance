using BackBase.Application.Constants;
using FluentValidation;

namespace BackBase.Application.Commands.SendChatMessage;

public sealed class SendChatMessageCommandValidator : AbstractValidator<SendChatMessageCommand>
{
    public SendChatMessageCommandValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage(ChatConstants.MessageEmpty)
            .MaximumLength(ChatConstants.MaxMessageLength).WithMessage(ChatConstants.MessageTooLong);

        RuleFor(x => x.SenderUserId)
            .NotEmpty();

        RuleFor(x => x.SenderEmail)
            .NotEmpty();
    }
}
