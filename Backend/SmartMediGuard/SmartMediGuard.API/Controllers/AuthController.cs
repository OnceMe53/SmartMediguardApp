using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMediGuard.API.DTOs;
using SmartMediGuard.API.Services;
using System.Security.Claims;

namespace SmartMediGuard.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // POST /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        if (result == null)
            return BadRequest(new { message = "Bu email zaten kayıtlı." });
        return Ok(result);
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        if (result == null)
            return Unauthorized(new { message = "Email veya şifre yanlış." });
        return Ok(result);
    }

    // DELETE /api/auth/account
    [HttpDelete("account"), Authorize]
    public async Task<IActionResult> DeleteAccount()
    {
        var ok = await _authService.DeleteAccountAsync(UserId);
        if (!ok) return NotFound();
        return Ok(new { message = "Hesap silindi." });
    }

    // PUT /api/auth/profile
    [HttpPut("profile"), Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var ok = await _authService.UpdateProfileAsync(UserId, dto.Name);
        if (!ok) return NotFound();
        return Ok(new { message = "Profil güncellendi." });
    }
}