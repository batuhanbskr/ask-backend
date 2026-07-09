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

using ASK.Application.Common.Interfaces;

namespace ask_backend.Controllers;

/// <summary>
/// Tüm admin operasyonlarının tek giriş noktası.
/// Tüm action'lar [Authorize(Roles = "Admin,SuperAdmin")] gerektirir.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminController(IMediator mediator, AppDbContext db, ICurrentUserService currentUser, IPasswordHasher passwordHasher)
    : ControllerBase
{
    // ═══════════════════════════════════════════════════════════
    // DASHBOARD — Özet istatistikler
    // ═══════════════════════════════════════════════════════════

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var currentUserId = currentUser.UserId ?? 0;
        var isSuperAdmin = currentUser.Role == "SuperAdmin";

        var totalProducts  = await db.Products.CountAsync(ct);
        
        var usersQuery = db.Users.AsQueryable();
        var ordersQuery = db.Orders.AsQueryable();
        var paymentsQuery = db.Payments.AsQueryable();

        if (!isSuperAdmin)
        {
            usersQuery = usersQuery.Where(u => u.SalesRepresentativeId == currentUserId);
            ordersQuery = ordersQuery.Where(o => o.User.SalesRepresentativeId == currentUserId);
            paymentsQuery = paymentsQuery.Where(p =>
                p.User != null && p.User.SalesRepresentativeId == currentUserId);
        }

        var totalUsers     = await usersQuery.CountAsync(ct);
        var totalOrders    = await ordersQuery.CountAsync(ct);
        var pendingOrders  = await ordersQuery.CountAsync(o => o.Status == OrderStatus.Pending, ct);
        var totalRevenue   = await ordersQuery
            .Where(o => o.Status != OrderStatus.Cancelled)
            .SumAsync(o => o.TotalAmount, ct);
        var totalPayments  = await paymentsQuery.SumAsync(p => p.Amount, ct);
        
        var recentOrders   = await ordersQuery
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
        [FromQuery] bool? isNew, [FromQuery] bool? isFeatured, [FromQuery] bool? isDealOfTheDay,
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 100);
        var result = await mediator.Send(
            new GetProductsQuery(categoryId, brandId, isNew, isFeatured, isDealOfTheDay, null, search, page, limit, ActiveOnly: false), ct);
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
        var currentUserId = currentUser.UserId ?? 0;
        var isSuperAdmin = currentUser.Role == "SuperAdmin";

        limit = Math.Clamp(limit, 1, 100);
        var query = db.Users.AsQueryable();

        if (!isSuperAdmin)
        {
            query = query.Where(u => u.SalesRepresentativeId == currentUserId);
        }

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
                u.Company, u.City, u.Role, u.IsActive, u.GlobalDiscountRate, u.CreatedAt,
                SalesRepresentativeId = u.SalesRepresentativeId,
                SalesRepresentativeName = u.SalesRepresentative != null ? u.SalesRepresentative.FirstName + " " + u.SalesRepresentative.LastName : null
            })
            .ToListAsync(ct);

        return Ok(new { success = true, data = users, total, page, limit });
    }

    [HttpGet("users/{id:int}")]
    public async Task<IActionResult> GetUser(int id, CancellationToken ct)
    {
        var currentUserId = currentUser.UserId ?? 0;
        var isSuperAdmin = currentUser.Role == "SuperAdmin";

        var query = db.Users.Where(u => u.Id == id);

        if (!isSuperAdmin)
        {
            query = query.Where(u => u.SalesRepresentativeId == currentUserId);
        }

        var user = await query
            .Select(u => new {
                u.Id, u.FirstName, u.LastName, u.Email, u.Phone,
                u.Company, u.Address, u.City, u.Role, u.IsActive, u.GlobalDiscountRate, u.CreatedAt,
                SalesRepresentativeId = u.SalesRepresentativeId,
                OrderCount = u.Orders.Count
            })
            .FirstOrDefaultAsync(ct);

        if (user is null) return NotFound(new { success = false, message = "Kullanıcı bulunamadı veya yetkiniz yok." });
        return Ok(new { success = true, data = user });
    }

    [HttpPut("users/{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] AdminUpdateUserDto dto, CancellationToken ct)
    {
        var currentUserId = currentUser.UserId ?? 0;
        var isSuperAdmin = currentUser.Role == "SuperAdmin";

        var user = await db.Users.FindAsync([id], ct);
        if (user is null) return NotFound(new { success = false, message = "Kullanıcı bulunamadı." });

        if (!isSuperAdmin && user.SalesRepresentativeId != currentUserId)
            return Forbid();

        user.FirstName = dto.FirstName;
        user.LastName  = dto.LastName;
        user.Phone     = dto.Phone;
        user.Company   = dto.Company;
        user.City      = dto.City;
        user.Address   = dto.Address;
        user.Role      = dto.Role;
        user.IsActive  = dto.IsActive;
        user.GlobalDiscountRate = dto.GlobalDiscountRate;
        user.SalesRepresentativeId = dto.SalesRepresentativeId;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(new { success = true, message = "Kullanıcı güncellendi." });
    }

    [HttpDelete("users/{id:int}")]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken ct)
    {
        var currentUserId = currentUser.UserId ?? 0;
        var isSuperAdmin = currentUser.Role == "SuperAdmin";

        var user = await db.Users.FindAsync([id], ct);
        if (user is null) return NotFound(new { success = false, message = "Kullanıcı bulunamadı." });

        if (!isSuperAdmin && user.SalesRepresentativeId != currentUserId)
            return Forbid();

        // Soft delete — tamamen silmek yerine pasif yap
        user.IsActive  = false;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { success = true, message = "Kullanıcı pasif yapıldı." });
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserDto dto, CancellationToken ct)
    {
        var isSuperAdmin = currentUser.Role == "SuperAdmin";
        if (!isSuperAdmin)
            return Forbid();

        var exists = await db.Users.AnyAsync(u => u.Email == dto.Email, ct);
        if (exists)
            return BadRequest(new { success = false, message = "Bu e-posta adresi zaten kullanımda." });

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email.ToLowerInvariant().Trim(),
            PasswordHash = passwordHasher.Hash(dto.Password),
            Phone = dto.Phone,
            Company = dto.Company,
            City = dto.City,
            Address = dto.Address,
            Role = dto.Role,
            IsActive = true,
            GlobalDiscountRate = dto.GlobalDiscountRate,
            SalesRepresentativeId = dto.SalesRepresentativeId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);

        if (dto.Role == UserRole.Customer)
        {
            var cart = new Cart { User = user };
            db.Carts.Add(cart);
        }

        await db.SaveChangesAsync(ct);
        return StatusCode(201, new { success = true, message = "Kullanıcı başarıyla oluşturuldu.", data = new { user.Id } });
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
        var currentUserId = currentUser.UserId ?? 0;
        var isSuperAdmin = currentUser.Role == "SuperAdmin";

        limit = Math.Clamp(limit, 1, 100);
        var query = db.Orders.Include(o => o.User).Include(o => o.OrderItems).AsQueryable();

        if (!isSuperAdmin)
        {
            query = query.Where(o => o.User.SalesRepresentativeId == currentUserId);
        }

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

    [HttpDelete("orders/{id:int}")]
    public async Task<IActionResult> DeleteOrder(int id, CancellationToken ct)
    {
        var order = await db.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null) return NotFound(new { success = false, message = "Sipariş bulunamadı." });

        // Delete items explicitly
        if (order.OrderItems.Any())
        {
            db.OrderItems.RemoveRange(order.OrderItems);
        }

        db.Orders.Remove(order);
        await db.SaveChangesAsync(ct);

        return Ok(new { success = true, message = "Sipariş ve ilişkili tüm kayıtlar silindi." });
    }

    [HttpDelete("orders/{orderId:int}/items/{itemId:int}")]
    public async Task<IActionResult> DeleteOrderItem(int orderId, int itemId, CancellationToken ct)
    {
        var order = await db.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null) return NotFound(new { success = false, message = "Sipariş bulunamadı." });

        var item = order.OrderItems.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return NotFound(new { success = false, message = "Sipariş kalemi bulunamadı." });

        db.OrderItems.Remove(item);
        
        // Recalculate order total amount (excluding the item we just removed)
        order.TotalAmount = order.OrderItems.Where(i => i.Id != itemId).Sum(i => i.Quantity * i.UnitPrice);
        order.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return Ok(new { 
            success = true, 
            message = "Sipariş kalemi silindi ve sipariş tutarı güncellendi.",
            data = new {
                order.TotalAmount
            }
        });
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
        var currentUserId = currentUser.UserId ?? 0;
        var isSuperAdmin = currentUser.Role == "SuperAdmin";

        limit = Math.Clamp(limit, 1, 100);
        var query = db.Payments
            .Include(p => p.User)
            .AsQueryable();

        if (!isSuperAdmin)
        {
            query = query.Where(p => p.User != null && p.User.SalesRepresentativeId == currentUserId);
        }

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
                p.UserId,
                CustomerName = p.User != null ? p.User.FirstName + " " + p.User.LastName : null
            })
            .ToListAsync(ct);

        return Ok(new { success = true, data = payments, total, totalAmount, page, limit });
    }

    [HttpPost("payments")]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto, CancellationToken ct)
    {
        if (!dto.UserId.HasValue)
        {
            return BadRequest(new { success = false, message = "Ödeme bir müşteri ile ilişkilendirilmelidir." });
        }

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
            UserId      = dto.UserId.Value,
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);
        return StatusCode(201, new { success = true, data = new { payment.Id, payment.PaymentNumber } });
    }

    [HttpPut("payments/{id:int}")]
    public async Task<IActionResult> UpdatePayment(int id, [FromBody] CreatePaymentDto dto, CancellationToken ct)
    {
        if (!dto.UserId.HasValue)
        {
            return BadRequest(new { success = false, message = "Ödeme bir müşteri ile ilişkilendirilmelidir." });
        }

        var payment = await db.Payments.FindAsync(new object[] { id }, ct);
        if (payment is null) return NotFound(new { success = false, message = "Ödeme bulunamadı." });

        payment.Amount      = dto.Amount;
        payment.Method      = dto.Method;
        payment.Status      = dto.Status;
        payment.Description = dto.Description;
        payment.Reference   = dto.Reference;
        payment.PaidAt      = dto.PaidAt ?? payment.PaidAt;
        payment.UserId      = dto.UserId.Value;
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

    [HttpGet("sales-representatives")]
    public async Task<IActionResult> GetSalesRepresentatives(CancellationToken ct)
    {
        var admins = await db.Users
            .Where(u => u.Role == UserRole.Admin || u.Role == UserRole.SuperAdmin)
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email })
            .ToListAsync(ct);
        return Ok(new { success = true, data = admins });
    }

    [HttpGet("sales-representatives/performances")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetSalesRepresentativePerformances(CancellationToken ct)
    {
        var admins = await db.Users
            .Where(u => u.Role == UserRole.Admin || u.Role == UserRole.SuperAdmin)
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .ToListAsync(ct);

        var result = new List<object>();

        foreach (var admin in admins)
        {
            var totalClients = await db.Users
                .CountAsync(u => u.SalesRepresentativeId == admin.Id, ct);

            var totalOrders = await db.Orders
                .CountAsync(o => o.User.SalesRepresentativeId == admin.Id, ct);

            var totalSales = await db.Orders
                .Where(o => o.User.SalesRepresentativeId == admin.Id && o.Status != OrderStatus.Cancelled)
                .SumAsync(o => o.TotalAmount, ct);

            var totalPayments = await db.Payments
                .Where(p => p.Status == PaymentStatus.Completed &&
                    p.User != null && p.User.SalesRepresentativeId == admin.Id)
                .SumAsync(p => p.Amount, ct);

            result.Add(new {
                admin.Id,
                FullName = admin.FirstName + " " + admin.LastName,
                admin.Email,
                Role = admin.Role.ToString(),
                TotalClients = totalClients,
                TotalOrders = totalOrders,
                TotalSales = totalSales,
                TotalPayments = totalPayments
            });
        }

        return Ok(new { success = true, data = result });
    }
}

// ─── DTOs (yalnızca Admin'e özel) ─────────────────────────────────────────────

public record AdminUpdateUserDto(
    string FirstName, string LastName,
    string? Phone, string? Company, string? City, string? Address,
    UserRole Role, bool IsActive, decimal GlobalDiscountRate, int? SalesRepresentativeId
);

public record AdminCreateUserDto(
    string FirstName, string LastName,
    string Email, string Password,
    string? Phone, string? Company, string? City, string? Address,
    UserRole Role, decimal GlobalDiscountRate, int? SalesRepresentativeId
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
