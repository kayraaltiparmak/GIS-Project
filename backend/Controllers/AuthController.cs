using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend.Data;
using backend.DTOs;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IUserPermissionService _userPermissionService;

    public AuthController(
        AppDbContext context,
        IConfiguration configuration,
        IUserPermissionService userPermissionService)
    {
        _context = context;
        _configuration = configuration;
        _userPermissionService = userPermissionService;
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

            return Ok(new
            {
                token = CreateToken(user)
            });
        }
        catch
        {
            return StatusCode(500, "Beklenmeyen bir hata olustu.");
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        try
        {
            var username = request.Username.Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Kullanici adi ve sifre zorunludur.");
            }

            if (request.Password.Length < 6)
            {
                return BadRequest("Sifre en az 6 karakter olmalidir.");
            }

            var usernameExists = await _context.Users
                .AnyAsync(u =>
                    u.Username.ToLower() == username.ToLower() &&
                    u.IsDeleted == false);

            if (usernameExists)
            {
                return Conflict("Bu kullanici adi zaten kullaniliyor.");
            }

            var user = new User
            {
                Username = username,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                IsActive = true,
                IsDeleted = false,
                ModifiedDate = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                token = CreateToken(user)
            });
        }
        catch
        {
            return StatusCode(500, "Beklenmeyen bir hata olustu.");
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = GetUserId();

        if (userId == null)
        {
            return Unauthorized();
        }

        var currentUser = await _userPermissionService.GetCurrentUserAsync(userId.Value);

        if (currentUser == null)
        {
            return Unauthorized();
        }

        return Ok(currentUser);
    }

    private string CreateToken(User user)
    {
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

        return new JwtSecurityTokenHandler().WriteToken(token);
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

    private int? GetUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
