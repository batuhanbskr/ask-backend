using System.Text.Json;
using ASK.Application.Common.Exceptions;
using ASK.Application.DTOs.Product;
using ASK.Application.Features.Products.Queries.GetProducts;
using ASK.Domain.Entities;
using ASK.Domain.Interfaces;
using MediatR;

namespace ASK.Application.Features.Products.Commands.UpdateProduct;

public record UpdateProductCommand(int Id, UpdateProductDto Dto) : IRequest<ProductDto>;

public class UpdateProductCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await unitOfWork.Products.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.Id);

        var dto = request.Dto;

        if (await unitOfWork.Products.SlugExistsAsync(dto.Slug, request.Id, cancellationToken))
            throw new ValidationException("Bu slug zaten kullanılmaktadır.");

        product.Name = dto.Name;
        product.Slug = dto.Slug.ToLowerInvariant().Trim();
        product.Code = dto.Code;
        product.IntegrationCode = dto.IntegrationCode;
        product.Barcode = dto.Barcode;
        product.ShortDescription = dto.ShortDescription;
        product.Description = dto.Description;
        product.ImageUrl = dto.ImageUrl;
        product.FeaturesJson = JsonSerializer.Serialize(dto.Features);
        product.SpecificationsJson = JsonSerializer.Serialize(dto.Specifications);
        product.Stock = dto.Stock;
        product.Price = dto.Price;
        product.DiscountedPrice = dto.DiscountedPrice;
        product.Discount = dto.Discount;
        product.TaxRate = dto.TaxRate;
        product.Desi = dto.Desi;
        product.Currency = dto.Currency;
        product.IsNew = dto.IsNew;
        product.IsFeatured = dto.IsFeatured;
        product.Status = dto.Status;
        product.CategoryId = dto.CategoryId;
        product.BrandId = dto.BrandId;
        product.SupplierLink = dto.SupplierLink;
        product.UpdatedAt = DateTime.UtcNow;

        unitOfWork.Products.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        product = (await unitOfWork.Products.GetByIdAsync(product.Id, cancellationToken))!;
        return GetProductsQueryHandler.MapToDto(product);
    }
}
