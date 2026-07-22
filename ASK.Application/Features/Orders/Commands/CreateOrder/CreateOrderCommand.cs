using ASK.Application.Common.Exceptions;
using ASK.Application.Common.Interfaces;
using ASK.Application.DTOs.Order;
using ASK.Application.Features.Orders.Queries.GetOrders;
using ASK.Domain.Entities;
using ASK.Domain.Interfaces;
using FluentValidation;
using MediatR;
using AppValidationException = ASK.Application.Common.Exceptions.ValidationException;
using DomainCart = ASK.Domain.Entities.Cart;

namespace ASK.Application.Features.Orders.Commands.CreateOrder;

public record CreateOrderCommand(int UserId, string ShippingAddress, string? Notes) : IRequest<OrderDto>;

public class CreateOrderCommandHandler(IUnitOfWork unitOfWork, IEmailService emailService)
    : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // Kullanıcının sepetini al
        var cart = await unitOfWork.Carts.GetByUserIdWithItemsAsync(request.UserId, cancellationToken)
            ?? throw new AppValidationException("Sepet bulunamadı.");

        if (!cart.CartItems.Any())
            throw new AppValidationException("Sepet boş. Sipariş oluşturulamaz.");

        // Stok ve ürün geçerliliği kontrolü
        foreach (var cartItem in cart.CartItems)
        {
            var product = await unitOfWork.Products.GetByIdAsync(cartItem.ProductId, cancellationToken)
                ?? throw new NotFoundException(nameof(Product), cartItem.ProductId);

            if (product.Status != 1)
                throw new AppValidationException($"'{product.Name}' ürünü aktif değil.");

            if (product.Stock < cartItem.Quantity)
                throw new AppValidationException($"'{product.Name}' için yeterli stok yok. Mevcut: {product.Stock}");
        }

        var orderNumber = await unitOfWork.Orders.GenerateOrderNumberAsync(cancellationToken);

        var order = new Order
        {
            UserId = request.UserId,
            OrderNumber = orderNumber,
            ShippingAddress = request.ShippingAddress,
            Notes = request.Notes
        };

        var user = await unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            throw new AppValidationException("Kullanıcı bulunamadı.");

        if (!user.IsActive)
            throw new AppValidationException("Hesabınız pasife alındığı için sipariş oluşturamazsınız. Lütfen yöneticiniz veya müşteri temsilciniz ile iletişime geçiniz.");

        var globalDiscountRate = user.GlobalDiscountRate;

        decimal total = 0;

        foreach (var cartItem in cart.CartItems)
        {
            var product = await unitOfWork.Products.GetByIdAsync(cartItem.ProductId, cancellationToken)!;
            var unitPrice = product!.DiscountedPrice;
            if (unitPrice <= 0 || unitPrice > product.Price)
            {
                unitPrice = product.Price;
            }

            if (globalDiscountRate > 0)
            {
                unitPrice = unitPrice * (1 - globalDiscountRate / 100);
            }

            order.OrderItems.Add(new OrderItem
            {
                ProductId = cartItem.ProductId,
                ProductName = product.Name,
                Quantity = cartItem.Quantity,
                UnitPrice = unitPrice
            });

            // Stok düş
            product.Stock -= cartItem.Quantity;
            unitOfWork.Products.Update(product);

            total += unitPrice * cartItem.Quantity;
        }

        order.TotalAmount = total;

        await unitOfWork.Orders.AddAsync(order, cancellationToken);

        // Cari bakiyeyi sipariş tutarı kadar düşür
        if (user != null)
        {
            user.CurrentBalance -= total;
            unitOfWork.Users.Update(user);
        }

        // Sepeti temizle
        unitOfWork.Carts.Remove(cart);
        var newCart = new DomainCart { UserId = request.UserId };
        await unitOfWork.Carts.AddAsync(newCart, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Send sales representative email notification
        try
        {
            if (user?.SalesRepresentativeId.HasValue == true)
            {
                var rep = await unitOfWork.Users.GetByIdAsync(user.SalesRepresentativeId.Value, cancellationToken);
                if (rep != null && !string.IsNullOrWhiteSpace(rep.Email))
                {
                    var customerName = $"{user.FirstName} {user.LastName}";
                    var companyName = user.Company ?? "Belirtilmemiş";
                    var orderItemsHtml = string.Join("", order.OrderItems.Select(item => $@"
                        <tr>
                            <td style='padding: 10px; border-bottom: 1px solid #e2e8f0;'>{item.ProductName}</td>
                            <td style='padding: 10px; border-bottom: 1px solid #e2e8f0; text-align: center;'>{item.Quantity}</td>
                            <td style='padding: 10px; border-bottom: 1px solid #e2e8f0; text-align: right;'>₺{item.UnitPrice:N2}</td>
                            <td style='padding: 10px; border-bottom: 1px solid #e2e8f0; text-align: right; font-weight: bold;'>₺{(item.Quantity * item.UnitPrice):N2}</td>
                        </tr>"));

                    var emailBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f8fafc; color: #1e293b; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1); margin: 0 auto; border: 1px solid #e2e8f0; }}
        .header {{ background-color: #c0392b; color: #ffffff; padding: 25px 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 20px; font-weight: 700; letter-spacing: 0.5px; }}
        .content {{ padding: 30px 25px; }}
        .greet {{ font-size: 16px; font-weight: bold; margin-bottom: 15px; }}
        .intro {{ font-size: 14px; line-height: 1.6; color: #475569; margin-bottom: 25px; }}
        .details-box {{ background-color: #f1f5f9; border-radius: 8px; padding: 15px; margin-bottom: 25px; font-size: 13px; }}
        .table {{ width: 100%; border-collapse: collapse; margin-top: 15px; font-size: 13px; }}
        .table th {{ background-color: #f8fafc; padding: 10px; text-align: left; font-weight: bold; color: #475569; border-bottom: 2px solid #e2e8f0; }}
        .cta-container {{ text-align: center; margin-top: 30px; }}
        .btn {{ display: inline-block; background-color: #c0392b; color: #ffffff !important; padding: 12px 24px; border-radius: 6px; text-decoration: none; font-size: 14px; font-weight: bold; }}
        .footer {{ background-color: #f8fafc; padding: 20px; text-align: center; font-size: 11px; color: #94a3b8; border-top: 1px solid #e2e8f0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Yeni Sipariş Bildirimi</h1>
        </div>
        <div class='content'>
            <div class='greet'>Sayın {rep.FirstName} {rep.LastName},</div>
            <div class='intro'>
                Temsilcisi olduğunuz <strong>{customerName}</strong> ({companyName}) firması tarafından yeni bir sipariş oluşturulmuştur. Sipariş ayrıntıları aşağıda listelenmiştir:
            </div>
            
            <div class='details-box'>
                <div style='margin-bottom: 8px;'><strong>Sipariş No:</strong> <code style='background: #e2e8f0; padding: 2px 6px; border-radius: 4px;'>{order.OrderNumber}</code></div>
                <div style='margin-bottom: 8px;'><strong>Tarih:</strong> {DateTime.UtcNow.AddHours(3):dd.MM.yyyy HH:mm}</div>
                <div style='margin-bottom: 8px;'><strong>Toplam Tutar:</strong> <span style='font-size: 15px; font-weight: bold; color: #c0392b;'>₺{order.TotalAmount:N2}</span></div>
                <div style='margin-bottom: 8px;'><strong>Adres:</strong> {order.ShippingAddress}</div>
                {(!string.IsNullOrWhiteSpace(order.Notes) ? $"<div style='margin-bottom: 8px;'><strong>Sipariş Notu:</strong> {order.Notes}</div>" : "")}
            </div>

            <h3 style='font-size: 14px; margin-bottom: 10px; color: #0f172a; border-bottom: 1px solid #e2e8f0; padding-bottom: 5px; text-transform: uppercase;'>Sipariş Kalemleri</h3>
            <table class='table'>
                <thead>
                    <tr>
                        <th>Ürün</th>
                        <th style='text-align: center;'>Adet</th>
                        <th style='text-align: right;'>Fiyat</th>
                        <th style='text-align: right;'>Toplam</th>
                    </tr>
                </thead>
                <tbody>
                    {orderItemsHtml}
                </tbody>
            </table>

            <div class='cta-container'>
                <a href='https://b2b.askteknikhirdavat.com/admin/orders' class='btn'>Siparişleri Görüntüle</a>
            </div>
        </div>
        <div class='footer'>
            Bu e-posta ASK B2B Otomasyon Sistemi tarafından otomatik olarak gönderilmiştir.<br>
            Lütfen bu mesajı yanıtlamayınız.
        </div>
    </div>
</body>
</html>";

                    await emailService.SendEmailAsync(
                        rep.Email,
                        $"[Yeni Sipariş] {order.OrderNumber} - {customerName}",
                        emailBody,
                        isHtml: true,
                        cancellationToken);
                }
            }
        }
        catch (Exception)
        {
            // Fail-safe: email sending issues must not break order placement completion
        }

        return GetOrdersQueryHandler.MapToDto(order);
    }
}

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.ShippingAddress)
            .NotEmpty().WithMessage("Teslimat adresi boş olamaz.")
            .MaximumLength(500);
    }
}
