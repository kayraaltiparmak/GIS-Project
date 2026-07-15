using System.Security.Claims;
using backend.Data;
using backend.DTOs;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace backend.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IUserPermissionService _userPermissionService;

    public AdminController(AppDbContext context, IUserPermissionService userPermissionService)
    {
        _context = context;
        _userPermissionService = userPermissionService;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        if (!await HasAdminPermission("User.Manage"))
        {
            return Forbid();
        }

        var users = await _context.Users
            .Include(user => user.Role)
            .Where(user => !user.IsDeleted)
            .OrderBy(user => user.Id)
            .Select(user => new
            {
                user.Id,
                user.Username,
                user.RoleId,
                RoleName = user.Role != null && !user.Role.IsDeleted ? user.Role.Name : null,
                user.IsActive,
                user.IsDeleted,
                user.ModifiedDate
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(AdminUserRequest request)
    {
        if (!await HasAdminPermission("User.Manage"))
        {
            return Forbid();
        }

        var username = request.Username.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Kullanici adi ve sifre zorunludur.");
        }

        if (await UsernameExists(username))
        {
            return Conflict("Bu kullanici adi zaten kullaniliyor.");
        }

        if (!await RoleExists(request.RoleId))
        {
            return BadRequest("Rol bulunamadi.");
        }

        var user = new User
        {
            Username = username,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId = request.RoleId,
            IsActive = request.IsActive,
            IsDeleted = false,
            ModifiedDate = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, new { user.Id });
    }

    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(int id, AdminUserRequest request)
    {
        if (!await HasAdminPermission("User.Manage"))
        {
            return Forbid();
        }

        var user = await _context.Users.FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);

        if (user == null)
        {
            return NotFound();
        }

        var username = request.Username.Trim();

        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest("Kullanici adi zorunludur.");
        }

        if (await UsernameExists(username, id))
        {
            return Conflict("Bu kullanici adi zaten kullaniliyor.");
        }

        if (!await RoleExists(request.RoleId))
        {
            return BadRequest("Rol bulunamadi.");
        }

        user.Username = username;
        user.RoleId = request.RoleId;
        user.IsActive = request.IsActive;
        user.ModifiedDate = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        if (!await HasAdminPermission("User.Manage"))
        {
            return Forbid();
        }

        var user = await _context.Users.FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);

        if (user == null)
        {
            return NotFound();
        }

        user.IsDeleted = true;
        user.IsActive = false;
        user.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        if (!await HasAdminPermission("Role.Manage"))
        {
            return Forbid();
        }

        var roles = await _context.TblRoles
            .Where(role => !role.IsDeleted)
            .OrderBy(role => role.Id)
            .ToListAsync();

        return Ok(roles);
    }

    [HttpGet("role-options")]
    public async Task<IActionResult> GetRoleOptions()
    {
        if (!await HasAdminPermission("Permission.Manage"))
        {
            return Forbid();
        }

        var roles = await _context.TblRoles
            .Where(role => role.IsActive && !role.IsDeleted)
            .OrderBy(role => role.Name)
            .Select(role => new { role.Id, role.Name })
            .ToListAsync();

        return Ok(roles);
    }

    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole(AdminRoleRequest request)
    {
        if (!await HasAdminPermission("Role.Manage"))
        {
            return Forbid();
        }

        var name = request.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Rol adi zorunludur.");
        }

        if (await _context.TblRoles.AnyAsync(role => role.Name.ToLower() == name.ToLower() && !role.IsDeleted))
        {
            return Conflict("Bu rol adi zaten kullaniliyor.");
        }

        var role = new TblRole
        {
            Name = name,
            Description = request.Description.Trim(),
            IsActive = request.IsActive,
            IsDeleted = false,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _context.TblRoles.Add(role);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRoles), new { id = role.Id }, new { role.Id });
    }

    [HttpPut("roles/{id}")]
    public async Task<IActionResult> UpdateRole(int id, AdminRoleRequest request)
    {
        if (!await HasAdminPermission("Role.Manage"))
        {
            return Forbid();
        }

        var role = await _context.TblRoles.FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);

        if (role == null)
        {
            return NotFound();
        }

        var name = request.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Rol adi zorunludur.");
        }

        if (await _context.TblRoles.AnyAsync(item => item.Id != id && item.Name.ToLower() == name.ToLower() && !item.IsDeleted))
        {
            return Conflict("Bu rol adi zaten kullaniliyor.");
        }

        role.Name = name;
        role.Description = request.Description.Trim();
        role.IsActive = request.IsActive;
        role.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("roles/{id}")]
    public async Task<IActionResult> DeleteRole(int id)
    {
        if (!await HasAdminPermission("Role.Manage"))
        {
            return Forbid();
        }

        var role = await _context.TblRoles.FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);

        if (role == null)
        {
            return NotFound();
        }

        role.IsDeleted = true;
        role.IsActive = false;
        role.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissions()
    {
        if (!await HasAdminPermission("Permission.Manage"))
        {
            return Forbid();
        }

        var permissions = await _context.TblPermissions
            .Where(permission => !permission.IsDeleted)
            .OrderBy(permission => permission.Code)
            .ToListAsync();

        return Ok(permissions);
    }

    [HttpGet("user-options")]
    public async Task<IActionResult> GetUserOptions()
    {
        if (!await HasAdminPermission("Permission.Manage"))
        {
            return Forbid();
        }

        var users = await _context.Users
            .Where(user => user.IsActive && !user.IsDeleted)
            .OrderBy(user => user.Username)
            .Select(user => new { user.Id, user.Username })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost("permissions")]
    public async Task<IActionResult> CreatePermission(AdminPermissionRequest request)
    {
        if (!await HasAdminPermission("Permission.Manage"))
        {
            return Forbid();
        }

        var code = request.Code.Trim();

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(code))
        {
            return BadRequest("Yetki adi ve kodu zorunludur.");
        }

        if (await _context.TblPermissions.AnyAsync(permission => permission.Code.ToLower() == code.ToLower() && !permission.IsDeleted))
        {
            return Conflict("Bu yetki kodu zaten kullaniliyor.");
        }

        var permission = new TblPermission
        {
            Name = request.Name.Trim(),
            Code = code,
            Description = request.Description.Trim(),
            IsActive = request.IsActive,
            IsDeleted = false,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _context.TblPermissions.Add(permission);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPermissions), new { id = permission.Id }, new { permission.Id });
    }

    [HttpPut("permissions/{id}")]
    public async Task<IActionResult> UpdatePermission(int id, AdminPermissionRequest request)
    {
        if (!await HasAdminPermission("Permission.Manage"))
        {
            return Forbid();
        }

        var permission = await _context.TblPermissions.FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);

        if (permission == null)
        {
            return NotFound();
        }

        var code = request.Code.Trim();

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(code))
        {
            return BadRequest("Yetki adi ve kodu zorunludur.");
        }

        if (await _context.TblPermissions.AnyAsync(item => item.Id != id && item.Code.ToLower() == code.ToLower() && !item.IsDeleted))
        {
            return Conflict("Bu yetki kodu zaten kullaniliyor.");
        }

        permission.Name = request.Name.Trim();
        permission.Code = code;
        permission.Description = request.Description.Trim();
        permission.IsActive = request.IsActive;
        permission.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("permissions/{id}")]
    public async Task<IActionResult> DeletePermission(int id)
    {
        if (!await HasAdminPermission("Permission.Manage"))
        {
            return Forbid();
        }

        var permission = await _context.TblPermissions.FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);

        if (permission == null)
        {
            return NotFound();
        }

        permission.IsDeleted = true;
        permission.IsActive = false;
        permission.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("role-permissions/{roleId}")]
    public async Task<IActionResult> GetRolePermissions(int roleId)
    {
        if (!await HasAdminPermission("Permission.Manage"))
        {
            return Forbid();
        }

        var assignedIds = await _context.TblRolePermissions
            .Where(item => item.RoleId == roleId && item.IsActive && !item.IsDeleted)
            .Select(item => item.PermissionId)
            .ToListAsync();

        var permissions = await _context.TblPermissions
            .Where(permission => permission.IsActive && !permission.IsDeleted)
            .OrderBy(permission => permission.Code)
            .Select(permission => new
            {
                permission.Id,
                permission.Name,
                permission.Code,
                permission.Description,
                Assigned = assignedIds.Contains(permission.Id)
            })
            .ToListAsync();

        return Ok(permissions);
    }

    [HttpPut("role-permissions/{roleId}")]
    public async Task<IActionResult> UpdateRolePermissions(int roleId, PermissionAssignmentRequest request)
    {
        if (!await HasAdminPermission("Permission.Manage"))
        {
            return Forbid();
        }

        if (!await _context.TblRoles.AnyAsync(role => role.Id == roleId && role.IsActive && !role.IsDeleted))
        {
            return NotFound();
        }

        var permissionIds = await _context.TblPermissions
            .Where(permission => request.PermissionIds.Contains(permission.Id) && permission.IsActive && !permission.IsDeleted)
            .Select(permission => permission.Id)
            .ToListAsync();

        var existing = await _context.TblRolePermissions
            .Where(item => item.RoleId == roleId)
            .ToListAsync();

        foreach (var rolePermission in existing)
        {
            var shouldBeActive = permissionIds.Contains(rolePermission.PermissionId);
            rolePermission.IsActive = shouldBeActive;
            rolePermission.IsDeleted = !shouldBeActive;
            rolePermission.ModifiedDate = DateTime.UtcNow;
        }

        var existingIds = existing.Select(item => item.PermissionId).ToHashSet();
        var newRolePermissions = permissionIds
            .Where(permissionId => !existingIds.Contains(permissionId))
            .Select(permissionId => new TblRolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                IsActive = true,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            });

        _context.TblRolePermissions.AddRange(newRolePermissions);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("user-permissions/{userId}")]
    public async Task<IActionResult> GetUserPermissions(int userId)
    {
        if (!await HasAdminPermission("Permission.Manage"))
        {
            return Forbid();
        }

        var user = await _context.Users.FirstOrDefaultAsync(item => item.Id == userId && item.IsActive && !item.IsDeleted);

        if (user == null)
        {
            return NotFound();
        }

        var rolePermissionIds = user.RoleId == null
            ? []
            : await _context.TblRolePermissions
                .Where(item => item.RoleId == user.RoleId && item.IsActive && !item.IsDeleted)
                .Select(item => item.PermissionId)
                .ToListAsync();

        var extraPermissionIds = await _context.TblUserPermissions
            .Where(item => item.UserId == userId && item.IsActive && !item.IsDeleted)
            .Select(item => item.PermissionId)
            .ToListAsync();

        var permissions = await _context.TblPermissions
            .Where(permission => permission.IsActive && !permission.IsDeleted)
            .OrderBy(permission => permission.Code)
            .Select(permission => new
            {
                permission.Id,
                permission.Name,
                permission.Code,
                permission.Description,
                FromRole = rolePermissionIds.Contains(permission.Id),
                Extra = extraPermissionIds.Contains(permission.Id),
                Source = rolePermissionIds.Contains(permission.Id)
                    ? "Rolden Geldi"
                    : extraPermissionIds.Contains(permission.Id)
                        ? "Ekstra Yetki"
                        : ""
            })
            .ToListAsync();

        return Ok(permissions);
    }

    [HttpPut("user-permissions/{userId}")]
    public async Task<IActionResult> UpdateUserPermissions(int userId, PermissionAssignmentRequest request)
    {
        if (!await HasAdminPermission("Permission.Manage"))
        {
            return Forbid();
        }

        var user = await _context.Users.FirstOrDefaultAsync(item => item.Id == userId && item.IsActive && !item.IsDeleted);

        if (user == null)
        {
            return NotFound();
        }

        var rolePermissionIds = user.RoleId == null
            ? []
            : await _context.TblRolePermissions
                .Where(item => item.RoleId == user.RoleId && item.IsActive && !item.IsDeleted)
                .Select(item => item.PermissionId)
                .ToListAsync();

        var extraPermissionIds = await _context.TblPermissions
            .Where(permission =>
                request.PermissionIds.Contains(permission.Id) &&
                !rolePermissionIds.Contains(permission.Id) &&
                permission.IsActive &&
                !permission.IsDeleted)
            .Select(permission => permission.Id)
            .ToListAsync();

        var existing = await _context.TblUserPermissions
            .Where(item => item.UserId == userId)
            .ToListAsync();

        foreach (var userPermission in existing)
        {
            var shouldBeActive = extraPermissionIds.Contains(userPermission.PermissionId);
            userPermission.IsActive = shouldBeActive;
            userPermission.IsDeleted = !shouldBeActive;
            userPermission.ModifiedDate = DateTime.UtcNow;
        }

        var existingIds = existing.Select(item => item.PermissionId).ToHashSet();
        var newUserPermissions = extraPermissionIds
            .Where(permissionId => !existingIds.Contains(permissionId))
            .Select(permissionId => new TblUserPermission
            {
                UserId = userId,
                PermissionId = permissionId,
                IsActive = true,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            });

        _context.TblUserPermissions.AddRange(newUserPermissions);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("user-geographic-permissions/{userId}")]
    public async Task<IActionResult> GetUserGeographicPermissions(int userId)
    {
        if (!await HasAdminPermission("User.Manage"))
        {
            return Forbid();
        }

        var permissions = await _context.TblUserGeographicPermissions
            .Where(item => item.UserId == userId && !item.IsDeleted)
            .OrderByDescending(item => item.ModifiedDate)
            .ToListAsync();

        return Ok(permissions.Select(ToGeographicResponse));
    }

    [HttpPost("user-geographic-permissions/{userId}")]
    public async Task<IActionResult> CreateUserGeographicPermission(int userId, GeographicPermissionRequest request)
    {
        if (!await HasAdminPermission("User.Manage"))
        {
            return Forbid();
        }

        if (!await _context.Users.AnyAsync(user => user.Id == userId && user.IsActive && !user.IsDeleted))
        {
            return NotFound();
        }

        var geometry = ReadPolygon(request.Wkt);

        if (geometry == null)
        {
            return BadRequest("Gecersiz polygon geometrisi.");
        }

        await DisableActiveUserGeographicPermissions(userId);

        var permission = new TblUserGeographicPermission
        {
            UserId = userId,
            Geometry = geometry,
            IsActive = true,
            IsDeleted = false,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _context.TblUserGeographicPermissions.Add(permission);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUserGeographicPermissions), new { userId }, ToGeographicResponse(permission));
    }

    [HttpPut("user-geographic-permissions/{id}")]
    public async Task<IActionResult> UpdateUserGeographicPermission(int id, GeographicPermissionRequest request)
    {
        if (!await HasAdminPermission("User.Manage"))
        {
            return Forbid();
        }

        var permission = await _context.TblUserGeographicPermissions.FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);

        if (permission == null)
        {
            return NotFound();
        }

        var geometry = ReadPolygon(request.Wkt);

        if (geometry == null)
        {
            return BadRequest("Gecersiz polygon geometrisi.");
        }

        await DisableActiveUserGeographicPermissions(permission.UserId, id);

        permission.Geometry = geometry;
        permission.IsActive = true;
        permission.IsDeleted = false;
        permission.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("user-geographic-permissions/{id}")]
    public async Task<IActionResult> DeleteUserGeographicPermission(int id)
    {
        if (!await HasAdminPermission("User.Manage"))
        {
            return Forbid();
        }

        var permission = await _context.TblUserGeographicPermissions.FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);

        if (permission == null)
        {
            return NotFound();
        }

        permission.IsDeleted = true;
        permission.IsActive = false;
        permission.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("role-geographic-permissions/{roleId}")]
    public async Task<IActionResult> GetRoleGeographicPermissions(int roleId)
    {
        if (!await HasAdminPermission("Role.Manage"))
        {
            return Forbid();
        }

        var permissions = await _context.TblRoleGeographicPermissions
            .Where(item => item.RoleId == roleId && !item.IsDeleted)
            .OrderByDescending(item => item.ModifiedDate)
            .ToListAsync();

        return Ok(permissions.Select(ToGeographicResponse));
    }

    [HttpPost("role-geographic-permissions/{roleId}")]
    public async Task<IActionResult> CreateRoleGeographicPermission(int roleId, GeographicPermissionRequest request)
    {
        if (!await HasAdminPermission("Role.Manage"))
        {
            return Forbid();
        }

        if (!await _context.TblRoles.AnyAsync(role => role.Id == roleId && role.IsActive && !role.IsDeleted))
        {
            return NotFound();
        }

        var geometry = ReadPolygon(request.Wkt);

        if (geometry == null)
        {
            return BadRequest("Gecersiz polygon geometrisi.");
        }

        await DisableActiveRoleGeographicPermissions(roleId);

        var permission = new TblRoleGeographicPermission
        {
            RoleId = roleId,
            Geometry = geometry,
            IsActive = true,
            IsDeleted = false,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _context.TblRoleGeographicPermissions.Add(permission);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRoleGeographicPermissions), new { roleId }, ToGeographicResponse(permission));
    }

    [HttpPut("role-geographic-permissions/{id}")]
    public async Task<IActionResult> UpdateRoleGeographicPermission(int id, GeographicPermissionRequest request)
    {
        if (!await HasAdminPermission("Role.Manage"))
        {
            return Forbid();
        }

        var permission = await _context.TblRoleGeographicPermissions.FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);

        if (permission == null)
        {
            return NotFound();
        }

        var geometry = ReadPolygon(request.Wkt);

        if (geometry == null)
        {
            return BadRequest("Gecersiz polygon geometrisi.");
        }

        await DisableActiveRoleGeographicPermissions(permission.RoleId, id);

        permission.Geometry = geometry;
        permission.IsActive = true;
        permission.IsDeleted = false;
        permission.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("role-geographic-permissions/{id}")]
    public async Task<IActionResult> DeleteRoleGeographicPermission(int id)
    {
        if (!await HasAdminPermission("Role.Manage"))
        {
            return Forbid();
        }

        var permission = await _context.TblRoleGeographicPermissions.FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);

        if (permission == null)
        {
            return NotFound();
        }

        permission.IsDeleted = true;
        permission.IsActive = false;
        permission.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<bool> HasAdminPermission(string permissionCode)
    {
        var userId = GetUserId();

        return userId != null && await _userPermissionService.HasPermissionAsync(userId.Value, permissionCode);
    }

    private int? GetUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(userIdValue, out var userId) ? userId : null;
    }

    private async Task<bool> UsernameExists(string username, int? exceptUserId = null)
    {
        return await _context.Users.AnyAsync(user =>
            user.Id != exceptUserId &&
            user.Username.ToLower() == username.ToLower() &&
            !user.IsDeleted);
    }

    private async Task<bool> RoleExists(int? roleId)
    {
        return roleId == null ||
               await _context.TblRoles.AnyAsync(role => role.Id == roleId && role.IsActive && !role.IsDeleted);
    }

    private async Task DisableActiveUserGeographicPermissions(int userId, int? exceptId = null)
    {
        var activePermissions = await _context.TblUserGeographicPermissions
            .Where(item => item.UserId == userId && item.IsActive && !item.IsDeleted && item.Id != exceptId)
            .ToListAsync();

        foreach (var permission in activePermissions)
        {
            permission.IsActive = false;
            permission.ModifiedDate = DateTime.UtcNow;
        }
    }

    private async Task DisableActiveRoleGeographicPermissions(int roleId, int? exceptId = null)
    {
        var activePermissions = await _context.TblRoleGeographicPermissions
            .Where(item => item.RoleId == roleId && item.IsActive && !item.IsDeleted && item.Id != exceptId)
            .ToListAsync();

        foreach (var permission in activePermissions)
        {
            permission.IsActive = false;
            permission.ModifiedDate = DateTime.UtcNow;
        }
    }

    private static Polygon? ReadPolygon(string wkt)
    {
        if (string.IsNullOrWhiteSpace(wkt))
        {
            return null;
        }

        try
        {
            var geometry = new WKTReader().Read(wkt);
            geometry.SRID = 4326;

            return geometry is Polygon polygon && polygon.IsValid ? polygon : null;
        }
        catch
        {
            return null;
        }
    }

    private static object ToGeographicResponse(TblUserGeographicPermission permission)
    {
        return new
        {
            permission.Id,
            permission.UserId,
            Wkt = new WKTWriter().Write(permission.Geometry),
            permission.IsActive,
            permission.IsDeleted,
            permission.CreatedDate,
            permission.ModifiedDate
        };
    }

    private static object ToGeographicResponse(TblRoleGeographicPermission permission)
    {
        return new
        {
            permission.Id,
            permission.RoleId,
            Wkt = new WKTWriter().Write(permission.Geometry),
            permission.IsActive,
            permission.IsDeleted,
            permission.CreatedDate,
            permission.ModifiedDate
        };
    }
}
