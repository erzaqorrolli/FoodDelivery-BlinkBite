using FoodDeliveryyy.Data;
using FoodDeliveryyy.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace FoodDeliveryyy.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RestaurantAddressesController : ControllerBase
{
    private readonly AppDbContext _context;

    public RestaurantAddressesController(AppDbContext context)
    {
        _context = context;
    }

    // GET api/restaurantaddresses
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<RestaurantAddress>>> Get([FromQuery] int? restaurantId)
    {
        if (restaurantId.HasValue)
        {
            var list = await _context.RestaurantAddresses
                .Where(r => r.RestaurantId == restaurantId.Value)
                .ToListAsync();
            return Ok(list);
        }

        var all = await _context.RestaurantAddresses.ToListAsync();
        return Ok(all);
    }

    [HttpGet("by-restaurant/{restaurantId}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<RestaurantAddress>>> GetByRestaurant(int restaurantId)
    {
        var list = await _context.RestaurantAddresses
            .Where(r => r.RestaurantId == restaurantId)
            .ToListAsync();
        return Ok(list);
    }

    // legacy route variants that the frontend may call
    [HttpGet("byrestaurant/{restaurantId}")]
    [AllowAnonymous]
    public Task<ActionResult<IEnumerable<RestaurantAddress>>> GetByRestaurantAlias(int restaurantId)
        => GetByRestaurant(restaurantId);

    [HttpGet("restaurant/{restaurantId}")]
    [AllowAnonymous]
    public Task<ActionResult<IEnumerable<RestaurantAddress>>> GetByRestaurantAlias2(int restaurantId)
        => GetByRestaurant(restaurantId);

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<RestaurantAddress>> GetById(int id)
    {
        var addr = await _context.RestaurantAddresses.FindAsync(id);
        if (addr == null) return NotFound();
        return Ok(addr);
    }
}
