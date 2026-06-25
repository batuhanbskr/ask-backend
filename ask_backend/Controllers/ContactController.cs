using ASK.Application.DTOs.Contact;
using ASK.Application.Features.Contact.Commands.SendContactMessage;
using ASK.Application.Features.Contact.Queries.GetContactMessages;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ask_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController(IMediator mediator) : ControllerBase
{
    /// <summary>İletişim formu gönderir. Herkese açık.</summary>
    [HttpPost]
    public async Task<IActionResult> Send([FromBody] SendContactMessageDto dto, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new SendContactMessageCommand(dto.Name, dto.Email, dto.Phone, dto.Company, dto.Subject, dto.Message),
            cancellationToken);

        return Ok(new { success = true, message = "Mesajınız başarıyla gönderildi." });
    }

    /// <summary>Mesajları listeler. [Admin]</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetMessages([FromQuery] bool onlyUnread = false, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetContactMessagesQuery(onlyUnread), cancellationToken);
        return Ok(new { success = true, data = result });
    }
}
