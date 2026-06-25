using ASK.Application.DTOs.Brand;
using ASK.Application.Features.Brands.Commands.CreateBrand;
using ASK.Application.Features.Brands.Commands.DeleteBrand;
using ASK.Application.Features.Brands.Commands.UpdateBrand;
using ASK.Application.Features.Brands.Queries.GetBrands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ask_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BrandsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetBrandsQuery(), cancellationToken);
        return Ok(new { success = true, data = result });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateBrandDto dto, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateBrandCommand(dto.Name, dto.LogoUrl, dto.Website), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { success = true, data = result });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBrandDto dto, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateBrandCommand(id, dto.Name, dto.LogoUrl, dto.Website, dto.IsActive), cancellationToken);
        return Ok(new { success = true, data = result });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteBrandCommand(id), cancellationToken);
        return Ok(new { success = true, message = "Marka silindi." });
    }
}
