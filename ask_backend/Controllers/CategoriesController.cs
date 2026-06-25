using ASK.Application.DTOs.Category;
using ASK.Application.Features.Categories.Commands.CreateCategory;
using ASK.Application.Features.Categories.Commands.DeleteCategory;
using ASK.Application.Features.Categories.Commands.UpdateCategory;
using ASK.Application.Features.Categories.Queries.GetCategories;
using ASK.Application.Features.Categories.Queries.GetCategoryBySlug;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ask_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController(IMediator mediator) : ControllerBase
{
    /// <summary>Tüm aktif kategorileri listeler.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCategoriesQuery(), cancellationToken);
        return Ok(new { success = true, data = result });
    }

    /// <summary>Slug ile kategori getirir.</summary>
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCategoryBySlugQuery(slug), cancellationToken);
        return Ok(new { success = true, data = result });
    }

    /// <summary>Yeni kategori oluşturur. [Admin]</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateCategoryCommand(dto.Name, dto.Slug, dto.Description, dto.Icon, dto.ParentCategoryId),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { success = true, data = result });
    }

    /// <summary>Kategori günceller. [Admin]</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryDto dto, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateCategoryCommand(id, dto.Name, dto.Slug, dto.Description, dto.Icon, dto.IsActive, dto.ParentCategoryId),
            cancellationToken);
        return Ok(new { success = true, data = result });
    }

    /// <summary>Kategori siler. [Admin]</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteCategoryCommand(id), cancellationToken);
        return Ok(new { success = true, message = "Kategori silindi." });
    }
}
