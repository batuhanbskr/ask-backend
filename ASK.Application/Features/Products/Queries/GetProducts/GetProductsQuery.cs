using System.Text.Json;
using ASK.Application.Common.Models;
using ASK.Application.DTOs.Product;
using ASK.Domain.Interfaces;
using MediatR;

namespace ASK.Application.Features.Products.Queries.GetProducts;

public record GetProductsQuery(
    int? CategoryId,
    int? BrandId,
    bool? IsNew,
    bool? IsFeatured,
    string? Search,
    int Page = 1,
    int Limit = 12,
    bool ActiveOnly = true
) : IRequest<PaginatedResponse<ProductDto>>;

public class GetProductsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetProductsQuery, PaginatedResponse<ProductDto>>
{
    public async Task<PaginatedResponse<ProductDto>> Handle(
        GetProductsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await unitOfWork.Products.GetPagedAsync(
            request.CategoryId, request.BrandId, request.IsNew, request.IsFeatured,
            request.Search, request.Page, request.Limit, request.ActiveOnly, cancellationToken);

        var dtos = items.Select(MapToDto).ToList();
        return PaginatedResponse<ProductDto>.Ok(dtos, total, request.Page, request.Limit);
    }

    internal static ProductDto MapToDto(Domain.Entities.Product p) => new(
        p.Id, p.Name, p.Slug, p.Code, p.IntegrationCode, p.Barcode,
        p.ShortDescription, p.Description, p.ImageUrl,
        JsonSerializer.Deserialize<List<string>>(p.FeaturesJson) ?? [],
        JsonSerializer.Deserialize<Dictionary<string, string>>(p.SpecificationsJson) ?? [],
        p.Stock, p.Price, p.DiscountedPrice, p.Discount, p.TaxRate, p.Currency,
        p.IsNew, p.IsFeatured, p.Status,
        p.CategoryId, p.Category?.Name ?? string.Empty,
        p.BrandId, p.Brand?.Name ?? string.Empty
    );
}
