using ASK.Application.DTOs.Contact;
using ASK.Domain.Entities;
using ASK.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace ASK.Application.Features.Contact.Commands.SendContactMessage;

public record SendContactMessageCommand(
    string Name,
    string Email,
    string? Phone,
    string? Company,
    string Subject,
    string Message
) : IRequest<Unit>;

public class SendContactMessageCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<SendContactMessageCommand, Unit>
{
    public async Task<Unit> Handle(SendContactMessageCommand request, CancellationToken cancellationToken)
    {
        var message = new ContactMessage
        {
            Name = request.Name,
            Email = request.Email.ToLowerInvariant().Trim(),
            Phone = request.Phone,
            Company = request.Company,
            Subject = request.Subject,
            Message = request.Message
        };

        await unitOfWork.ContactMessages.AddAsync(message, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public class SendContactMessageCommandValidator : AbstractValidator<SendContactMessageCommand>
{
    public SendContactMessageCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Phone).MaximumLength(20).When(x => x.Phone is not null);
    }
}
