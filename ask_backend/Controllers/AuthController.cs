using ASK.Application.DTOs.Auth;
using ASK.Application.Features.Auth.Commands.Login;
using ASK.Application.Features.Auth.Commands.RefreshToken;
using ASK.Application.Features.Auth.Commands.Register;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ask_backend.Controllers;

/// <summary>
/// Kullanıcı kayıt, giriş ve token yenileme işlemleri.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(IMediator mediator) : ControllerBase
{
    /// <summary>Yeni kullanıcı kaydı.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RegisterCommand(dto.FirstName, dto.LastName, dto.Email, dto.Password, dto.Phone, dto.Company),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created,
            new { success = true, data = result, message = "Kayıt başarılı." });
    }

    /// <summary>Kullanıcı girişi, JWT token döndürür.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new LoginCommand(dto.Email, dto.Password), cancellationToken);
        return Ok(new { success = true, data = result });
    }

    /// <summary>Access token yenileme.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RefreshTokenCommand(dto.RefreshToken), cancellationToken);
        return Ok(new { success = true, data = result });
    }
}
