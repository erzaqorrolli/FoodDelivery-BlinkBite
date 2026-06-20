using FoodDeliveryyy.Data;
using FoodDeliveryyy.Models.Entities;
using FoodDeliveryyy.Models.Enums;
using FoodDeliveryyy.Models.Identity;
using FoodDeliveryyy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodDeliveryyy.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin/applications")]
public class AdminApplicationController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IEmailService _emailService;

    public AdminApplicationController(
        AppDbContext context,
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IEmailService emailService)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _emailService = emailService;
    }

    // ==================== TESTS ====================
    [HttpGet]
    public IActionResult Test()
    {
        return Ok(new { message = "Admin controller is working!" });
    }

    // ==================== RESTAURANT APPLICATIONS ====================
    [HttpGet("restaurants")]
    public async Task<IActionResult> GetRestaurantApplications([FromQuery] string? status = null)
    {
        var query = _context.RestaurantApplications.AsQueryable();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(x => x.Status == status);

        var applications = await query.OrderByDescending(x => x.AppliedAt).ToListAsync();
        return Ok(applications);
    }

    [HttpPost("restaurant/{id}/approve")]
    public async Task<IActionResult> ApproveRestaurant(int id, [FromBody] ApproveDto? dto)
    {
        var application = await _context.RestaurantApplications.FindAsync(id);
        if (application == null)
            return NotFound(new { message = "Application not found" });

        if (application.Status != "Pending")
            return BadRequest(new { message = "This application has already been reviewed" });

        var username = GenerateUsernameFromRestaurant(application.RestaurantName);
        var user = new User
        {
            UserName = username,
            Email = application.Email,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var password = GenerateRandomPassword();
        var createResult = await _userManager.CreateAsync(user, password);

        if (!createResult.Succeeded)
        {
            return BadRequest(new { message = "Cannot create account", errors = createResult.Errors });
        }

        await _userManager.AddToRoleAsync(user, "Merchant");

        var restaurant = new Restaurant
        {
            Emertimi = application.RestaurantName,
            Pershkrimi = application.RestaurantDescription ?? "",
            Telefoni = application.Phone,
            Email = application.Email,
            UserId = user.Id,
            Statusi = RestaurantStatus.Active,
            Rating = 0,
            Kategori = application.Category ?? "Fast Food"
        };

        _context.Restaurants.Add(restaurant);
        await _context.SaveChangesAsync();

        var address = new RestaurantAddress
        {
            RestaurantId = restaurant.Id,
            Adresa = application.Address,
            Qyteti = application.City,
            IsMain = true,
            IsActive = true
        };

        _context.RestaurantAddresses.Add(address);
        await _context.SaveChangesAsync();

        application.Status = "Approved";
        application.ReviewedAt = DateTime.UtcNow;
        application.AdminNotes = dto?.Notes;
        await _context.SaveChangesAsync();

        try
        {
            await _emailService.SendMerchantCredentialsEmailAsync(
                application.Email,
                restaurant.Emertimi,
                username,
                password
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Email failed for Merchant: {ex.Message}");
        }

        return Ok(new
        {
            message = "Restaurant approved. Credentials sent via email.",
            email = application.Email,
            username = username,
            restaurantId = restaurant.Id
        });
    }

    [HttpPost("restaurant/{id}/reject")]
    public async Task<IActionResult> RejectRestaurant(int id, [FromBody] RejectDto dto)
    {
        var application = await _context.RestaurantApplications.FindAsync(id);
        if (application == null)
            return NotFound(new { message = "Application not found" });

        application.Status = "Rejected";
        application.ReviewedAt = DateTime.UtcNow;
        application.AdminNotes = dto.Reason;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Application rejected" });
    }

    // 🔥 DELETE PËR RESTAURANT APPLICATION
    [HttpDelete("restaurant-application/{id}")]
    public async Task<IActionResult> DeleteRestaurantApplication(int id)
    {
        var application = await _context.RestaurantApplications.FindAsync(id);
        if (application == null)
            return NotFound(new { message = "Application not found" });

        _context.RestaurantApplications.Remove(application);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Restaurant application deleted successfully" });
    }

    // ==================== COURIER APPLICATIONS ====================
    [HttpGet("couriers")]
    public async Task<IActionResult> GetCourierApplications([FromQuery] string? status = null)
    {
        var query = _context.CourierApplications.AsQueryable();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(x => x.Status == status);

        var applications = await query.OrderByDescending(x => x.AppliedAt).ToListAsync();
        return Ok(applications);
    }

    [HttpPost("courier/{id}/approve")]
    public async Task<IActionResult> ApproveCourier(int id, [FromBody] ApproveDto? dto)
    {
        var application = await _context.CourierApplications.FindAsync(id);
        if (application == null)
            return NotFound(new { message = "Application not found" });

        if (application.Status != "Pending")
            return BadRequest(new { message = "This application has already been reviewed" });

        var username = GenerateUsernameFromName(application.FullName);
        var user = new User
        {
            UserName = username,
            Email = application.Email,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var password = GenerateRandomPassword();
        var createResult = await _userManager.CreateAsync(user, password);

        if (!createResult.Succeeded)
        {
            return BadRequest(new { message = "Cannot create account", errors = createResult.Errors });
        }

        await _userManager.AddToRoleAsync(user, "Courier");

        var driver = new DeliveryDrivers
        {
            UserId = user.Id,
            Automjeti = application.VehicleType,
            Targa = application.LicensePlate ?? "",
            Zona = application.WorkingArea,
            Statusi = DriverStatus.Available,
            Vlersimi = 0
        };

        _context.DeliveryDrivers.Add(driver);
        await _context.SaveChangesAsync();

        application.Status = "Approved";
        application.ReviewedAt = DateTime.UtcNow;
        application.AdminNotes = dto?.Notes;
        await _context.SaveChangesAsync();

        try
        {
            await _emailService.SendCourierCredentialsEmailAsync(
                application.Email,
                application.FullName,
                username,
                password
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Email failed for Courier: {ex.Message}");
        }

        return Ok(new
        {
            message = "Courier approved. Credentials sent via email.",
            email = application.Email,
            username = username
        });
    }

    [HttpPost("courier/{id}/reject")]
    public async Task<IActionResult> RejectCourier(int id, [FromBody] RejectDto dto)
    {
        var application = await _context.CourierApplications.FindAsync(id);
        if (application == null)
            return NotFound(new { message = "Application not found" });

        application.Status = "Rejected";
        application.ReviewedAt = DateTime.UtcNow;
        application.AdminNotes = dto.Reason;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Application rejected" });
    }

    // 🔥 DELETE PËR COURIER APPLICATION
    [HttpDelete("courier-application/{id}")]
    public async Task<IActionResult> DeleteCourierApplication(int id)
    {
        var application = await _context.CourierApplications.FindAsync(id);
        if (application == null)
            return NotFound(new { message = "Application not found" });

        _context.CourierApplications.Remove(application);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Courier application deleted successfully" });
    }

    // ==================== BRANCH APPLICATIONS ====================
    [HttpGet("branches")]
    public async Task<IActionResult> GetBranchApplications([FromQuery] string? status = null)
    {
        var query = _context.BranchApplications
            .Include(b => b.Restaurant)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(x => x.Status == status);

        var applications = await query
            .OrderByDescending(x => x.AppliedAt)
            .ToListAsync();

        return Ok(applications);
    }

    [HttpPost("branch/{id}/approve")]
    public async Task<IActionResult> ApproveBranchApplication(int id, [FromBody] ApproveDto? dto)
    {
        var application = await _context.BranchApplications
            .Include(b => b.Restaurant)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (application == null)
            return NotFound(new { message = "Application not found" });

        if (application.Status != "Pending")
            return BadRequest(new { message = "This application has already been reviewed" });

        var branch = new RestaurantAddress
        {
            RestaurantId = application.RestaurantId,
            Adresa = application.Address,
            Qyteti = application.City,
            Zona = application.Zone,
            TarifaDorezimit = application.DeliveryFee,
            Latitude = application.Latitude,
            Longitude = application.Longitude,
            IsMain = application.IsMain,
            IsActive = true,
            MerchantUserId = null
        };

        _context.RestaurantAddresses.Add(branch);
        await _context.SaveChangesAsync();

        if (application.CreateBranchManager && !string.IsNullOrEmpty(application.ManagerEmail))
        {
            var username = GenerateUsernameFromName(application.ManagerName ?? application.ManagerEmail.Split('@')[0]);
            var password = GenerateRandomPassword();

            var branchManager = new User
            {
                UserName = username,
                Email = application.ManagerEmail,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(branchManager, password);

            if (createResult.Succeeded)
            {
                await _userManager.AddToRoleAsync(branchManager, "BranchManager");
                branch.MerchantUserId = branchManager.Id;
                await _context.SaveChangesAsync();

                try
                {
                    await _emailService.SendBranchManagerCredentialsEmailAsync(
                        branchManager.Email,
                        branch.Adresa,
                        application.Restaurant!.Emertimi,
                        branchManager.UserName,
                        password
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Email failed for Branch Manager: {ex.Message}");
                }
            }
        }

        application.Status = "Approved";
        application.ProcessedAt = DateTime.UtcNow;
        application.AdminNotes = dto?.Notes;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Branch application approved successfully",
            branchId = branch.Id
        });
    }

    [HttpPost("branch/{id}/reject")]
    public async Task<IActionResult> RejectBranchApplication(int id, [FromBody] RejectDto dto)
    {
        var application = await _context.BranchApplications.FindAsync(id);
        if (application == null)
            return NotFound(new { message = "Application not found" });

        if (application.Status != "Pending")
            return BadRequest(new { message = "This application has already been reviewed" });

        application.Status = "Rejected";
        application.ProcessedAt = DateTime.UtcNow;
        application.AdminNotes = dto.Reason;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Branch application rejected" });
    }

    // 🔥 DELETE PËR BRANCH APPLICATION
    [HttpDelete("branch-application/{id}")]
    public async Task<IActionResult> DeleteBranchApplication(int id)
    {
        var application = await _context.BranchApplications.FindAsync(id);
        if (application == null)
            return NotFound(new { message = "Application not found" });

        _context.BranchApplications.Remove(application);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Branch application deleted successfully" });
    }

    // ==================== DELETE RESTAURANT (i plotë) ====================
    [HttpDelete("restaurant/{id}")]
    public async Task<IActionResult> DeleteRestaurant(int id)
    {
        var application = await _context.RestaurantApplications.FindAsync(id);
        if (application == null)
            return NotFound(new { message = "Application not found" });

        var restaurant = await _context.Restaurants
            .FirstOrDefaultAsync(r => r.Email == application.Email || r.Emertimi == application.RestaurantName);

        if (restaurant == null)
            return NotFound(new { message = "Restaurant not found" });

        var orders = _context.Orders.Where(o => o.RestaurantId == restaurant.Id).ToList();
        _context.Orders.RemoveRange(orders);

        var reviews = _context.Reviews.Where(r => r.RestaurantId == restaurant.Id).ToList();
        _context.Reviews.RemoveRange(reviews);

        var orderIds = orders.Select(o => o.Id).ToList();
        var deliveries = _context.Deliveries.Where(d => orderIds.Contains(d.OrderId)).ToList();
        _context.Deliveries.RemoveRange(deliveries);

        var addresses = _context.RestaurantAddresses.Where(a => a.RestaurantId == restaurant.Id).ToList();
        _context.RestaurantAddresses.RemoveRange(addresses);

        var menuCategories = _context.MenuCategories.Where(c => c.RestaurantId == restaurant.Id).ToList();
        foreach (var category in menuCategories)
        {
            var menuItems = _context.MenuItems.Where(m => m.CategoryId == category.Id).ToList();
            _context.MenuItems.RemoveRange(menuItems);
        }
        _context.MenuCategories.RemoveRange(menuCategories);

        _context.Restaurants.Remove(restaurant);
        _context.RestaurantApplications.Remove(application);

        await _context.SaveChangesAsync();

        return Ok(new { message = "Restaurant deleted successfully" });
    }

    // ==================== HELPER METHODS ====================
    private string GenerateUsernameFromRestaurant(string restaurantName)
    {
        var baseName = restaurantName.ToLower()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("&", "")
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

    private string GenerateUsernameFromName(string fullName)
    {
        var baseName = fullName.ToLower()
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
        return $"BlinkBite@{numbers}";
    }
}

// ==================== DTOs ====================
public class ApproveDto
{
    public string? Notes { get; set; }
}

public class RejectDto
{
    public string Reason { get; set; } = string.Empty;
}