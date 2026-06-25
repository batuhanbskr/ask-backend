using ASK.Application.Common.Interfaces;
using ASK.Application.DTOs.Cart;
using ASK.Application.Features.Cart.Commands.AddToCart;
using ASK.Application.Features.Cart.Commands.ClearCart;
using ASK.Application.Features.Cart.Commands.RemoveFromCart;
using ASK.Application.Features.Cart.Commands.UpdateCartItem;
using ASK.Application.Features.Cart.Queries.GetCart;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ask_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController(IMediator mediator, ICurrentUserService currentUser) : ControllerBase
{
    private int UserId => currentUser.UserId ?? 0;

    [HttpGet]
    public async Task<IActionResult> GetCart(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCartQuery(UserId), cancellationToken);
        return Ok(new { success = true, data = result });
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddToCartDto dto, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AddToCartCommand(UserId, dto.ProductId, dto.Quantity), cancellationToken);
        return Ok(new { success = true, data = result });
    }

    [HttpPut("items/{itemId:int}")]
    public async Task<IActionResult> UpdateItem(int itemId, [FromBody] UpdateCartItemDto dto, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateCartItemCommand(UserId, itemId, dto.Quantity), cancellationToken);
        return Ok(new { success = true, data = result });
    }

    [HttpDelete("items/{itemId:int}")]
    public async Task<IActionResult> RemoveItem(int itemId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RemoveFromCartCommand(UserId, itemId), cancellationToken);
        return Ok(new { success = true, data = result });
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ClearCartCommand(UserId), cancellationToken);
        return Ok(new { success = true, data = result });
    }
}
