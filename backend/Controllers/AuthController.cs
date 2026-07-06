using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend.Data;
using backend.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Username == request.Username &&
                    u.IsActive == true &&
                    u.IsDeleted == false);

            if (user == null || !IsPasswordValid(request.Password, user.Password))
            {
                return Unauthorized("Kullanici adi veya sifre hatali.");
            }

            if (!IsBCryptHash(user.Password))
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
                user.ModifiedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

            var creds = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(10),
                signingCredentials: creds);

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token)
            });
        }
        catch
        {
            return StatusCode(500, "Beklenmeyen bir hata olustu.");
        }
    }

    private static bool IsPasswordValid(string password, string storedPassword)
    {
        if (IsBCryptHash(storedPassword))
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, storedPassword);
            }
            catch
            {
                return false;
            }
        }

        return storedPassword == password;
    }

    private static bool IsBCryptHash(string password)
    {
        return !string.IsNullOrWhiteSpace(password) &&
               (password.StartsWith("$2a$") ||
                password.StartsWith("$2b$") ||
                password.StartsWith("$2y$"));
    }
}
