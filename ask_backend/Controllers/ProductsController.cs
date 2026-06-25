using ASK.Application.DTOs.Product;
using ASK.Application.Features.Products.Commands.CreateProduct;
using ASK.Application.Features.Products.Commands.DeleteProduct;
using ASK.Application.Features.Products.Commands.UpdateProduct;
using ASK.Application.Features.Products.Queries.GetFeaturedProducts;
using ASK.Application.Features.Products.Queries.GetLowStockProducts;
using ASK.Application.Features.Products.Queries.GetNewProducts;
using ASK.Application.Features.Products.Queries.GetProductBySlug;
using ASK.Application.Features.Products.Queries.GetProducts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ask_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(IMediator mediator) : ControllerBase
{
    /// <summary>Filtreleme ve sayfalama ile ürün listesi.</summary>
    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int? categoryId,
        [FromQuery] int? brandId,
        [FromQuery] bool? isNew,
        [FromQuery] bool? isFeatured,
        [FromQuery] bool? inStockOnly,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 12,
        CancellationToken cancellationToken = default)
    {
        // Limit injection koruması
        limit = Math.Clamp(limit, 1, 100);
        page = Math.Max(1, page);

        var result = await mediator.Send(
            new GetProductsQuery(categoryId, brandId, isNew, isFeatured, inStockOnly, search, page, limit),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>Öne çıkan ürünler.</summary>
    [HttpGet("featured")]
    public async Task<IActionResult> GetFeatured(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetFeaturedProductsQuery(), cancellationToken);
        return Ok(new { success = true, data = result });
    }

    /// <summary>Yeni ürünler.</summary>
    [HttpGet("new")]
    public async Task<IActionResult> GetNew(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetNewProductsQuery(), cancellationToken);
        return Ok(new { success = true, data = result });
    }

    /// <summary>Stok tükenmek üzere olan ürünler (stok ≤ maxStock).</summary>
    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock(
        [FromQuery] int maxStock = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetLowStockProductsQuery(maxStock), cancellationToken);
        return Ok(new { success = true, data = result });
    }

    /// <summary>Slug ile ürün detayı.</summary>
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetProductBySlugQuery(slug), cancellationToken);
        return Ok(new { success = true, data = result });
    }

    /// <summary>Yeni ürün ekle. [Admin]</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateProductCommand(dto), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { success = true, data = result });
    }

    /// <summary>Ürün güncelle. [Admin]</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateProductCommand(id, dto), cancellationToken);
        return Ok(new { success = true, data = result });
    }

    /// <summary>Ürün pasif yap (soft delete). [Admin]</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteProductCommand(id), cancellationToken);
        return Ok(new { success = true, message = "Ürün pasif yapıldı." });
    }
}
