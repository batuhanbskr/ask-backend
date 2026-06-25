using ASK.Application.Common.Exceptions;
using ASK.Application.DTOs.Category;
using ASK.Domain.Entities;
using ASK.Domain.Interfaces;
using FluentValidation;
using MediatR;
using AppValidationException = ASK.Application.Common.Exceptions.ValidationException;

namespace ASK.Application.Features.Categories.Commands.CreateCategory;

public record CreateCategoryCommand(
    string Name,
    string Slug,
    string? Description,
    string? Icon,
    int? ParentCategoryId
) : IRequest<CategoryDto>;

public class CreateCategoryCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        if (await unitOfWork.Categories.SlugExistsAsync(request.Slug, null, cancellationToken))
            throw new AppValidationException("Bu slug zaten kullanılmaktadır.");

        var category = new Category
        {
            Name = request.Name,
            Slug = request.Slug.ToLowerInvariant().Trim(),
            Description = request.Description,
            Icon = request.Icon,
            ParentCategoryId = request.ParentCategoryId
        };

        await unitOfWork.Categories.AddAsync(category, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CategoryDto(category.Id, category.Name, category.Slug,
            category.Description, category.Icon, 0, category.ParentCategoryId);
    }
}

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(200)
            .Matches(@"^[a-z0-9\-]+$").WithMessage("Slug yalnızca küçük harf, rakam ve tire içerebilir.");
    }
}
