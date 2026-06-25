using ASK.Application.DTOs.Contact;
using ASK.Domain.Interfaces;
using MediatR;

namespace ASK.Application.Features.Contact.Queries.GetContactMessages;

public record GetContactMessagesQuery(bool OnlyUnread = false) : IRequest<List<ContactMessageDto>>;

public class GetContactMessagesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetContactMessagesQuery, List<ContactMessageDto>>
{
    public async Task<List<ContactMessageDto>> Handle(GetContactMessagesQuery request, CancellationToken cancellationToken)
    {
        var messages = request.OnlyUnread
            ? await unitOfWork.ContactMessages.FindAsync(m => !m.IsRead, cancellationToken)
            : await unitOfWork.ContactMessages.GetAllAsync(cancellationToken);

        return messages
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new ContactMessageDto(
                m.Id, m.Name, m.Email, m.Phone, m.Company,
                m.Subject, m.Message, m.IsRead, m.CreatedAt))
            .ToList();
    }
}
