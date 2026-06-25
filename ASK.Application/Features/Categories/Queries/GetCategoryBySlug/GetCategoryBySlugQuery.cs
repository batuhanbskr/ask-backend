using ASK.Application.Common.Exceptions;
using ASK.Application.DTOs.Category;
using ASK.Domain.Entities;
using ASK.Domain.Interfaces;
using MediatR;

namespace ASK.Application.Features.Categories.Queries.GetCategoryBySlug;

public record GetCategoryBySlugQuery(string Slug) : IRequest<CategoryDto>;

public class GetCategoryBySlugQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetCategoryBySlugQuery, CategoryDto>
{
    public async Task<CategoryDto> Handle(GetCategoryBySlugQuery request, CancellationToken cancellationToken)
    {
        var category = await unitOfWork.Categories.GetBySlugAsync(request.Slug, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), request.Slug);

        return new CategoryDto(
            category.Id, category.Name, category.Slug, category.Description, category.Icon,
            category.Products.Count(p => p.Status == 1),
            category.ParentCategoryId);
    }
}
