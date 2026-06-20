using Microsoft.AspNetCore.Mvc;
using FoodDeliveryyy.Data;
using FoodDeliveryyy.Models.Entities;

namespace FoodDeliveryyy.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApplicationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ApplicationsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("restaurant")]
    public async Task<IActionResult> ApplyRestaurant([FromBody] RestaurantApplicationDto dto)
    {
        var application = new RestaurantApplication
        {
            RestaurantName = dto.RestaurantName,
            RestaurantDescription = dto.RestaurantDescription,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address,
            City = dto.City,
            Category = dto.Category,
            Status = "Pending",
            AppliedAt = DateTime.UtcNow
        };

        _context.RestaurantApplications.Add(application);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Aplikimi u dërgua me sukses!" });
    }

    [HttpPost("courier")]
    public async Task<IActionResult> ApplyCourier([FromBody] CourierApplicationDto dto)
    {
        var application = new CourierApplication
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Phone = dto.Phone,
            VehicleType = dto.VehicleType,
            LicensePlate = dto.LicensePlate,
            WorkingArea = dto.WorkingArea,
            Status = "Pending",
            AppliedAt = DateTime.UtcNow
        };

        _context.CourierApplications.Add(application);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Aplikimi u dërgua me sukses!" });
    }
}

public class RestaurantApplicationDto
{
    public string RestaurantName { get; set; } = string.Empty;
    public string RestaurantDescription { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class CourierApplicationDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public string WorkingArea { get; set; } = string.Empty;
}