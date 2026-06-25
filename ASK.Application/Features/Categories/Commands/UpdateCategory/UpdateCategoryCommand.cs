using ASK.Application.Common.Exceptions;
using ASK.Application.DTOs.Category;
using ASK.Domain.Entities;
using ASK.Domain.Interfaces;
using MediatR;
using AppValidationException = ASK.Application.Common.Exceptions.ValidationException;

namespace ASK.Application.Features.Categories.Commands.UpdateCategory;

public record UpdateCategoryCommand(
    int Id,
    string Name,
    string Slug,
    string? Description,
    string? Icon,
    bool IsActive,
    int? ParentCategoryId
) : IRequest<CategoryDto>;

public class UpdateCategoryCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await unitOfWork.Categories.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), request.Id);

        if (await unitOfWork.Categories.SlugExistsAsync(request.Slug, request.Id, cancellationToken))
            throw new AppValidationException("Bu slug zaten kullanılmaktadır.");

        category.Name = request.Name;
        category.Slug = request.Slug.ToLowerInvariant().Trim();
        category.Description = request.Description;
        category.Icon = request.Icon;
        category.IsActive = request.IsActive;
        category.ParentCategoryId = request.ParentCategoryId;
        category.UpdatedAt = DateTime.UtcNow;

        unitOfWork.Categories.Update(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CategoryDto(category.Id, category.Name, category.Slug,
            category.Description, category.Icon,
            category.Products.Count(p => p.Status == 1),
            category.ParentCategoryId);
    }
}
