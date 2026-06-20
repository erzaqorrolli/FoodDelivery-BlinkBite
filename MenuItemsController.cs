using FoodDeliveryyy.Data;
using FoodDeliveryyy.Models.Entities;
using FoodDeliveryyy.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FoodDeliveryyy.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MenuItemsController : ControllerBase
{
    private readonly AppDbContext _context;

    public MenuItemsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<object>>> GetMenuItems([FromQuery] int? branchId = null, [FromQuery] int? restaurantId = null)
    {
        try
        {
            IQueryable<MenuItems> query = _context.MenuItems
                .Include(m => m.Category)
                .Include(m => m.BranchDetails);

            // 🔥 FILTRO SIPAS BRANCH-IT (për Branch Manager)
            if (branchId.HasValue)
            {
                var branch = await _context.RestaurantAddresses
                    .FirstOrDefaultAsync(b => b.Id == branchId.Value);

                if (branch != null)
                {
                    query = query.Where(m => m.Category != null && m.Category.RestaurantId == branch.RestaurantId);
                }
            }
            // 🔥 FILTRO SIPAS RESTORANTIT (për Merchant)
            else if (restaurantId.HasValue)
            {
                query = query.Where(m => m.Category != null && m.Category.RestaurantId == restaurantId.Value);
            }

            var items = await query.ToListAsync();

            var result = items.Select(item => {
                MenuItemBranch? branchCustom = null;
                if (branchId.HasValue && item.BranchDetails != null)
                {
                    branchCustom = item.BranchDetails
                        .FirstOrDefault(b => b.RestaurantAddressId == branchId.Value);
                }

                return new
                {
                    item.Id,
                    item.Emertimi,
                    item.Pershkrimi,
                    Cmimi = branchCustom?.Cmimi ?? item.Cmimi,
                    item.Foto,
                    Disponueshme = branchCustom?.Disponueshme ?? item.Disponueshme,
                    item.Alergjene,
                    item.Kalori,
                    Perberesit = branchCustom?.Perberesit ?? item.Perberesit,
                    RequestOptions = branchCustom?.RequestOptions ?? item.RequestOptions,
                    item.CategoryId,
                    CategoryName = item.Category?.Emertimi,
                    item.RestaurantAddressId,
                    HasBranchCustomization = branchCustom != null
                };
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while fetching menu items.", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<MenuItems>> GetMenuItem(int id)
    {
        var menuItem = await _context.MenuItems.FindAsync(id);
        if (menuItem == null)
            return NotFound();
        return menuItem;
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Merchant + "," + AppRoles.Admin)]
    public async Task<ActionResult<MenuItems>> CreateMenuItem(MenuItems menuItem)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        var category = await _context.MenuCategories.FirstOrDefaultAsync(c => c.Id == menuItem.CategoryId);
        if (category == null)
            return BadRequest("Invalid categoryId.");

        var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == category.RestaurantId);
        if (restaurant == null)
            return BadRequest("Restaurant not found for this category.");

        if (role == AppRoles.Merchant && restaurant.UserId != userId)
            return Forbid();

        if (menuItem.RestaurantAddressId.HasValue)
        {
            var address = await _context.RestaurantAddresses.FirstOrDefaultAsync(a => a.Id == menuItem.RestaurantAddressId.Value);
            if (address == null || address.RestaurantId != category.RestaurantId)
                return BadRequest("Invalid restaurantAddressId.");
        }

        menuItem.Category = null;
        menuItem.RestaurantAddress = null;
        _context.MenuItems.Add(menuItem);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMenuItem), new { id = menuItem.Id }, menuItem);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = AppRoles.Merchant + "," + AppRoles.Admin)]
    public async Task<IActionResult> UpdateMenuItem(int id, MenuItems menuItem)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        if (id != menuItem.Id)
            return BadRequest();

        var category = await _context.MenuCategories.FirstOrDefaultAsync(c => c.Id == menuItem.CategoryId);
        if (category == null)
            return BadRequest("Invalid categoryId.");

        var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == category.RestaurantId);
        if (restaurant == null)
            return BadRequest("Restaurant not found for this category.");

        if (role == AppRoles.Merchant && restaurant.UserId != userId)
            return Forbid();

        if (menuItem.RestaurantAddressId.HasValue)
        {
            var address = await _context.RestaurantAddresses.FirstOrDefaultAsync(a => a.Id == menuItem.RestaurantAddressId.Value);
            if (address == null || address.RestaurantId != category.RestaurantId)
                return BadRequest("Invalid restaurantAddressId.");
        }

        var existing = await _context.MenuItems.FindAsync(id);
        if (existing == null)
            return NotFound();

        existing.Emertimi = menuItem.Emertimi;
        existing.Pershkrimi = menuItem.Pershkrimi;
        existing.Cmimi = menuItem.Cmimi;
        existing.Foto = menuItem.Foto;
        existing.Disponueshme = menuItem.Disponueshme;
        existing.Alergjene = menuItem.Alergjene;
        existing.Kalori = menuItem.Kalori;
        existing.Perberesit = menuItem.Perberesit;
        existing.RequestOptions = menuItem.RequestOptions;
        existing.CategoryId = menuItem.CategoryId;
        existing.RestaurantAddressId = menuItem.RestaurantAddressId;

        await _context.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = AppRoles.Merchant + "," + AppRoles.Admin)]
    public async Task<IActionResult> DeleteMenuItem(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        var menuItem = await _context.MenuItems.FindAsync(id);
        if (menuItem == null)
            return NotFound();

        if (role == AppRoles.Merchant)
        {
            var category = await _context.MenuCategories.FirstOrDefaultAsync(c => c.Id == menuItem.CategoryId);
            if (category == null)
                return BadRequest("Invalid categoryId.");

            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == category.RestaurantId);
            if (restaurant == null || restaurant.UserId != userId)
                return Forbid();
        }

        _context.MenuItems.Remove(menuItem);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Menu item deleted successfully" });
    }
}