using FoodDeliveryyy.Data;
using FoodDeliveryyy.Models.Entities;
using FoodDeliveryyy.Models.Enums;
using FoodDeliveryyy.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace FoodDeliveryyy.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ReviewsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReviewsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Reviews>>> GetReviews()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        IQueryable<Reviews> query = _context.Reviews
            .Include(r => r.User)
            .Include(r => r.Restaurant)
            .Include(r => r.Order)
            .OrderByDescending(r => r.DataKrijimit);

        if (role == AppRoles.Merchant)
        {
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.UserId == userId);
            if (restaurant != null)
                query = query.Where(r => r.RestaurantId == restaurant.Id);
            else
                return Ok(new List<Reviews>());
        }
        else if (role != AppRoles.Admin)
        {
            return Forbid();
        }

        return Ok(await query.ToListAsync());
    }


    [HttpGet("by-restaurant/{restaurantId}")]
    [AllowAnonymous] 
    public async Task<ActionResult<IEnumerable<Reviews>>> GetReviewsByRestaurant(int restaurantId)
    {
        var reviews = await _context.Reviews
            .Where(r => r.RestaurantId == restaurantId)
            .Include(r => r.User)
            .OrderByDescending(r => r.DataKrijimit)
            .ToListAsync();
        return Ok(reviews);
    }

    [HttpGet("by-user/{userId}")]
    public async Task<ActionResult<IEnumerable<Reviews>>> GetReviewsByUser(string userId)
    {
        var reviews = await _context.Reviews
            .Where(r => r.UserId == userId)
            .Include(r => r.Restaurant)
            .OrderByDescending(r => r.DataKrijimit)
            .ToListAsync();

        return Ok(reviews);
    }

    [HttpGet("by-order/{orderId}")]
    public async Task<ActionResult<Reviews>> GetReviewByOrder(int orderId)
    {
        var review = await _context.Reviews
            .FirstOrDefaultAsync(r => r.OrderId == orderId);

        if (review == null)
        {
            return NotFound();
        }

        return review;
    }

    [HttpGet("rating/{restaurantId}")]
    public async Task<ActionResult<object>> GetRestaurantRating(int restaurantId)
    {
        var reviews = await _context.Reviews
            .Where(r => r.RestaurantId == restaurantId)
            .ToListAsync();

        if (!reviews.Any())
        {
            return Ok(new
            {
                restaurantId = restaurantId,
                averageRating = 0,
                totalReviews = 0
            });
        }

        var averageRating = reviews.Average(r => r.Vlersimi);
        var ratingDistribution = new
        {
            rating1 = reviews.Count(r => r.Vlersimi == 1),
            rating2 = reviews.Count(r => r.Vlersimi == 2),
            rating3 = reviews.Count(r => r.Vlersimi == 3),
            rating4 = reviews.Count(r => r.Vlersimi == 4),
            rating5 = reviews.Count(r => r.Vlersimi == 5)
        };

        return Ok(new
        {
            restaurantId = restaurantId,
            averageRating = Math.Round(averageRating, 1),
            totalReviews = reviews.Count,
            distribution = ratingDistribution
        });
    }

    [HttpPost]
    [Authorize(Roles =AppRoles.Customer + "," + AppRoles.Admin)]
    public async Task<ActionResult<Reviews>> CreateReview(Reviews review)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == review.OrderId);

        if (order == null)
        {
            return BadRequest("Order does not exist");
        }

        if (order.Statusi != OrderStatus.Delivered)
        {
            return BadRequest("You can review only purchased orders");
        }

        if (order.UserId != review.UserId)
        {
            return BadRequest("You can review only your purchases");
        }

        var existingReview = await _context.Reviews
            .FirstOrDefaultAsync(r => r.OrderId == review.OrderId);

        if (existingReview != null)
        {
            return BadRequest("This order is already reviewed");
        }

        if (order.RestaurantId != review.RestaurantId)
        {
            return BadRequest("Restaurant does not match the order. ");
        }

        review.DataKrijimit = DateTime.Now;

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        await UpdateRestaurantRating(review.RestaurantId);

        var createdReview = await _context.Reviews
            .Include(r => r.User)
            .Include(r => r.Restaurant)
            .FirstOrDefaultAsync(r => r.Id == review.Id);

        return CreatedAtAction(nameof(GetReviews), new { id = review.Id }, createdReview);
    }
    [HttpPut("{id}")]
    [Authorize(Roles = AppRoles.Customer + "," + AppRoles.Admin)]
    public async Task<IActionResult> UpdateReview(int id, [FromBody] JsonElement updateData)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        var existingReview = await _context.Reviews.FindAsync(id);
        if (existingReview == null) return NotFound();

        if (role == AppRoles.Customer && existingReview.UserId != userId)
            return Forbid("You cannot change someone else's review");

        if (updateData.TryGetProperty("vlersimi", out var ratingProp))
            existingReview.Vlersimi = ratingProp.GetDecimal();

        if (updateData.TryGetProperty("komenti", out var commentProp))
            existingReview.Komenti = commentProp.GetString();

        await _context.SaveChangesAsync();
        await UpdateRestaurantRating(existingReview.RestaurantId);

        return Ok(existingReview);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReview(int id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review == null)
        {
            return NotFound();
        }

        var restaurantId = review.RestaurantId;

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();

        await UpdateRestaurantRating(restaurantId);

        return NoContent();
    }

    private async Task UpdateRestaurantRating(int restaurantId)
    {
        var averageRating = await _context.Reviews
            .Where(r => r.RestaurantId == restaurantId)
            .AverageAsync(r => (double)r.Vlersimi);

        var restaurant = await _context.Restaurants.FindAsync(restaurantId);
        if (restaurant != null)
        {
            restaurant.Rating = (decimal)Math.Round(averageRating, 1);
            await _context.SaveChangesAsync();
        }
    }

    private bool ReviewExists(int id)
    {
        return _context.Reviews.Any(e => e.Id == id);
    }
}