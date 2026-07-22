using ASK.Application.Common.Interfaces;
using ASK.Application.DTOs.Order;
using ASK.Application.Features.Orders.Commands.CreateOrder;
using ASK.Application.Features.Orders.Commands.UpdateOrderStatus;
using ASK.Application.Features.Orders.Queries.GetOrderById;
using ASK.Application.Features.Orders.Queries.GetOrders;
using ASK.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ask_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController(IMediator mediator, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Siparişleri listeler. Kullanıcı yalnızca kendi siparişlerini görür. Admin için /api/admin/orders kullanın.</summary>
    [HttpGet]
    public async Task<IActionResult> GetOrders(CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? 0;

        var result = await mediator.Send(new GetOrdersQuery(userId, false), cancellationToken);
        return Ok(new { success = true, data = result });
    }

    /// <summary>Sipariş detayı.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetOrderByIdQuery(id, currentUser.UserId ?? 0, currentUser.Role == "Admin" || currentUser.Role == "SuperAdmin"),
            cancellationToken);
        return Ok(new { success = true, data = result });
    }

    /// <summary>Sepetteki ürünlerden sipariş oluşturur.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateOrderCommand(currentUser.UserId ?? 0, dto.ShippingAddress, dto.Notes),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { success = true, data = result });
    }

    /// <summary>Sipariş durumunu günceller. [Admin]</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateOrderStatusCommand(id, dto.Status), cancellationToken);
        return Ok(new { success = true, data = result });
    }

    /// <summary>Kullanıcının kendi beklemedeki siparişini iptal eder.</summary>
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> CancelOrder(int id, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? 0;
        var order = await mediator.Send(
            new GetOrderByIdQuery(id, userId, false), cancellationToken);

        if (order == null)
            return NotFound(new { success = false, message = "Sipariş bulunamadı." });

        if (order.Status != OrderStatus.Pending)
            return BadRequest(new { success = false, message = "Sadece 'Beklemede' durumundaki siparişler iptal edilebilir." });

        var result = await mediator.Send(new UpdateOrderStatusCommand(id, OrderStatus.Cancelled), cancellationToken);
        return Ok(new { success = true, message = "Sipariş başarıyla iptal edildi ve cari bakiyeniz güncellendi.", data = result });
    }
}
