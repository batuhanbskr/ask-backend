using System.Text.Json;
using ASK.Application.Common.Exceptions;
using ASK.Application.DTOs.Product;
using ASK.Application.Features.Products.Queries.GetProducts;
using ASK.Domain.Entities;
using ASK.Domain.Interfaces;
using FluentValidation;
using MediatR;
using AppValidationException = ASK.Application.Common.Exceptions.ValidationException;

namespace ASK.Application.Features.Products.Commands.CreateProduct;

public record CreateProductCommand(CreateProductDto Dto) : IRequest<ProductDto>;

public class CreateProductCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        if (await unitOfWork.Products.SlugExistsAsync(dto.Slug, null, cancellationToken))
            throw new AppValidationException("Bu slug zaten kullanılmaktadır.");

        if (!await unitOfWork.Categories.AnyAsync(c => c.Id == dto.CategoryId && c.IsActive, cancellationToken))
            throw new NotFoundException(nameof(Category), dto.CategoryId);

        if (!await unitOfWork.Brands.AnyAsync(b => b.Id == dto.BrandId && b.IsActive, cancellationToken))
            throw new NotFoundException(nameof(Brand), dto.BrandId);

        var product = new Product
        {
            Name = dto.Name,
            Slug = dto.Slug.ToLowerInvariant().Trim(),
            Code = dto.Code,
            IntegrationCode = dto.IntegrationCode,
            Barcode = dto.Barcode,
            SupplierProductId = dto.SupplierProductId,
            ShortDescription = dto.ShortDescription,
            Description = dto.Description,
            ImageUrl = dto.ImageUrl,
            FeaturesJson = JsonSerializer.Serialize(dto.Features),
            SpecificationsJson = JsonSerializer.Serialize(dto.Specifications),
            Stock = dto.Stock,
            Price = dto.Price,
            DiscountedPrice = dto.DiscountedPrice,
            Discount = dto.Discount,
            TaxRate = dto.TaxRate,
            Desi = dto.Desi,
            Currency = dto.Currency,
            IsNew = dto.IsNew,
            IsFeatured = dto.IsFeatured,
            CategoryId = dto.CategoryId,
            BrandId = dto.BrandId,
            SupplierLink = dto.SupplierLink,
            Status = 1
        };

        await unitOfWork.Products.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Navigation property'leri populate et
        product = (await unitOfWork.Products.GetByIdAsync(product.Id, cancellationToken))!;
        return GetProductsQueryHandler.MapToDto(product);
    }
}

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Dto.Name).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Dto.Slug).NotEmpty().MaximumLength(500)
            .Matches(@"^[a-z0-9\-]+$").WithMessage("Slug yalnızca küçük harf, rakam ve tire içerebilir.");
        RuleFor(x => x.Dto.Code).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Dto.Price).GreaterThanOrEqualTo(0).WithMessage("Fiyat negatif olamaz.");
        RuleFor(x => x.Dto.DiscountedPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Dto.Stock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Dto.CategoryId).GreaterThan(0);
        RuleFor(x => x.Dto.BrandId).GreaterThan(0);
        RuleFor(x => x.Dto.Currency).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Dto.TaxRate).InclusiveBetween(0, 100);
    }
}
