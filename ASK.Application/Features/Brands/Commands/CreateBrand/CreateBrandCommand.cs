using ASK.Application.Common.Exceptions;
using ASK.Application.DTOs.Brand;
using ASK.Domain.Entities;
using ASK.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace ASK.Application.Features.Brands.Commands.CreateBrand;

public record CreateBrandCommand(string Name, string? LogoUrl, string? Website) : IRequest<BrandDto>;

public class CreateBrandCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateBrandCommand, BrandDto>
{
    public async Task<BrandDto> Handle(CreateBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = new Brand
        {
            Name = request.Name,
            LogoUrl = request.LogoUrl,
            Website = request.Website
        };

        await unitOfWork.Brands.AddAsync(brand, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new BrandDto(brand.Id, brand.Name, brand.LogoUrl, brand.Website);
    }
}

public class CreateBrandCommandValidator : AbstractValidator<CreateBrandCommand>
{
    public CreateBrandCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LogoUrl).MaximumLength(500).When(x => x.LogoUrl is not null);
        RuleFor(x => x.Website).MaximumLength(500).When(x => x.Website is not null);
    }
}
