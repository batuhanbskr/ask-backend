using System.Text.Json;
using ASK.Application.Common.Interfaces;
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
    bool? IsDealOfTheDay,
    bool? InStockOnly,
    string? Search,
    int Page = 1,
    int Limit = 12,
    bool ActiveOnly = true
) : IRequest<PaginatedResponse<ProductDto>>;

public class GetProductsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : IRequestHandler<GetProductsQuery, PaginatedResponse<ProductDto>>
{
    public async Task<PaginatedResponse<ProductDto>> Handle(
        GetProductsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await unitOfWork.Products.GetPagedAsync(
            request.CategoryId, request.BrandId, request.IsNew, request.IsFeatured, request.IsDealOfTheDay, request.InStockOnly,
            request.Search, request.Page, request.Limit, request.ActiveOnly, cancellationToken);

        List<Domain.Entities.UserCategoryDiscount> categoryDiscounts = [];
        decimal globalDiscountRate = 0;

        if (currentUser.IsAuthenticated && currentUser.UserId.HasValue)
        {
            var user = await unitOfWork.Users.GetByIdAsync(currentUser.UserId.Value, cancellationToken);
            if (user != null)
            {
                globalDiscountRate = user.GlobalDiscountRate;
                categoryDiscounts = user.CategoryDiscounts?.ToList() ?? [];
            }
        }

        var dtos = items.Select(p => {
            decimal rate = globalDiscountRate;
            var catDisc = categoryDiscounts.FirstOrDefault(cd => cd.CategoryId == p.CategoryId);
            if (catDisc != null && catDisc.DiscountRate > 0)
            {
                rate = catDisc.DiscountRate;
            }
            else if (p.Category != null && p.Category.ParentCategoryId.HasValue)
            {
                var parentDisc = categoryDiscounts.FirstOrDefault(cd => cd.CategoryId == p.Category.ParentCategoryId.Value);
                if (parentDisc != null && parentDisc.DiscountRate > 0)
                {
                    rate = parentDisc.DiscountRate;
                }
            }
            return MapToDto(p, rate);
        }).ToList();

        return PaginatedResponse<ProductDto>.Ok(dtos, total, request.Page, request.Limit);
    }

    internal static ProductDto MapToDto(Domain.Entities.Product p) => MapToDto(p, 0);

    internal static ProductDto MapToDto(Domain.Entities.Product p, decimal globalDiscountRate)
    {
        var price = p.Price;
        var discountedPrice = p.DiscountedPrice;

        if (discountedPrice <= 0 || discountedPrice > price)
        {
            discountedPrice = price;
        }

        if (globalDiscountRate > 0)
        {
            discountedPrice = discountedPrice * (1 - globalDiscountRate / 100);
        }

        decimal discount = 0;
        if (price > 0)
        {
            discount = Math.Round((1 - (discountedPrice / price)) * 100, 2);
        }

        return new(
            p.Id, p.Name, p.Slug, p.Code, p.IntegrationCode, p.Barcode,
            p.ShortDescription, p.Description, p.ImageUrl,
            JsonSerializer.Deserialize<List<string>>(p.FeaturesJson) ?? [],
            JsonSerializer.Deserialize<Dictionary<string, string>>(p.SpecificationsJson) ?? [],
            p.Stock, price, discountedPrice, discount, p.TaxRate, p.Currency,
            p.IsNew, p.IsFeatured, p.IsDealOfTheDay, p.Status,
            p.CategoryId, p.Category?.Name ?? string.Empty,
            p.BrandId, p.Brand?.Name ?? string.Empty
        );
    }
}
