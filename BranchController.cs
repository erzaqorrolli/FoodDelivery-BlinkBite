using FoodDeliveryyy.Data;
using FoodDeliveryyy.Models.Entities;
using FoodDeliveryyy.Models.Identity;
using FoodDeliveryyy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FoodDeliveryyy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BranchController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IEmailService _emailService;

        public BranchController(
            AppDbContext context,
            UserManager<User> userManager,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        // 🔥 METODA E RE - APLIKIM PËR BRANCH TË RI (shkon te admin për aprovim)
        [HttpPost("apply")]
        [Authorize(Roles = AppRoles.Merchant)]
        public async Task<IActionResult> ApplyBranch([FromBody] BranchApplicationDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (restaurant == null)
                return BadRequest(new { message = "No restaurant found for this merchant" });

            // Kontrollo nëse ka aplikim në pritje
            var existingApplication = await _context.BranchApplications
                .FirstOrDefaultAsync(a => a.RestaurantId == restaurant.Id && a.Status == "Pending");

            if (existingApplication != null)
                return BadRequest(new { message = "You already have a pending branch application" });

            var application = new BranchApplication
            {
                RestaurantId = restaurant.Id,
                Address = dto.Address,
                City = dto.City,
                Zone = dto.Zone ?? "",
                DeliveryFee = dto.DeliveryFee,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                IsMain = dto.IsMain,
                CreateBranchManager = dto.CreateBranchManager,
                ManagerName = dto.ManagerName,
                ManagerEmail = dto.ManagerEmail,
                Status = "Pending",
                AppliedAt = DateTime.UtcNow
            };

            _context.BranchApplications.Add(application);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Branch application submitted successfully! Admin will review it." });
        }

        // METODA E VJETËR - KRIJIM DIREKT (vetëm për Admin, pa aprovim)
        [HttpPost("create")]
        [Authorize(Roles = AppRoles.Admin)]  // 🔥 Ndryshuar: vetëm Admin mund të krijojë direkt
        public async Task<IActionResult> CreateBranchDirect([FromBody] CreateBranchDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Address))
                {
                    return BadRequest(new { message = "Address is required" });
                }

                if (string.IsNullOrWhiteSpace(dto.City))
                {
                    return BadRequest(new { message = "City is required" });
                }

                var restaurant = await _context.Restaurants
                    .FirstOrDefaultAsync(r => r.Id == dto.RestaurantId);

                if (restaurant == null)
                {
                    return BadRequest(new { message = "Restaurant not found" });
                }

                var branch = new RestaurantAddress
                {
                    Adresa = dto.Address,
                    Qyteti = dto.City,
                    Zona = dto.Zone ?? "",
                    IsMain = dto.IsMain,
                    IsActive = dto.IsActive,
                    Latitude = dto.Latitude,
                    Longitude = dto.Longitude,
                    TarifaDorezimit = dto.DeliveryFee,
                    RestaurantId = restaurant.Id,
                    MerchantUserId = null
                };

                _context.RestaurantAddresses.Add(branch);
                await _context.SaveChangesAsync();

                int? branchManagerId = null;

                if (dto.CreateBranchManager && !string.IsNullOrEmpty(dto.ManagerEmail))
                {
                    var username = GenerateUsername(dto.ManagerName ?? dto.ManagerEmail.Split('@')[0]);
                    var password = GenerateRandomPassword();

                    var branchManager = new User
                    {
                        UserName = username,
                        Email = dto.ManagerEmail,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var createResult = await _userManager.CreateAsync(branchManager, password);

                    if (createResult.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(branchManager, "BranchManager");
                        branch.MerchantUserId = branchManager.Id;
                        await _context.SaveChangesAsync();
                        branchManagerId = branch.Id;

                        try
                        {
                            await _emailService.SendBranchManagerCredentialsEmailAsync(
                                branchManager.Email,
                                branch.Adresa,
                                restaurant.Emertimi,
                                branchManager.UserName,
                                password
                            );
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Email failed: {ex.Message}");
                        }
                    }
                }

                return Ok(new
                {
                    message = "Branch created successfully",
                    branchId = branch.Id,
                    branchManagerCreated = dto.CreateBranchManager && branchManagerId != null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error creating branch: {ex.Message}" });
            }
        }

        // MERRE BRANCH-IN SIPAS ID
        [HttpGet("{id}")]
        [Authorize(Roles = AppRoles.Merchant + "," + AppRoles.Admin + "," + AppRoles.BranchManager)]
        public async Task<IActionResult> GetBranch(int id)
        {
            var branch = await _context.RestaurantAddresses
                .Include(b => b.Restaurant)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (branch == null)
                return NotFound(new { message = "Branch not found" });

            return Ok(branch);
        }

        // MERRE TE GJITHA BRANCHET E NJE RESTORANTI
        [HttpGet("by-restaurant/{restaurantId}")]
        [Authorize(Roles = AppRoles.Merchant + "," + AppRoles.Admin + "," + AppRoles.BranchManager)]
        public async Task<IActionResult> GetBranchesByRestaurant(int restaurantId)
        {
            var branches = await _context.RestaurantAddresses
                .Where(b => b.RestaurantId == restaurantId)
                .ToListAsync();

            return Ok(branches);
        }

        private string GenerateUsername(string name)
        {
            var baseName = name.ToLower()
                .Replace(" ", ".")
                .Replace("-", "")
                .Replace(".", "");

            if (baseName.Length > 20)
                baseName = baseName.Substring(0, 20);

            var username = baseName;
            var counter = 1;

            while (_userManager.Users.Any(u => u.UserName == username))
            {
                username = $"{baseName}{counter}";
                counter++;
            }

            return username;
        }

        private string GenerateRandomPassword()
        {
            var random = new Random();
            var numbers = random.Next(1000, 9999);
            return $"Branch@{numbers}";
        }
    }

    // DTO për aplikim
    public class BranchApplicationDto
    {
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public decimal DeliveryFee { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsMain { get; set; }
        public bool CreateBranchManager { get; set; }
        public string? ManagerName { get; set; }
        public string? ManagerEmail { get; set; }
    }

    // DTO për krijim direkt (vetëm për Admin)
    public class CreateBranchDto
    {
        public int RestaurantId { get; set; }
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public decimal DeliveryFee { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsMain { get; set; }
        public bool IsActive { get; set; }
        public bool CreateBranchManager { get; set; }
        public string? ManagerName { get; set; }
        public string? ManagerEmail { get; set; }
    }
}