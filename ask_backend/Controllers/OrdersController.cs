using ASK.Application.Common.Interfaces;
using ASK.Application.DTOs.Order;
using ASK.Application.Features.Orders.Commands.CreateOrder;
using ASK.Application.Features.Orders.Commands.UpdateOrderStatus;
using ASK.Application.Features.Orders.Queries.GetOrderById;
using ASK.Application.Features.Orders.Queries.GetOrders;
using ASK.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ask_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController(IMediator mediator, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Siparişleri listeler. Kullanıcı yalnızca kendi siparişlerini görür. Admin için /api/admin/orders kullanın.</summary>
    [HttpGet]
    public async Task<IActionResult> GetOrders(CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? 0;

        var result = await mediator.Send(new GetOrdersQuery(userId, false), cancellationToken);
        return Ok(new { success = true, data = result });
    }

    /// <summary>Sipariş detayı.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetOrderByIdQuery(id, currentUser.UserId ?? 0, currentUser.Role == "Admin" || currentUser.Role == "SuperAdmin"),
            cancellationToken);
        return Ok(new { success = true, data = result });
    }

    /// <summary>Sepetteki ürünlerden sipariş oluşturur.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateOrderCommand(currentUser.UserId ?? 0, dto.ShippingAddress, dto.Notes),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { success = true, data = result });
    }

    /// <summary>Sipariş durumunu günceller. [Admin]</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateOrderStatusCommand(id, dto.Status), cancellationToken);
        return Ok(new { success = true, data = result });
    }

    /// <summary>Kullanıcının kendi beklemedeki siparişini iptal eder. Danışmanına mail gider.</summary>
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> CancelOrder(
        int id,
        [FromServices] ASK.Infrastructure.Persistence.AppDbContext db,
        [FromServices] IEmailService emailService,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? 0;
        var order = await mediator.Send(
            new GetOrderByIdQuery(id, userId, false), cancellationToken);

        if (order == null)
            return NotFound(new { success = false, message = "Sipariş bulunamadı." });

        if (order.Status != OrderStatus.Pending)
            return BadRequest(new { success = false, message = "Sadece 'Beklemede' durumundaki siparişler doğrudan iptal edilebilir." });

        var result = await mediator.Send(new UpdateOrderStatusCommand(id, OrderStatus.Cancelled), cancellationToken);

        // Danışmana Bildirim Maili Gönder
        try
        {
            var dbOrder = await db.Orders.Include(o => o.User).FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
            if (dbOrder?.User != null && dbOrder.User.SalesRepresentativeId.HasValue)
            {
                var rep = await db.Users.FirstOrDefaultAsync(u => u.Id == dbOrder.User.SalesRepresentativeId.Value, cancellationToken);
                if (rep != null && !string.IsNullOrWhiteSpace(rep.Email))
                {
                    var customerName = $"{dbOrder.User.FirstName} {dbOrder.User.LastName}";
                    var companyName = dbOrder.User.Company ?? "Belirtilmemiş";
                    var emailBody = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: Arial, sans-serif; background: #f8fafc; padding: 20px;'>
    <div style='max-width: 600px; background: #fff; border-radius: 8px; padding: 25px; border: 1px solid #cbd5e1;'>
        <h2 style='color: #c0392b;'>⚠️ Müşteri Sipariş İptali Bildirimi</h2>
        <p>Sayın <strong>{rep.FirstName} {rep.LastName}</strong>,</p>
        <p>Sorumluluğunuzdaki bayi/müşteri (<strong>{customerName}</strong> - {companyName}) bekleyen siparişini iptal etmiştir:</p>
        <div style='background: #f1f5f9; padding: 15px; border-radius: 6px; margin: 15px 0;'>
            <div><strong>Sipariş No:</strong> {dbOrder.OrderNumber}</div>
            <div><strong>İptal Edilen Tutar:</strong> ₺{dbOrder.TotalAmount:N2}</div>
            <div><strong>Tarih:</strong> {DateTime.UtcNow.AddHours(3):dd.MM.yyyy HH:mm}</div>
        </div>
        <p>İptal tutarı müşterinin cari bakiyesine iade edilmiştir.</p>
    </div>
</body>
</html>";

                    await emailService.SendEmailAsync(
                        rep.Email,
                        $"[SİPARİŞ İPTALİ] {dbOrder.OrderNumber} - {customerName}",
                        emailBody,
                        isHtml: true,
                        cancellationToken);
                }
            }
        }
        catch {}

        return Ok(new { success = true, message = "Sipariş başarıyla iptal edildi ve cari bakiyeniz güncellendi.", data = result });
    }

    /// <summary>Kullanıcı onaylanmış/teslim edilmiş siparişi için İade Talebi oluşturur. Danışmanına otomatik mail gider.</summary>
    [HttpPost("{id:int}/return-request")]
    public async Task<IActionResult> RequestOrderReturn(
        int id,
        [FromBody] CreateReturnRequestDto dto,
        [FromServices] ASK.Infrastructure.Persistence.AppDbContext db,
        [FromServices] IEmailService emailService,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? 0;
        var order = await db.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId, cancellationToken);

        if (order == null)
            return NotFound(new { success = false, message = "Sipariş bulunamadı." });

        if (order.Status == OrderStatus.Pending)
            return BadRequest(new { success = false, message = "Beklemedeki siparişler henüz sevk edilmediği için 'Siparişi İptal Et' butonu ile doğrudan iptal edilmelidir." });

        if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Returned)
            return BadRequest(new { success = false, message = "İptal edilmiş veya zaten iade edilmiş siparişler için iade talebi oluşturulamaz." });

        if (order.Status == OrderStatus.ReturnRequested)
            return BadRequest(new { success = false, message = "Bu sipariş için zaten aktif bir iade talebiniz bulunmaktadır." });

        order.Status = OrderStatus.ReturnRequested;
        if (!string.IsNullOrWhiteSpace(dto.Reason))
        {
            order.Notes = (order.Notes != null ? order.Notes + " | " : "") + "İADE SEBEBİ: " + dto.Reason;
        }
        order.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        // Danışmana ve Yönetime Bildirim Maili Gönder
        try
        {
            var user = order.User;
            if (user != null && user.SalesRepresentativeId.HasValue)
            {
                var rep = await db.Users.FirstOrDefaultAsync(u => u.Id == user.SalesRepresentativeId.Value, cancellationToken);
                if (rep != null && !string.IsNullOrWhiteSpace(rep.Email))
                {
                    var customerName = $"{user.FirstName} {user.LastName}";
                    var companyName = user.Company ?? "Belirtilmemiş";
                    var emailBody = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: Arial, sans-serif; background: #f8fafc; padding: 20px;'>
    <div style='max-width: 600px; background: #fff; border-radius: 8px; padding: 25px; border: 1px solid #cbd5e1;'>
        <h2 style='color: #dc2626;'>📢 Müşteri İade Talebi Bildirimi</h2>
        <p>Sayın <strong>{rep.FirstName} {rep.LastName}</strong>,</p>
        <p>Sorumluluğunuzdaki bayi/müşteri (<strong>{customerName}</strong> - {companyName}) yeni bir sipariş iade talebi oluşturmuştur:</p>
        <div style='background: #f1f5f9; padding: 15px; border-radius: 6px; margin: 15px 0;'>
            <div><strong>Sipariş No:</strong> {order.OrderNumber}</div>
            <div><strong>Toplam Tutar:</strong> ₺{order.TotalAmount:N2}</div>
            <div><strong>İade Sebebi:</strong> {dto.Reason ?? "Belirtilmedi"}</div>
        </div>
        <p>Admin panelinizden siparişi inceleyerek iade işlemini onaylayabilir veya reddedebilirsiniz.</p>
    </div>
</body>
</html>";

                    await emailService.SendEmailAsync(
                        rep.Email,
                        $"[İADE TALEBİ] {order.OrderNumber} - {customerName}",
                        emailBody,
                        isHtml: true,
                        cancellationToken);
                }
            }
        }
        catch {}

        return Ok(new { success = true, message = "İade talebiniz alınmıştır. Müşteri temsilciniz inceleyerek sizinle iletişime geçecektir." });
    }
}

public record CreateReturnRequestDto(string Reason);
