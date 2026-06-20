using FoodDeliveryyy.Data;
using FoodDeliveryyy.Models.Entities;
using FoodDeliveryyy.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodDeliveryyy.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CategoryController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoryController(AppDbContext context)
    {
        _context = context;
    }

    // 🔥 GET - Merr të gjitha kategoritë (publik - kushdo mund të shohë)
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllCategories()
    {
        var categories = await _context.Categories
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                ImageUrl = c.ImageUrl ?? "",
            })
            .ToListAsync();
        return Ok(categories);
    }

    // 🔥 GET - Merr një kategori sipas ID (publik)
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryById(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
            return NotFound(new { message = "Category not found" });

        return Ok(new
        {
            category.Id,
            category.Name,
            ImageUrl = category.ImageUrl ?? "",
        });
    }

    // 🔥 POST - Krijo kategori të re (vetëm Admin)
    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> CreateCategory([FromBody] CategoryDto dto)
    {
        // Validimi
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "Category name is required." });

        // Kontrollo nëse ekziston kategori me të njëjtin emër
        var existingCategory = await _context.Categories
            .FirstOrDefaultAsync(c => c.Name.ToLower() == dto.Name.Trim().ToLower());

        if (existingCategory != null)
            return BadRequest(new { message = "A category with this name already exists." });

        var category = new Category
        {
            Name = dto.Name.Trim(),
            ImageUrl = dto.ImageUrl ?? ""
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            category.Id,
            category.Name,
            category.ImageUrl,
            message = "Category created successfully"
        });
    }

    // 🔥 PUT - Përditëso kategori (vetëm Admin)
    [HttpPut("{id}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryDto dto)
    {
        // Validimi
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "Category name is required." });

        var category = await _context.Categories.FindAsync(id);
        if (category == null)
            return NotFound(new { message = "Category not found" });

        // Kontrollo nëse ekziston kategori tjetër me të njëjtin emër
        var existingCategory = await _context.Categories
            .FirstOrDefaultAsync(c => c.Name.ToLower() == dto.Name.Trim().ToLower() && c.Id != id);

        if (existingCategory != null)
            return BadRequest(new { message = "A category with this name already exists." });

        category.Name = dto.Name.Trim();
        category.ImageUrl = dto.ImageUrl ?? "";

        await _context.SaveChangesAsync();

        return Ok(new
        {
            category.Id,
            category.Name,
            category.ImageUrl,
            message = "Category updated successfully"
        });
    }

    // 🔥 DELETE - Fshij kategori (vetëm Admin) - 🔥 TANI FSHIN EDHE MENU ITEMS
    [HttpDelete("{id}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
            return NotFound(new { message = "Category not found" });

        // 🔥 FSHIJ TË GJITHA MENU ITEMS TË LIDHURA ME KËTË KATEGORI
        var menuItems = await _context.MenuItems.Where(m => m.CategoryId == id).ToListAsync();
        int deletedItemsCount = menuItems.Count;

        if (menuItems.Any())
        {
            _context.MenuItems.RemoveRange(menuItems);
        }

        // Kontrollo nëse ka restorante të lidhura
        var hasRestaurants = await _context.Restaurants.AnyAsync(r => r.CategoryId == id);
        if (hasRestaurants)
        {
            return BadRequest(new { message = "Cannot delete category because it has associated restaurants. Please delete or reassign those restaurants first." });
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        string successMessage = $"Category '{category.Name}' deleted successfully.";
        if (deletedItemsCount > 0)
        {
            successMessage = $"Category '{category.Name}' and {deletedItemsCount} menu item(s) deleted successfully.";
        }

        return Ok(new { message = successMessage, deletedItemsCount = deletedItemsCount });
    }
}

// DTO për Category
public class CategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}