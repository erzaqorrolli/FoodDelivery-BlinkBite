using FoodDeliveryyy.Data;
using FoodDeliveryyy.Models.Entities;
using FoodDeliveryyy.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodDeliveryyy.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DeliveriesController : ControllerBase
{
    private readonly AppDbContext _context;

    public DeliveriesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Deliveries>>> GetDeliveries()
    {
        return await _context.Deliveries
            .Include(d => d.Order)
            .Include(d => d.Driver)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Deliveries>> GetDelivery(int id)
    {
        var delivery = await _context.Deliveries
            .Include(d => d.Order)
            .Include(d => d.Driver)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (delivery == null)
        {
            return NotFound();
        }

        return delivery;
    }

    [HttpGet("by-order/{orderId}")]
    public async Task<ActionResult<Deliveries>> GetDeliveryByOrder(int orderId)
    {
        var delivery = await _context.Deliveries
            .FirstOrDefaultAsync(d => d.OrderId == orderId);

        if (delivery == null)
        {
            return NotFound();
        }

        return delivery;
    }

    [HttpGet("by-driver/{driverId}")]
    public async Task<ActionResult<IEnumerable<Deliveries>>> GetDeliveriesByDriver(int driverId)
    {
        var deliveries = await _context.Deliveries
            .Where(d => d.DriverId == driverId)
            .Include(d => d.Order)
            .ToListAsync();

        return Ok(deliveries);
    }
    [HttpPost]
    public async Task<ActionResult<Deliveries>> CreateDelivery(Deliveries delivery)
    {
        var order = await _context.Orders.FindAsync(delivery.OrderId);
        if (order == null)
        {
            return BadRequest("Porosia nuk ekziston");
        }

        var driver = await _context.DeliveryDrivers.FindAsync(delivery.DriverId);
        if (driver == null)
        {
            return BadRequest("Shoferi nuk ekziston");
        }

        // Set enum values directly instead of assigning strings
        delivery.Statusi = DeliveryStatus.Pending;
        delivery.DataMarrjes = null;
        delivery.DataDorezimit = null;

        _context.Deliveries.Add(delivery);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDelivery), new { id = delivery.Id }, delivery);
    }

    [HttpPatch("{id}/pickup")]
    public async Task<IActionResult> MarkAsPickedUp(int id)
    {
        var delivery = await _context.Deliveries.FindAsync(id);
        if (delivery == null)
        {
            return NotFound();
        }

        delivery.Statusi = DeliveryStatus.PickedUp;
        delivery.DataMarrjes = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new { id = delivery.Id, statusi = delivery.Statusi });
    }

    [HttpPatch("{id}/deliver")]
    public async Task<IActionResult> MarkAsDelivered(int id)
    {
        var delivery = await _context.Deliveries.FindAsync(id);
        if (delivery == null)
        {
            return NotFound();
        }

        delivery.Statusi = DeliveryStatus.Delivered;
        delivery.DataDorezimit = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new { id = delivery.Id, statusi = delivery.Statusi });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDelivery(int id)
    {
        var delivery = await _context.Deliveries.FindAsync(id);
        if (delivery == null)
        {
            return NotFound();
        }

        _context.Deliveries.Remove(delivery);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool DeliveryExists(int id)
    {
        return _context.Deliveries.Any(e => e.Id == id);
    }
}