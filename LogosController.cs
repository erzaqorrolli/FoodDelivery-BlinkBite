using FoodDeliveryyy.Data;
using FoodDeliveryyy.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Hosting;

namespace FoodDeliveryyy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        public LogosController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }
        // GET: api/Logos
        [HttpPost("upload/{restaurantId}")]
        public async Task<ActionResult> UploadLogo(int restaurantId, IFormFile file)
        {
            var restaurant = await _context.Restaurants.FindAsync(restaurantId);
            if (restaurant == null)
                return NotFound(new { message = "Restaurant not found!" });

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded!" });
       
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if(!allowedExtensions.Contains(extension))
            return BadRequest(new { message="Invalid file type!"});

        if(file.Length > 5 * 1024 * 1024)
            return BadRequest(new { message="File size exceeds the limit of 5MB!"});

        if(!string.IsNullOrEmpty(restaurant.Logo))
        {
        var oldLogoPath = Path.Combine(_environment.WebRootPath, restaurant.Logo.TrimStart('/'));
        if(System.IO.File.Exists(oldLogoPath))
            System.IO.File.Delete(oldLogoPath);
        }
            var fileName = $"restaurant_{restaurantId}_{Guid.NewGuid()}{extension}";
        var uploadPath = Path.Combine(_environment.WebRootPath, "uploads","logos");

        if(!Directory.Exists(uploadPath))
        Directory.CreateDirectory(uploadPath);

        var filePath = Path.Combine(uploadPath, fileName);
    using(var stream =new FileStream(filePath, FileMode.Create)){
        await file.CopyToAsync(stream);
}

restaurant.Logo = $"/uploads/logos/{fileName}";
await _context.SaveChangesAsync();

return Ok(new
{
    message = "Logo uploaded successfully!",
    logoUrl = restaurant.Logo
});
        }

        [HttpDelete("delete/{restaurantId}")]
public async Task<IActionResult> DeleteLogo(int restaurantId)
{
    var restaurant = await _context.Restaurants.FindAsync(restaurantId);
    if (restaurant == null)
        return NotFound(new { message = "Restaurant not found!" });

    if (string.IsNullOrEmpty(restaurant.Logo))
        return BadRequest(new { message = "This restaurant does not have a logo!" });

    var logoPath = Path.Combine(_environment.WebRootPath, restaurant.Logo.TrimStart('/'));
    if (System.IO.File.Exists(logoPath))
        System.IO.File.Delete(logoPath);

    restaurant.Logo = string.Empty;
    await _context.SaveChangesAsync();

    return Ok(new { message = "Logo deleted successfully!" });
}
    }
}