namespace ASK.Application.DTOs.Product;

/// <summary>Ürün okuma DTO'su. Frontend Product interface ile uyumlu.</summary>
public record ProductDto(
    int Id,
    string Name,
    string Slug,
    string Code,
    string? IntegrationCode,
    string? Barcode,
    string ShortDescription,
    string Description,
    string? ImageUrl,
    List<string> Features,
    Dictionary<string, string> Specifications,
    int Stock,
    decimal Price,
    decimal DiscountedPrice,
    decimal Discount,
    decimal TaxRate,
    string Currency,
    bool IsNew,
    bool IsFeatured,
    int Status,
    int CategoryId,
    string CategoryName,
    int BrandId,
    string BrandName
);

public record CreateProductDto(
    string Name,
    string Slug,
    string Code,
    string? IntegrationCode,
    string? Barcode,
    string? SupplierProductId,
    string ShortDescription,
    string Description,
    string? ImageUrl,
    List<string> Features,
    Dictionary<string, string> Specifications,
    int Stock,
    decimal Price,
    decimal DiscountedPrice,
    decimal Discount,
    decimal TaxRate,
    decimal Desi,
    string Currency,
    bool IsNew,
    bool IsFeatured,
    int CategoryId,
    int BrandId,
    string? SupplierLink
);

public record UpdateProductDto(
    string Name,
    string Slug,
    string Code,
    string? IntegrationCode,
    string? Barcode,
    string ShortDescription,
    string Description,
    string? ImageUrl,
    List<string> Features,
    Dictionary<string, string> Specifications,
    int Stock,
    decimal Price,
    decimal DiscountedPrice,
    decimal Discount,
    decimal TaxRate,
    decimal Desi,
    string Currency,
    bool IsNew,
    bool IsFeatured,
    int Status = 1,
    int CategoryId = 0,
    int BrandId = 0,
    string? SupplierLink = null
);

public record ProductFilterDto(
    int? CategoryId,
    int? BrandId,
    bool? IsNew,
    bool? IsFeatured,
    string? Search,
    int Page = 1,
    int Limit = 12
);
