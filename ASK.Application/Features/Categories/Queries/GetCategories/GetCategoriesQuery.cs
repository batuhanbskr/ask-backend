using ASK.Application.DTOs.Category;
using ASK.Domain.Interfaces;
using MediatR;

namespace ASK.Application.Features.Categories.Queries.GetCategories;

public record GetCategoriesQuery : IRequest<List<CategoryDto>>;

public class GetCategoriesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
{
    public async Task<List<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await unitOfWork.Categories.GetAllWithProductCountAsync(cancellationToken);

        return categories.Select(c => new CategoryDto(
            c.Id, c.Name, c.Slug, c.Description, c.Icon,
            c.Products.Count(p => p.Status == 1),
            c.ParentCategoryId
        )).ToList();
    }
}
