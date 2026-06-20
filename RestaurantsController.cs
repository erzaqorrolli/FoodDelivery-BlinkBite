using FoodDeliveryyy.Data;
using FoodDeliveryyy.Models.Entities;
using FoodDeliveryyy.Models.Enums;
using FoodDeliveryyy.Models.Identity;
using FoodDeliveryyy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;

namespace FoodDeliveryyy.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RestaurantsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;

    public RestaurantsController(AppDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    // 🔥 PUBLIC - Kushdo mund të shohë listën e restoranteve (pa login)
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Restaurant>>> GetRestaurant([FromQuery] string? search)
    {
        var query = _context.Restaurants.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(r =>
                r.Emertimi.ToLower().Contains(search) ||
                (r.Kategori != null && r.Kategori.ToLower().Contains(search))
            );
        }

        return await query.ToListAsync();
    }

    // 🔥 PUBLIC - Kushdo mund të shohë detajet e një restoranti (pa login)
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<Restaurant>> GetRestaurant(int id)
    {
        var restaurant = await _context.Restaurants.FindAsync(id);
        if (restaurant == null) return NotFound();
        return restaurant;
    }

    // 🔥 VETËM PËR PËRDORUES TË LOGUAR - Shikon adresat e restorantit
    [HttpGet("{id:int}/addresses")]
    [Authorize]
    public async Task<ActionResult> GetRestaurantAddresses(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        if (role == AppRoles.Merchant)
        {
            var merchantRestaurant = await _context.Restaurants.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
            if (merchantRestaurant == null)
            {
                return Forbid();
            }
        }

        if (role == AppRoles.BranchManager)
        {
            var hasBranchAccess = await _context.RestaurantAddresses
                .AsNoTracking()
                .AnyAsync(a => a.RestaurantId == id && a.MerchantUserId == userId);

            if (!hasBranchAccess)
            {
                return Forbid();
            }
        }

        var restaurant = await _context.Restaurants
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (restaurant == null)
        {
            return NotFound("Restaurant not found.");
        }

        var addressQuery = _context.RestaurantAddresses
            .AsNoTracking()
            .Where(a => a.RestaurantId == id);

        if (role == AppRoles.BranchManager)
        {
            addressQuery = addressQuery.Where(a => a.MerchantUserId == userId);
        }

        var addresses = await addressQuery
            .OrderByDescending(a => a.IsMain)
            .ThenByDescending(a => a.IsActive)
            .ThenBy(a => a.Qyteti)
            .ThenBy(a => a.Adresa)
            .Select(a => new
            {
                id = a.Id,
                restaurantId = a.RestaurantId,
                merchantUserId = a.MerchantUserId,
                adresa = a.Adresa,
                qyteti = a.Qyteti,
                zona = a.Zona,
                isMain = a.IsMain,
                isActive = a.IsActive,
                latitude = a.Latitude,
                longitude = a.Longitude,
                tarifaDorezimit = a.TarifaDorezimit
            })
            .ToListAsync();

        return Ok(addresses);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Merchant + "," + AppRoles.Admin)]
    public async Task<ActionResult<Restaurant>> CreateRestaurant([FromBody] JsonElement restaurantData)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        if (!restaurantData.TryGetProperty("emertimi", out var emertimiElement) || string.IsNullOrWhiteSpace(emertimiElement.GetString()))
        {
            return BadRequest(new { message = "Restaurant name is required" });
        }

        var restaurant = new Restaurant
        {
            Emertimi = emertimiElement.GetString(),
            Pershkrimi = restaurantData.TryGetProperty("pershkrimi", out var pershkrimi) ? pershkrimi.GetString() : null,
            Telefoni = restaurantData.TryGetProperty("telefoni", out var telefoni) ? telefoni.GetString() : null,
            Email = restaurantData.TryGetProperty("email", out var email) ? email.GetString() : null,
            Kategori = restaurantData.TryGetProperty("kategori", out var kategori) ? kategori.GetString() : null,
            Statusi = RestaurantStatus.Active,
            Rating = 0,
            UserId = userId
        };

        // Shto CategoryId nëse është dërguar
        if (restaurantData.TryGetProperty("categoryId", out var categoryId) && categoryId.ValueKind != JsonValueKind.Null)
        {
            restaurant.CategoryId = categoryId.GetInt32();
        }

        _context.Restaurants.Add(restaurant);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRestaurant), new { id = restaurant.Id }, restaurant);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = AppRoles.Merchant + "," + AppRoles.Admin)]
    public async Task<ActionResult> UpdateRestaurant(int id, [FromBody] JsonElement updateData)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        var existing = await _context.Restaurants.FindAsync(id);
        if (existing == null)
            return NotFound(new { message = "Restaurant not found" });

        if (role == AppRoles.Merchant && existing.UserId != userId)
            return Forbid();

        // Update emertimi
        if (updateData.TryGetProperty("emertimi", out var emertimi))
            existing.Emertimi = emertimi.GetString();

        // Update pershkrimi
        if (updateData.TryGetProperty("pershkrimi", out var pershkrimi))
            existing.Pershkrimi = pershkrimi.GetString();

        // Update telefoni
        if (updateData.TryGetProperty("telefoni", out var telefoni))
            existing.Telefoni = telefoni.GetString();

        // Update email
        if (updateData.TryGetProperty("email", out var email))
            existing.Email = email.GetString();

        // Update kategori
        if (updateData.TryGetProperty("kategori", out var kategori))
            existing.Kategori = kategori.GetString();

        // Update statusi
        if (updateData.TryGetProperty("statusi", out var statusi))
        {
            var statusString = statusi.GetString();
            existing.Statusi = statusString switch
            {
                "Active" => RestaurantStatus.Active,
                "Pending" => RestaurantStatus.Pending,
                "Inactive" => RestaurantStatus.Inactive,
                _ => existing.Statusi
            };
        }

        // Update categoryId
        if (updateData.TryGetProperty("categoryId", out var categoryId) && categoryId.ValueKind != JsonValueKind.Null)
        {
            existing.CategoryId = categoryId.GetInt32();
        }

        await _context.SaveChangesAsync();
        return Ok(existing);
    }

    // 🔥 VETËM PËR MERCHANT DHE ADMIN - Fshirje restoranti
    [HttpDelete("{id}")]
    [Authorize(Roles = AppRoles.Merchant + "," + AppRoles.Admin)]
    public async Task<ActionResult> DeleteRestaurant(int id)
    {
        var restaurant = await _context.Restaurants.FindAsync(id);
        if (restaurant == null)
            return NotFound(new { message = "Restaurant not found" });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        if (role == AppRoles.Merchant && restaurant.UserId != userId)
        {
            return Forbid();
        }

        _context.Restaurants.Remove(restaurant);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Restaurant deleted successfully" });
    }

    // 🔥 PUBLIC - Kategoritë për të gjithë
    [HttpGet("kategori")]
    [AllowAnonymous]
    public async Task<ActionResult> GetCategories()
    {
        var categories = await _context.Categories
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                id = c.Id,
                name = c.Name,
                emertimi = c.Name,
                imageUrl = c.ImageUrl
            })
            .ToListAsync();

        return Ok(categories);
    }

    // 🔥 PUBLIC - Restorantet sipas kategorisë
    [HttpGet("bykategori/{kategori}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Restaurant>>> GetRestaurantsByCategory(string kategori)
    {
        var restaurants = await _context.Restaurants
            .Where(r => r.Kategori == kategori)
            .ToListAsync();

        return Ok(restaurants);
    }

    // 🔥 PUBLIC - Restorantet afër (për Customer)
    [HttpGet("nearby")]
    [AllowAnonymous]
    public async Task<ActionResult> GetNearbyRestaurants([FromQuery] double latitude, [FromQuery] double longitude, [FromQuery] int take = 20)
    {
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
        {
            return BadRequest(new { message = "Invalid coordinates." });
        }

        if (take <= 0) take = 20;
        if (take > 100) take = 100;

        var restaurants = await _context.Restaurants
            .Include(r => r.Adresat)
            .ToListAsync();

        var nearby = restaurants
            .Select(r =>
            {
                var activeAddresses = r.Adresat
                    .Where(a => a.IsActive && a.Latitude.HasValue && a.Longitude.HasValue)
                    .ToList();

                if (!activeAddresses.Any()) return null;

                var closestAddress = activeAddresses
                    .OrderBy(a => DistanceKm(latitude, longitude, a.Latitude!.Value, a.Longitude!.Value))
                    .First();

                var distanceKm = DistanceKm(latitude, longitude, closestAddress.Latitude!.Value, closestAddress.Longitude!.Value);

                return new
                {
                    id = r.Id,
                    name = r.Emertimi,
                    image = r.Logo,
                    category = r.Kategori,
                    distanceKm = Math.Round(distanceKm, 2),
                    nearestAddress = closestAddress.Adresa,
                    city = closestAddress.Qyteti,
                    latitude = closestAddress.Latitude,
                    longitude = closestAddress.Longitude
                };
            })
            .Where(x => x != null)
            .OrderBy(x => x!.distanceKm)
            .Take(take)
            .ToList();

        return Ok(nearby);
    }

    private static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Pow(Math.Sin(dLon / 2), 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}