using FoodDeliveryyy.Models.Identity;
using FoodDeliveryyy.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FoodDeliveryyy.Models.DTOs;
using FoodDeliveryyy.Models.Entities;
using FoodDeliveryyy.Models.Enums;

namespace FoodDeliveryyy.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AuthController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<Role> roleManager,
            IConfiguration configuration,
            AppDbContext context,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _context = context;
            _environment = environment;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var username = dto.Username.Trim();
            var email = dto.Email.Trim();
            var role = AppRoles.Normalize(dto.Role);

            if (await _userManager.FindByNameAsync(username) != null)
            {
                return BadRequest(new { message = "Username is already taken." });
            }

            if (await _userManager.FindByEmailAsync(email) != null)
            {
                return BadRequest(new { message = "Email is already registered." });
            }

            if (!await _roleManager.RoleExistsAsync(role))
            {
                role = AppRoles.Customer;
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new Role { Name = role });
                }
            }

            var user = new User { UserName = username, Email = email };
            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded) return BadRequest(result.Errors);

            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return BadRequest(roleResult.Errors);
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateJwtToken(user, roles);
            var refreshToken = CreateRefreshToken(user.Id);
            _context.RefreshTokens.Add(refreshToken);

            if (role == AppRoles.Courier)
            {
                _context.DeliveryDrivers.Add(new DeliveryDrivers
                {
                    UserId = user.Id,
                    Automjeti = "N/A",
                    Targa = "N/A",
                    Zona = "N/A",
                    Statusi = DriverStatus.Available,
                    Vlersimi = 0
                });
            }

            await _context.SaveChangesAsync();
            SetRefreshTokenCookie(refreshToken.Token, refreshToken.Expires);
            return Ok(new { message = "User registered", userId = user.Id, token });
        }

        [HttpPost("admin/set-role")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> SetUserRole([FromBody] SetUserRoleDto dto)
        {
            var normalizedCredential = dto.UsernameOrEmail.Trim();
            if (string.IsNullOrWhiteSpace(normalizedCredential))
            {
                return BadRequest(new { message = "Username or email is required." });
            }

            var requestedRole = AppRoles.Normalize(dto.Role);
            if (!await _roleManager.RoleExistsAsync(requestedRole))
            {
                return BadRequest(new { message = $"Role '{requestedRole}' does not exist." });
            }

            User? user;
            if (normalizedCredential.Contains("@"))
            {
                user = await _userManager.FindByEmailAsync(normalizedCredential);
            }
            else
            {
                user = await _userManager.FindByNameAsync(normalizedCredential);
            }

            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    return BadRequest(removeResult.Errors);
                }
            }

            var addResult = await _userManager.AddToRoleAsync(user, requestedRole);
            if (!addResult.Succeeded)
            {
                return BadRequest(addResult.Errors);
            }

            return Ok(new
            {
                message = "User role updated.",
                userId = user.Id,
                userName = user.UserName,
                role = requestedRole
            });
        }

        [HttpPost("admin/assign-branch-manager")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> AssignBranchManager([FromBody] AssignBranchManagerDto dto)
        {
            var normalizedCredential = dto.UsernameOrEmail.Trim();
            if (string.IsNullOrWhiteSpace(normalizedCredential))
            {
                return BadRequest(new { message = "Username or email is required." });
            }

            var branch = await _context.RestaurantAddresses
                .Include(a => a.Restaurant)
                .FirstOrDefaultAsync(a => a.Id == dto.RestaurantAddressId);

            if (branch == null)
            {
                return NotFound(new { message = "Branch address not found." });
            }

            User? user;
            if (normalizedCredential.Contains("@"))
            {
                user = await _userManager.FindByEmailAsync(normalizedCredential);
            }
            else
            {
                user = await _userManager.FindByNameAsync(normalizedCredential);
            }

            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    return BadRequest(removeResult.Errors);
                }
            }

            var addResult = await _userManager.AddToRoleAsync(user, AppRoles.BranchManager);
            if (!addResult.Succeeded)
            {
                return BadRequest(addResult.Errors);
            }

            branch.MerchantUserId = user.Id;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Branch manager assigned.",
                userId = user.Id,
                userName = user.UserName,
                role = AppRoles.BranchManager,
                branchId = branch.Id,
                restaurantId = branch.RestaurantId,
                branchAddress = branch.Adresa,
                city = branch.Qyteti
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var credential = dto.Username.Trim();
            User? user;

            if (credential.Contains("@"))
            {
                user = await _userManager.FindByEmailAsync(credential);
            }
            else
            {
                user = await _userManager.FindByNameAsync(credential);
            }

            if (user == null)
            {
                return Unauthorized(new { message = "Invalid username/email or password." });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
            if (!result.Succeeded)
            {
                return Unauthorized(new { message = "Invalid username/email or password." });
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateJwtToken(user, roles);
            var refreshToken = CreateRefreshToken(user.Id);
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();
            SetRefreshTokenCookie(refreshToken.Token, refreshToken.Expires);
            return Ok(new { message = "Login successful", userId = user.Id, token });
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh()
        {
            var refreshTokenValue = Request.Cookies["refreshToken"];
            if (string.IsNullOrWhiteSpace(refreshTokenValue))
            {
                return Unauthorized(new { message = "Refresh token is missing." });
            }

            var currentRefreshToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshTokenValue);

            if (currentRefreshToken == null || currentRefreshToken.Revoked != null || currentRefreshToken.Expires <= DateTime.UtcNow)
            {
                return Unauthorized(new { message = "Refresh token is invalid or expired." });
            }

            var user = currentRefreshToken.User ?? await _userManager.FindByIdAsync(currentRefreshToken.UserId);
            if (user == null)
            {
                return Unauthorized(new { message = "User not found." });
            }

            currentRefreshToken.Revoked = DateTime.UtcNow;

            var newRefreshToken = CreateRefreshToken(user.Id);
            _context.RefreshTokens.Add(newRefreshToken);

            var roles = await _userManager.GetRolesAsync(user);
            var newAccessToken = GenerateJwtToken(user, roles);

            await _context.SaveChangesAsync();
            SetRefreshTokenCookie(newRefreshToken.Token, newRefreshToken.Expires);

            return Ok(new { token = newAccessToken });
        }

        [HttpPost("revoke")]
        [AllowAnonymous]
        public async Task<IActionResult> Revoke()
        {
            var refreshTokenValue = Request.Cookies["refreshToken"];

            if (!string.IsNullOrWhiteSpace(refreshTokenValue))
            {
                var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshTokenValue);
                if (refreshToken != null && refreshToken.Revoked == null)
                {
                    refreshToken.Revoked = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            Response.Cookies.Delete("refreshToken", BuildRefreshCookieOptions(DateTime.UtcNow.AddDays(-1)));
            return Ok(new { message = "Session revoked." });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized();
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                user.Id,
                user.UserName,
                user.Email,
                roles
            });
        }

        private string GenerateJwtToken(User user, IList<string> roles)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? Environment.GetEnvironmentVariable("JWT_KEY");
            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                throw new InvalidOperationException("JWT Key is not configured. Set Jwt:Key or JWT_KEY.");
            }
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured.");
            var jwtAudience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured.");

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private RefreshToken CreateRefreshToken(string userId)
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(64);
            return new RefreshToken
            {
                Token = Convert.ToBase64String(tokenBytes),
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                UserId = userId
            };
        }

        private void SetRefreshTokenCookie(string token, DateTime expires)
        {
            Response.Cookies.Append("refreshToken", token, BuildRefreshCookieOptions(expires));
        }

        private CookieOptions BuildRefreshCookieOptions(DateTime expires)
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = !_environment.IsDevelopment(),
                SameSite = SameSiteMode.Lax,
                Expires = expires,
                IsEssential = true
            };
        }
    }
}
