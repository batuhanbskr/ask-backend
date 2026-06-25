using ASK.Application.DTOs.Product;
using ASK.Application.Features.Products.Commands.CreateProduct;
using ASK.Application.Features.Products.Commands.DeleteProduct;
using ASK.Application.Features.Products.Commands.UpdateProduct;
using ASK.Application.Features.Products.Queries.GetProducts;
using ASK.Application.DTOs.Category;
using ASK.Application.Features.Categories.Commands.CreateCategory;
using ASK.Application.Features.Categories.Commands.DeleteCategory;
using ASK.Application.Features.Categories.Commands.UpdateCategory;
using ASK.Application.Features.Orders.Queries.GetOrders;
using ASK.Application.Features.Orders.Commands.UpdateOrderStatus;
using ASK.Application.DTOs.Order;
using ASK.Domain.Entities;
using ASK.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ASK.Infrastructure.Persistence;

namespace ask_backend.Controllers;

/// <summary>
/// Tüm admin operasyonlarının tek giriş noktası.
/// Tüm action'lar [Authorize(Roles = "Admin")] gerektirir.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController(IMediator mediator, AppDbContext db)
    : ControllerBase
{
    // ═══════════════════════════════════════════════════════════
    // DASHBOARD — Özet istatistikler
    // ═══════════════════════════════════════════════════════════

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var totalProducts  = await db.Products.CountAsync(ct);
        var totalUsers     = await db.Users.CountAsync(ct);
        var totalOrders    = await db.Orders.CountAsync(ct);
        var pendingOrders  = await db.Orders.CountAsync(o => o.Status == OrderStatus.Pending, ct);
        var totalRevenue   = await db.Orders
            .Where(o => o.Status != OrderStatus.Cancelled)
            .SumAsync(o => o.TotalAmount, ct);
        var totalPayments  = await db.Payments.SumAsync(p => p.Amount, ct);
        var recentOrders   = await db.Orders
            .Include(o => o.User)
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new {
                o.Id, o.OrderNumber, o.Status, o.TotalAmount, o.CreatedAt,
                CustomerName = o.User.FirstName + " " + o.User.LastName
            })
            .ToListAsync(ct);
        var lowStock = await db.Products
            .Where(p => p.Stock < 10)
            .OrderBy(p => p.Stock)
            .Take(5)
            .Select(p => new { p.Id, p.Name, p.Code, p.Stock })
            .ToListAsync(ct);

        return Ok(new { success = true, data = new {
            totalProducts, totalUsers, totalOrders, pendingOrders,
            totalRevenue, totalPayments, recentOrders, lowStock
        }});
    }

    // ═══════════════════════════════════════════════════════════
    // PRODUCTS
    // ═══════════════════════════════════════════════════════════

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int? categoryId, [FromQuery] int? brandId,
        [FromQuery] bool? isNew, [FromQuery] bool? isFeatured,
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 100);
        var result = await mediator.Send(
            new GetProductsQuery(categoryId, brandId, isNew, isFeatured, search, page, limit, ActiveOnly: false), ct);
        return Ok(result);
    }

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateProductCommand(dto), ct);
        return StatusCode(201, new { success = true, data = result });
    }

    [HttpPut("products/{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDto dto, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateProductCommand(id, dto), ct);
        return Ok(new { success = true, data = result });
    }

    [HttpDelete("products/{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id, CancellationToken ct)
    {
        await mediator.Send(new DeleteProductCommand(id), ct);
        return Ok(new { success = true, message = "Ürün silindi." });
    }

    // ═══════════════════════════════════════════════════════════
    // CATEGORIES
    // ═══════════════════════════════════════════════════════════

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var cats = await db.Categories
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.Description, c.Icon,
                c.Products.Count, c.ParentCategoryId))
            .ToListAsync(ct);
        return Ok(new { success = true, data = cats });
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto, CancellationToken ct)
    {
        var result = await mediator.Send(
            new CreateCategoryCommand(dto.Name, dto.Slug, dto.Description, dto.Icon, dto.ParentCategoryId), ct);
        return StatusCode(201, new { success = true, data = result });
    }

    [HttpPut("categories/{id:int}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDto dto, CancellationToken ct)
    {
        var result = await mediator.Send(
            new UpdateCategoryCommand(id, dto.Name, dto.Slug, dto.Description, dto.Icon, dto.IsActive, dto.ParentCategoryId), ct);
        return Ok(new { success = true, data = result });
    }

    [HttpDelete("categories/{id:int}")]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken ct)
    {
        await mediator.Send(new DeleteCategoryCommand(id), ct);
        return Ok(new { success = true, message = "Kategori silindi." });
    }

    // ═══════════════════════════════════════════════════════════
    // USERS
    // ═══════════════════════════════════════════════════════════

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 100);
        var query = db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u =>
                u.FirstName.Contains(search) || u.LastName.Contains(search) ||
                u.Email.Contains(search) || (u.Company != null && u.Company.Contains(search)));

        var total = await query.CountAsync(ct);
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * limit).Take(limit)
            .Select(u => new {
                u.Id, u.FirstName, u.LastName, u.Email, u.Phone,
                u.Company, u.City, u.Role, u.IsActive, u.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new { success = true, data = users, total, page, limit });
    }

    [HttpGet("users/{id:int}")]
    public async Task<IActionResult> GetUser(int id, CancellationToken ct)
    {
        var user = await db.Users.Where(u => u.Id == id)
            .Select(u => new {
                u.Id, u.FirstName, u.LastName, u.Email, u.Phone,
                u.Company, u.Address, u.City, u.Role, u.IsActive, u.CreatedAt,
                OrderCount = u.Orders.Count
            })
            .FirstOrDefaultAsync(ct);

        if (user is null) return NotFound(new { success = false, message = "Kullanıcı bulunamadı." });
        return Ok(new { success = true, data = user });
    }

    [HttpPut("users/{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] AdminUpdateUserDto dto, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct);
        if (user is null) return NotFound(new { success = false, message = "Kullanıcı bulunamadı." });

        user.FirstName = dto.FirstName;
        user.LastName  = dto.LastName;
        user.Phone     = dto.Phone;
        user.Company   = dto.Company;
        user.City      = dto.City;
        user.Address   = dto.Address;
        user.Role      = dto.Role;
        user.IsActive  = dto.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(new { success = true, message = "Kullanıcı güncellendi." });
    }

    [HttpDelete("users/{id:int}")]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct);
        if (user is null) return NotFound(new { success = false, message = "Kullanıcı bulunamadı." });

        // Soft delete — tamamen silmek yerine pasif yap
        user.IsActive  = false;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { success = true, message = "Kullanıcı pasif yapıldı." });
    }

    // ═══════════════════════════════════════════════════════════
    // ORDERS
    // ═══════════════════════════════════════════════════════════

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int? status, [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 100);
        var query = db.Orders.Include(o => o.User).Include(o => o.OrderItems).AsQueryable();

        if (status.HasValue)
            query = query.Where(o => (int)o.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(o =>
                o.OrderNumber.Contains(search) ||
                o.User.Email.Contains(search) ||
                o.User.FirstName.Contains(search) ||
                o.User.LastName.Contains(search));

        var total = await query.CountAsync(ct);
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * limit).Take(limit)
            .ToListAsync(ct);

        var dto = orders.Select(o => new {
            o.Id, o.OrderNumber, o.Status,
            StatusLabel = GetOrderStatusLabel(o.Status),
            o.TotalAmount, o.ShippingAddress, o.Notes, o.CreatedAt,
            CustomerName = o.User.FirstName + " " + o.User.LastName,
            CustomerEmail = o.User.Email,
            Items = o.OrderItems.Select(i => new {
                i.Id, i.ProductId, i.ProductName, i.Quantity, i.UnitPrice,
                LineTotal = i.Quantity * i.UnitPrice
            })
        });

        return Ok(new { success = true, data = dto, total, page, limit });
    }

    [HttpPatch("orders/{id:int}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto dto, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateOrderStatusCommand(id, dto.Status), ct);
        return Ok(new { success = true, data = result });
    }

    // ═══════════════════════════════════════════════════════════
    // PAYMENTS
    // ═══════════════════════════════════════════════════════════

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments(
        [FromQuery] int? method, [FromQuery] int? status,
        [FromQuery] string? search, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int page = 1, [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 100);
        var query = db.Payments
            .Include(p => p.User)
            .Include(p => p.Order)
            .AsQueryable();

        if (method.HasValue) query = query.Where(p => (int)p.Method == method.Value);
        if (status.HasValue) query = query.Where(p => (int)p.Status == status.Value);
        if (from.HasValue)   query = query.Where(p => p.PaidAt >= from.Value);
        if (to.HasValue)     query = query.Where(p => p.PaidAt <= to.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                p.PaymentNumber.Contains(search) ||
                (p.Reference != null && p.Reference.Contains(search)));

        var total = await query.CountAsync(ct);
        var totalAmount = await query.SumAsync(p => p.Amount, ct);
        var payments = await query
            .OrderByDescending(p => p.PaidAt)
            .Skip((page - 1) * limit).Take(limit)
            .Select(p => new {
                p.Id, p.PaymentNumber, p.Amount, p.Method, p.Status,
                MethodLabel = GetPaymentMethodLabel(p.Method),
                StatusLabel = GetPaymentStatusLabel(p.Status),
                p.Description, p.Reference, p.PaidAt,
                p.OrderId, OrderNumber = p.Order != null ? p.Order.OrderNumber : null,
                p.UserId,
                CustomerName = p.User != null ? p.User.FirstName + " " + p.User.LastName : null
            })
            .ToListAsync(ct);

        return Ok(new { success = true, data = payments, total, totalAmount, page, limit });
    }

    [HttpPost("payments")]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto, CancellationToken ct)
    {
        var count = await db.Payments.CountAsync(ct) + 1;
        var payment = new Payment
        {
            PaymentNumber = $"PAY-{DateTime.UtcNow:yyyyMMdd}-{count:D5}",
            Amount      = dto.Amount,
            Method      = dto.Method,
            Status      = dto.Status,
            Description = dto.Description,
            Reference   = dto.Reference,
            PaidAt      = dto.PaidAt ?? DateTime.UtcNow,
            OrderId     = dto.OrderId,
            UserId      = dto.UserId,
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);
        return StatusCode(201, new { success = true, data = new { payment.Id, payment.PaymentNumber } });
    }

    [HttpPut("payments/{id:int}")]
    public async Task<IActionResult> UpdatePayment(int id, [FromBody] CreatePaymentDto dto, CancellationToken ct)
    {
        var payment = await db.Payments.FindAsync([id], ct);
        if (payment is null) return NotFound(new { success = false, message = "Ödeme bulunamadı." });

        payment.Amount      = dto.Amount;
        payment.Method      = dto.Method;
        payment.Status      = dto.Status;
        payment.Description = dto.Description;
        payment.Reference   = dto.Reference;
        payment.PaidAt      = dto.PaidAt ?? payment.PaidAt;
        payment.OrderId     = dto.OrderId;
        payment.UserId      = dto.UserId;
        payment.UpdatedAt   = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(new { success = true, message = "Ödeme güncellendi." });
    }

    [HttpDelete("payments/{id:int}")]
    public async Task<IActionResult> DeletePayment(int id, CancellationToken ct)
    {
        var payment = await db.Payments.FindAsync([id], ct);
        if (payment is null) return NotFound(new { success = false, message = "Ödeme bulunamadı." });
        db.Payments.Remove(payment);
        await db.SaveChangesAsync(ct);
        return Ok(new { success = true, message = "Ödeme silindi." });
    }

    // ═══════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════
    private static string GetOrderStatusLabel(OrderStatus s) => s switch {
        OrderStatus.Pending   => "Beklemede",
        OrderStatus.Confirmed => "Onaylandı",
        OrderStatus.Shipped   => "Kargoda",
        OrderStatus.Delivered => "Teslim Edildi",
        OrderStatus.Cancelled => "İptal Edildi",
        _ => "Bilinmiyor"
    };

    private static string GetPaymentMethodLabel(PaymentMethod m) => m switch {
        PaymentMethod.Cash         => "Nakit",
        PaymentMethod.CreditCard   => "Kredi Kartı",
        PaymentMethod.VirtualPos   => "Sanal POS",
        PaymentMethod.BankTransfer => "Havale/EFT",
        PaymentMethod.Check        => "Çek",
        _ => "Diğer"
    };

    private static string GetPaymentStatusLabel(PaymentStatus s) => s switch {
        PaymentStatus.Pending   => "Beklemede",
        PaymentStatus.Completed => "Tamamlandı",
        PaymentStatus.Failed    => "Başarısız",
        PaymentStatus.Refunded  => "İade Edildi",
        _ => "Bilinmiyor"
    };
}

// ─── DTOs (yalnızca Admin'e özel) ─────────────────────────────────────────────

public record AdminUpdateUserDto(
    string FirstName, string LastName,
    string? Phone, string? Company, string? City, string? Address,
    UserRole Role, bool IsActive
);

public record CreatePaymentDto(
    decimal Amount,
    PaymentMethod Method,
    PaymentStatus Status,
    string? Description,
    string? Reference,
    DateTime? PaidAt,
    int? OrderId,
    int? UserId
);
