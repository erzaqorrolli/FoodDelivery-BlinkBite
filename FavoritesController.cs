using FoodDeliveryyy.Data;
using FoodDeliveryyy.Models.Entities;   
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FoodDeliveryyy.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]//veq perdoruesit qe jane login munden me perdor favorite
public class FavoritesController : ControllerBase
{
    private readonly AppDbContext _context;

    public FavoritesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<FavoritesResponse>> GetFavorites()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }
        var favorites = await _context.UserFavorites
          .Include(f => f.Restaurant)
          .Include(f => f.MenuItems)
          .Where(f => f.UserId == userId)
          .ToListAsync();

        return Ok(new FavoritesResponse
        {
            RestaurantIds = favorites.Where(f => f.RestaurantId.HasValue)
            .Select(f => f.RestaurantId.Value)
            .ToList(),

            MenuItemIds = favorites.Where(f => f.MenuItemId.HasValue)
            .Select(f => f.MenuItemId.Value)
            .ToList()

        }
            );
    }

    [HttpPost("restaurant/{restaurantId}")]
    public async Task<IActionResult> AddRestaurantFavorite(int restaurantId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var restaurant = await _context.Restaurants.FindAsync(restaurantId);
        if (restaurant == null)
            return NotFound(new { message = "Restaurant not found" });

        var exists = await _context.UserFavorites
           .AnyAsync(f => f.UserId == userId && f.RestaurantId == restaurantId);

        if (exists)
            return BadRequest(new { message = "Restaurant is already in favorites" });

        var favorite = new UserFavorite
        {
            UserId = userId,
            RestaurantId = restaurantId,
            CreatedAt = DateTime.UtcNow
        };

        _context.UserFavorites.Add(favorite);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Restaurant added to favorites!" });
    }

    [HttpPost("menuitem/{menuItemId}")]
    public async Task<IActionResult> AddMenuItemFavorite(int menuItemId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var menuItem = await _context.MenuItems.FindAsync(menuItemId);
        if (menuItem == null)
            return NotFound(new { message = "Product not found" });

        var exists = await _context.UserFavorites
            .AnyAsync(f => f.UserId == userId && f.MenuItemId == menuItemId);

        if (exists)
            return BadRequest(new { message = "Product is already in favorites!" });

        var favorite = new UserFavorite
        {
            UserId = userId,
            MenuItemId = menuItemId,
            CreatedAt = DateTime.UtcNow
        }
        ;

        _context.UserFavorites.Add(favorite);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Product added to favorites!" });
    }
    [HttpDelete("restaurant/{restaurantId}")]
    public async Task<IActionResult> RemoveRestaurantFavorite(int restaurantId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var favorite = await _context.UserFavorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.RestaurantId == restaurantId);

        if (favorite == null)
            return NotFound(new { message = "Restaurant is not in favorites!" });

        _context.UserFavorites.Remove(favorite);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Restaurant removed from favorites!" });
    }

    [HttpDelete("menuitem/{menuItemId}")]
    public async Task<IActionResult> RemoveMenuItemFavorite(int menuItemId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var favorite = await _context.UserFavorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.MenuItemId == menuItemId);

        if (favorite == null)
            return NotFound(new { message = "Product is not in favorites!" });

        _context.UserFavorites.Remove(favorite);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Product removed from favorties!" });
    }

    [HttpGet("check/restaurant/{restaurantId}")]
    public async Task<ActionResult<bool>> IsRestaurantFavorite(int restaurantId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return false;

        var isFavorite = await _context.UserFavorites
            .AnyAsync(f => f.UserId == userId && f.RestaurantId == restaurantId);

        return Ok(isFavorite);
    }

    [HttpGet("check/menuitem/{menuItemId}")]
    public async Task<ActionResult<bool>> IsMenuItemFavorite(int menuItemId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return false;

        var isFavorite = await _context.UserFavorites
            .AnyAsync(f => f.UserId == userId && f.MenuItemId == menuItemId);

        return Ok(isFavorite);
    }
}

public class FavoritesResponse
{
    public List<int> RestaurantIds { get; set; } = new();
    public List<int> MenuItemIds { get; set; } = new();
}