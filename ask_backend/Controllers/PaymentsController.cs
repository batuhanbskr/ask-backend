using System.Net;
using ASK.Application.Common.Interfaces;
using ASK.Domain.Entities;
using ASK.Domain.Enums;
using ASK.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ask_backend.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController(
    AppDbContext db,
    ICurrentUserService currentUser,
    ITamiPaymentService tamiPaymentService) : ControllerBase
{
    [HttpPost("pay-online")]
    [Authorize]
    public async Task<IActionResult> PayOnline([FromBody] PayOnlineDto dto, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? 0;
        if (userId <= 0)
        {
            return Unauthorized(new { success = false, message = "Giriş yapmanız gerekmektedir." });
        }

        if (dto.Amount <= 0)
        {
            return BadRequest(new { success = false, message = "Ödeme tutarı 0'dan büyük olmalıdır." });
        }

        if (string.IsNullOrWhiteSpace(dto.CardHolderName) || 
            string.IsNullOrWhiteSpace(dto.CardNumber) || 
            dto.ExpireMonth <= 0 || 
            dto.ExpireYear <= 0 || 
            string.IsNullOrWhiteSpace(dto.Cvv))
        {
            return BadRequest(new { success = false, message = "Kart bilgileri eksiksiz girilmelidir." });
        }

        // Construct backend callback URL dynamically
        var callbackUrl = $"{Request.Scheme}://{Request.Host}/api/payments/tami-callback";

        // Initiate payment on Tami gateway
        var result = await tamiPaymentService.Initiate3DPaymentAsync(
            userId,
            dto.Amount,
            dto.CardHolderName,
            dto.CardNumber,
            dto.ExpireMonth,
            dto.ExpireYear,
            dto.Cvv,
            callbackUrl,
            ct);

        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.ErrorMessage ?? "Ödeme başlatılamadı." });
        }

        // Retrieve the B2B frontend origin from Origin or Referer header to redirect dynamically later
        var origin = Request.Headers["Origin"].ToString();
        if (string.IsNullOrEmpty(origin))
        {
            origin = Request.Headers["Referer"].ToString();
        }
        if (!string.IsNullOrEmpty(origin))
        {
            try
            {
                var uri = new Uri(origin);
                origin = $"{uri.Scheme}://{uri.Authority}";
            }
            catch
            {
                origin = "https://b2b.askteknikhirdavat.com";
            }
        }
        else
        {
            origin = "https://b2b.askteknikhirdavat.com";
        }

        // Save a Pending payment record in our DB
        var count = await db.Payments.CountAsync(ct) + 1;
        var payment = new Payment
        {
            PaymentNumber = result.OrderId ?? $"PAY-{DateTime.UtcNow:yyyyMMdd}-{count:D5}",
            Amount = dto.Amount,
            Method = PaymentMethod.VirtualPos,
            Status = PaymentStatus.Pending,
            Description = "Cari Bakiye Online Ödeme",
            UserId = userId,
            Reference = origin, // Save the initiating B2B portal origin here
            PaidAt = DateTime.UtcNow
        };

        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);

        // Return the HTML content to render/execute redirect on frontend
        return Ok(new
        {
            success = true,
            threeDSHtmlContent = result.ThreeDSHtmlContent,
            orderId = result.OrderId
        });
    }

    [HttpPost("tami-callback")]
    [Consumes("application/x-www-form-urlencoded")]
    [AllowAnonymous]
    public async Task<IActionResult> TamiCallback(CancellationToken ct)
    {
        var orderId = Request.Form["orderId"].ToString();
        if (string.IsNullOrEmpty(orderId))
        {
            orderId = Request.Form["OrderId"].ToString();
        }
        if (string.IsNullOrEmpty(orderId))
        {
            orderId = Request.Query["orderId"].ToString();
        }
        if (string.IsNullOrEmpty(orderId))
        {
            orderId = Request.Query["OrderId"].ToString();
        }

        if (string.IsNullOrEmpty(orderId))
        {
            return BadRequest("Sipariş numarası (orderId) bulunamadı.");
        }

        // Complete/confirm transaction on Tami Payment Gateway
        var result = await tamiPaymentService.Complete3DPaymentAsync(orderId, ct);

        // Fetch corresponding local payment record
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.PaymentNumber == orderId, ct);
        if (payment == null)
        {
            return NotFound("Ödeme kaydı bulunamadı.");
        }

        // Prevent Replay Attacks: If the payment was already completed, just redirect without crediting again
        if (payment.Status == PaymentStatus.Completed)
        {
            var redirectUrl = payment.Reference;
            if (string.IsNullOrEmpty(redirectUrl))
            {
                redirectUrl = "https://b2b.askteknikhirdavat.com";
                if (Request.Host.Host == "localhost" || Request.Host.Host.Contains("127.0.0.1"))
                {
                    redirectUrl = "http://localhost:5174";
                }
            }
            return Redirect($"{redirectUrl}/payment/success?amount={payment.Amount}&orderId={orderId}");
        }

        // Base URLs for B2B Portal pages - use saved Origin, fallback to configuration / defaults
        var b2bBaseUrl = payment.Reference;
        if (string.IsNullOrEmpty(b2bBaseUrl))
        {
            b2bBaseUrl = "https://b2b.askteknikhirdavat.com";
            if (Request.Host.Host == "localhost" || Request.Host.Host.Contains("127.0.0.1"))
            {
                b2bBaseUrl = "http://localhost:5174"; // B2B default local port
            }
        }

        if (result.Success)
        {
            // Payment succeeded!
            payment.Status = PaymentStatus.Completed;
            payment.UpdatedAt = DateTime.UtcNow;

            var user = await db.Users.FindAsync(new object[] { payment.UserId ?? 0 }, ct);
            if (user != null)
            {
                user.CurrentBalance += payment.Amount;
                db.Users.Update(user);
            }

            await db.SaveChangesAsync(ct);

            // Redirect user back to B2B portal's success screen
            return Redirect($"{b2bBaseUrl}/payment/success?amount={payment.Amount}&orderId={orderId}");
        }
        else
        {
            // Payment failed
            payment.Status = PaymentStatus.Failed;
            payment.Description = $"Online ödeme başarısız: {result.ErrorMessage}";
            payment.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            var errorMsg = WebUtility.UrlEncode(result.ErrorMessage ?? "Bilinmeyen banka hatası");
            return Redirect($"{b2bBaseUrl}/payment/fail?error={errorMsg}&orderId={orderId}");
        }
    }
}

public record PayOnlineDto(
    decimal Amount,
    string CardHolderName,
    string CardNumber,
    int ExpireMonth,
    int ExpireYear,
    string Cvv
);
