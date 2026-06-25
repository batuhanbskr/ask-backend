using ASK.Application.Common.Exceptions;
using ASK.Application.DTOs.Brand;
using ASK.Domain.Entities;
using ASK.Domain.Interfaces;
using MediatR;

namespace ASK.Application.Features.Brands.Commands.UpdateBrand;

public record UpdateBrandCommand(int Id, string Name, string? LogoUrl, string? Website, bool IsActive) : IRequest<BrandDto>;

public class UpdateBrandCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateBrandCommand, BrandDto>
{
    public async Task<BrandDto> Handle(UpdateBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = await unitOfWork.Brands.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Brand), request.Id);

        brand.Name = request.Name;
        brand.LogoUrl = request.LogoUrl;
        brand.Website = request.Website;
        brand.IsActive = request.IsActive;
        brand.UpdatedAt = DateTime.UtcNow;

        unitOfWork.Brands.Update(brand);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new BrandDto(brand.Id, brand.Name, brand.LogoUrl, brand.Website);
    }
}
