using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartMediGuard.API.Data;
using SmartMediGuard.API.DTOs;
using SmartMediGuard.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SmartMediGuard.API.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // ─── KAYIT OL ───────────────────────────────────────────
    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
    {
        // Email daha önce kayıtlı mı?
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return null;

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Age = dto.Age,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new AuthResponseDto
        {
            Token = GenerateToken(user),
            Name = user.Name,
            UserId = user.Id
        };
    }

    // ─── GİRİŞ YAP ──────────────────────────────────────────
    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null) return null;
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash)) return null;

        return new AuthResponseDto
        {
            Token = GenerateToken(user),
            Name = user.Name,
            UserId = user.Id
        };
    }

    // ─── HESAP SİL ───────────────────────────────────────────
    public async Task<bool> DeleteAccountAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return false;

        user.IsActive  = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.Medications
            .Where(m => m.UserId == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.IsActive,   false)
                .SetProperty(m => m.UpdatedAt,  DateTime.UtcNow));

        await _db.SaveChangesAsync();
        return true;
    }

    // ─── PROFİL GÜNCELLE ────────────────────────────────────
    public async Task<bool> UpdateProfileAsync(int userId, string name)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return false;
        user.Name      = name;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    // ─── JWT TOKEN ÜRET ─────────────────────────────────────
    private string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim(ClaimTypes.Name, user.Name),
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}