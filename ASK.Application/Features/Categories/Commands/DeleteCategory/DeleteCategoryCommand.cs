using ASK.Application.Common.Exceptions;
using ASK.Domain.Entities;
using ASK.Domain.Interfaces;
using MediatR;
using AppValidationException = ASK.Application.Common.Exceptions.ValidationException;

namespace ASK.Application.Features.Categories.Commands.DeleteCategory;

public record DeleteCategoryCommand(int Id) : IRequest<Unit>;

public class DeleteCategoryCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCategoryCommand, Unit>
{
    public async Task<Unit> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await unitOfWork.Categories.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), request.Id);

        // Kategoriye bağlı ürün varsa silmeye izin verme
        if (category.Products.Any(p => p.Status == 1))
            throw new AppValidationException("Aktif ürünleri olan bir kategori silinemez.");

        unitOfWork.Categories.Remove(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
