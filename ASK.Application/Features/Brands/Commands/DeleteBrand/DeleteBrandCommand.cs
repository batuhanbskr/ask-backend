using ASK.Application.Common.Exceptions;
using ASK.Domain.Entities;
using ASK.Domain.Interfaces;
using MediatR;
using AppValidationException = ASK.Application.Common.Exceptions.ValidationException;

namespace ASK.Application.Features.Brands.Commands.DeleteBrand;

public record DeleteBrandCommand(int Id) : IRequest<Unit>;

public class DeleteBrandCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteBrandCommand, Unit>
{
    public async Task<Unit> Handle(DeleteBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = await unitOfWork.Brands.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Brand), request.Id);

        if (brand.Products.Any(p => p.Status == 1))
            throw new AppValidationException("Aktif ürünleri olan bir marka silinemez.");

        unitOfWork.Brands.Remove(brand);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
