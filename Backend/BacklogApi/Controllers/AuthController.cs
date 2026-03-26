using BacklogApi.Application.DTOs;
using BacklogApi.Application.Services;
using BacklogApi.Core.Entities;
using BacklogApi.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BacklogApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly IBoardService _boardService;

    public AuthController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration, IBoardService boardService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _boardService = boardService;
    }

    [HttpPost("register")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var user = new ApplicationUser { UserName = dto.Username, Email = dto.Email };
        var result = await _userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded) return BadRequest(result.Errors);

        if (!await _roleManager.RoleExistsAsync("User"))
            await _roleManager.CreateAsync(new IdentityRole("User"));
        
        await _userManager.AddToRoleAsync(user, "User");

        // Create personal board for new user
        try
        {
            await _boardService.CreateAsync(new CreateBoardDto(
                $"Personal Dashboard - {user.UserName}",
                $"Automatic dashboard for {user.UserName}",
                new List<string> { user.UserName! },
                null // Use default columns from BoardService
            ));
        }
        catch (Exception ex)
        {
            // Log error but don't fail registration if board creation fails
            Console.WriteLine($"Failed to create personal board for {user.UserName}: {ex.Message}");
        }

        return Ok(new { Message = "User registered successfully" });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var user = await _userManager.FindByNameAsync(dto.Username);
        if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        var authClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
        };

        foreach (var role in roles)
        {
            authClaims.Add(new Claim(ClaimTypes.Role, role));
        }

        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]!));

        var token = new JwtSecurityToken(
            issuer: _configuration["JWT:ValidIssuer"],
            audience: _configuration["JWT:ValidAudience"],
            expires: DateTime.Now.AddHours(3),
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );

        return Ok(new AuthResponseDto(
            new JwtSecurityTokenHandler().WriteToken(token),
            user.UserName!,
            roles.FirstOrDefault() ?? "User",
            user.ProfilePicture,
            user.TeamsWebhookUrl
        ));
    }

    [HttpGet("users")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<string>>> GetUsers()
    {
        var users = await _userManager.Users.Select(u => u.UserName).ToListAsync();
        return Ok(users);
    }

    [HttpGet("users-list")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsersList()
    {
        var users = await _userManager.Users.ToListAsync();
        var result = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserDto(
                user.UserName!,
                user.Email!,
                roles.FirstOrDefault() ?? "User",
                user.Tags,
                user.ProfilePicture
            ));
        }

        return Ok(result);
    }

    [HttpPut("users/{username}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUser(string username, UpdateUserDto dto)
    {
        var user = await _userManager.FindByNameAsync(username);
        if (user == null) return NotFound();

        user.Email = dto.Email;
        user.Tags = dto.Tags;
        user.ProfilePicture = dto.ProfilePicture;

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _userManager.ResetPasswordAsync(user, token, dto.Password);
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded) return BadRequest(result.Errors);

          var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        
        if (!await _roleManager.RoleExistsAsync(dto.Role))
            await _roleManager.CreateAsync(new IdentityRole(dto.Role));
            
        await _userManager.AddToRoleAsync(user, dto.Role);

        return Ok(new { Message = "User updated successfully" });
    }

    [HttpDelete("users/{username}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(string username)
    {
        if (username.ToLower() == "admin") return BadRequest("Cannot delete admin user");

        var user = await _userManager.FindByNameAsync(username);
        if (user == null) return NotFound();

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded) return BadRequest(result.Errors);

        return Ok(new { Message = "User deleted successfully" });
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return Unauthorized();

        var user = await _userManager.FindByNameAsync(username);
        if (user == null) return NotFound();

        user.ProfilePicture = dto.ProfilePicture;
        user.TeamsWebhookUrl = dto.TeamsWebhookUrl;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded) return BadRequest(result.Errors);

        return Ok(new { Message = "Profil uspešno posodobljen" });
    }

    [HttpPost("seed-admin")]
    public async Task<IActionResult> SeedAdmin([FromServices] BacklogDbContext context)
    {

        string[] roleNames = { "Admin", "Manager", "User" };
        foreach (var roleName in roleNames)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
                await _roleManager.CreateAsync(new IdentityRole(roleName));
        }

        var adminUser = await _userManager.FindByNameAsync("admin");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser { UserName = "admin", Email = "admin@example.com" };
            await _userManager.CreateAsync(adminUser, "Admin123!");
            await _userManager.AddToRoleAsync(adminUser, "Admin");
        }

        var managerUser = await _userManager.FindByNameAsync("manager");
        if (managerUser == null)
        {
            managerUser = new ApplicationUser { UserName = "manager", Email = "manager@example.com" };
            await _userManager.CreateAsync(managerUser, "Manager123!");
            await _userManager.AddToRoleAsync(managerUser, "Manager");
        }

        var normalUser = await _userManager.FindByNameAsync("user");
        if (normalUser == null)
        {
            normalUser = new ApplicationUser { UserName = "user", Email = "user@example.com" };
            await _userManager.CreateAsync(normalUser, "User123!");
            await _userManager.AddToRoleAsync(normalUser, "User");
        }

        if (!context.Boards.Any())
        {
            var defaultBoard = new Board 
            { 
                Name = "Glavna plošča", 
                Description = "Privzeta razvojna plošča",
                CreatedAt = DateTime.UtcNow 
            };
            
            if (adminUser != null) defaultBoard.Users.Add(adminUser);
            if (managerUser != null) defaultBoard.Users.Add(managerUser);
            if (normalUser != null) defaultBoard.Users.Add(normalUser);
            
            context.Boards.Add(defaultBoard);
            await context.SaveChangesAsync();

            if (!context.Columns.Any())
            {
                context.Columns.AddRange(new List<BoardColumn>
                {
                    new BoardColumn { Name = "TODO", Order = 1, Color = "#cbd5e1", BoardId = defaultBoard.Id }
                });
                await context.SaveChangesAsync();
            }
        }
        else if (!context.Columns.Any())
        {
            context.Columns.AddRange(new List<BoardColumn>
            {
                new BoardColumn { Name = "TODO", Order = 1, Color = "#cbd5e1" }
            });
            await context.SaveChangesAsync();
        }

        return Ok(new { Message = "Roles, Users, Default Board and Columns seeded" });
    }
}
