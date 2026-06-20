using FoodDeliveryyy.Data;
using FoodDeliveryyy.Models.Entities;
using FoodDeliveryyy.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;


[Route("api/[controller]")]
[ApiController]
public class MenuCategoriesController : ControllerBase
{
    private readonly AppDbContext _context;

    public MenuCategoriesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MenuCategory>>> GetMenuCategories()
    {
        return await _context.MenuCategories.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MenuCategory>> GetMenuCategory(int id)
    {
        var category = await _context.MenuCategories.FindAsync(id);
        if (category == null) return NotFound();
        return category;
    }

    [HttpGet("by-restaurant/{restaurantId}")]
    public async Task<ActionResult<IEnumerable<MenuCategory>>> GetByRestaurant(int restaurantId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        if (role == AppRoles.Merchant)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == restaurantId && r.UserId == userId);
            if (restaurant == null)
            {
                return Forbid();
            }
        }

        if (role == AppRoles.BranchManager)
        {
            var hasBranchAccess = await _context.RestaurantAddresses
                .AnyAsync(a => a.RestaurantId == restaurantId && a.MerchantUserId == userId);
            if (!hasBranchAccess)
            {
                return Forbid();
            }
        }

        var categories = await _context.MenuCategories
            .Where(c => c.RestaurantId == restaurantId)
            .OrderBy(c => c.Renditja)
            .ToListAsync();
        return Ok(categories);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Merchant + "," + AppRoles.BranchManager + "," + AppRoles.Admin)]
    public async Task<ActionResult<MenuCategory>> CreateMenuCategory(MenuCategory category)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == category.RestaurantId);
        if (restaurant == null)
        {
            return BadRequest("Restaurant not found.");
        }

        if (role == AppRoles.Merchant && restaurant.UserId != userId)
        {
            return Forbid();
        }

        if (role == AppRoles.BranchManager)
        {
            var hasBranchAccess = await _context.RestaurantAddresses
                .AnyAsync(a => a.RestaurantId == category.RestaurantId && a.MerchantUserId == userId);
            if (!hasBranchAccess)
            {
                return Forbid();
            }
        }

        _context.MenuCategories.Add(category);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetMenuCategory), new { id = category.Id }, category);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = AppRoles.Merchant + "," + AppRoles.BranchManager + "," + AppRoles.Admin)]
    public async Task<IActionResult> UpdateMenuCategory(int id, MenuCategory category)
    {
        if (id != category.Id) return BadRequest();

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == category.RestaurantId);
        if (restaurant == null)
        {
            return BadRequest("Restaurant not found.");
        }

        if (role == AppRoles.Merchant && restaurant.UserId != userId)
        {
            return Forbid();
        }

        if (role == AppRoles.BranchManager)
        {
            var hasBranchAccess = await _context.RestaurantAddresses
                .AnyAsync(a => a.RestaurantId == category.RestaurantId && a.MerchantUserId == userId);
            if (!hasBranchAccess)
            {
                return Forbid();
            }
        }

        _context.Entry(category).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = AppRoles.Merchant + "," + AppRoles.BranchManager + "," + AppRoles.Admin)]
    public async Task<IActionResult> DeleteMenuCategory(int id)
    {
        var category = await _context.MenuCategories.FindAsync(id);
        if (category == null) return NotFound();

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        if (role == AppRoles.Merchant)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == category.RestaurantId);
            if (restaurant == null || restaurant.UserId != userId)
            {
                return Forbid();
            }
        }

        if (role == AppRoles.BranchManager)
        {
            var hasBranchAccess = await _context.RestaurantAddresses
                .AnyAsync(a => a.RestaurantId == category.RestaurantId && a.MerchantUserId == userId);
            if (!hasBranchAccess)
            {
                return Forbid();
            }
        }

        _context.MenuCategories.Remove(category);
        await _context.SaveChangesAsync();
        return NoContent();
    }


    [HttpPatch("{id}")]
    [Authorize(Roles = AppRoles.Merchant + "," + AppRoles.BranchManager + "," + AppRoles.Admin)]
    public async Task<IActionResult> PatchMenuCategory(int id, [FromBody] JsonElement patch)
    {
        var category = await _context.MenuCategories.FindAsync(id);
        if (category == null) return NotFound();

        if (patch.TryGetProperty("emertimi", out var emertimi))
            category.Emertimi = emertimi.GetString();

        if (patch.TryGetProperty("pershkrimi", out var pershkrimi))
            category.Pershkrimi = pershkrimi.GetString();

        if (patch.TryGetProperty("renditja", out var renditja))
            category.Renditja = renditja.GetInt32();

        await _context.SaveChangesAsync();
        return Ok(category);
    }
}