using ASK.Application.DTOs.Brand;
using ASK.Domain.Interfaces;
using MediatR;

namespace ASK.Application.Features.Brands.Queries.GetBrands;

public record GetBrandsQuery : IRequest<List<BrandDto>>;

public class GetBrandsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetBrandsQuery, List<BrandDto>>
{
    public async Task<List<BrandDto>> Handle(GetBrandsQuery request, CancellationToken cancellationToken)
    {
        var brands = await unitOfWork.Brands.FindAsync(b => b.IsActive, cancellationToken);
        return brands.Select(b => new BrandDto(b.Id, b.Name, b.LogoUrl, b.Website)).ToList();
    }
}
