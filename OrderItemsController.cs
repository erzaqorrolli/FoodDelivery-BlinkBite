using FoodDeliveryyy.Data;
using FoodDeliveryyy.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodDeliveryyy.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrderItemsController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrderItemsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderItems>>> GetOrderItems()
    {
        return await _context.OrderItems
            .Include(oi => oi.MenuItem)
            .Include(oi => oi.Order)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderItems>> GetOrderItem(int id)
    {
        var orderItem = await _context.OrderItems
            .Include(oi => oi.MenuItem)
            .Include(oi => oi.Order)
            .FirstOrDefaultAsync(oi => oi.Id == id);

        if (orderItem == null)
        {
            return NotFound();
        }

        return orderItem;
    }

    [HttpGet("by-order/{orderId}")]
    public async Task<ActionResult<IEnumerable<OrderItems>>> GetOrderItemsByOrder(int orderId)
    {
        var orderItems = await _context.OrderItems
            .Where(oi => oi.OrderId == orderId)
            .Include(oi => oi.MenuItem)
            .ToListAsync();

        return Ok(orderItems);
    }

    [HttpPost]
    public async Task<ActionResult<OrderItems>> CreateOrderItem(OrderItems orderItem)
    {
        var order = await _context.Orders.FindAsync(orderItem.OrderId);
        if (order == null)
        {
            return BadRequest("Porosia nuk ekziston");
        }

        var menuItem = await _context.MenuItems.FindAsync(orderItem.MenuItemId);
        if (menuItem == null)
        {
            return BadRequest("Artikulli nuk ekziston");
        }

        _context.OrderItems.Add(orderItem);
        await _context.SaveChangesAsync();

        await UpdateOrderTotal(orderItem.OrderId);

        var createdOrderItem = await _context.OrderItems
            .Include(oi => oi.MenuItem)
            .FirstOrDefaultAsync(oi => oi.Id == orderItem.Id);

        return CreatedAtAction(nameof(GetOrderItem), new { id = orderItem.Id }, createdOrderItem);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrderItem(int id, OrderItems orderItem)
    {
        if (id != orderItem.Id)
        {
            return BadRequest();
        }

        _context.Entry(orderItem).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();

            await UpdateOrderTotal(orderItem.OrderId);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!OrderItemExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrderItem(int id)
    {
        var orderItem = await _context.OrderItems.FindAsync(id);
        if (orderItem == null)
        {
            return NotFound();
        }

        var orderId = orderItem.OrderId;
        _context.OrderItems.Remove(orderItem);
        await _context.SaveChangesAsync();

        await UpdateOrderTotal(orderId);

        return NoContent();
    }

    [HttpDelete("by-order/{orderId}")]
    public async Task<IActionResult> DeleteOrderItemsByOrder(int orderId)
    {
        var orderItems = await _context.OrderItems
            .Where(oi => oi.OrderId == orderId)
            .ToListAsync();

        if (orderItems.Any())
        {
            _context.OrderItems.RemoveRange(orderItems);
            await _context.SaveChangesAsync();

            await UpdateOrderTotal(orderId);
        }

        return NoContent();
    }

    private async Task UpdateOrderTotal(int orderId)
    {
        var total = await _context.OrderItems
            .Where(oi => oi.OrderId == orderId)
            .SumAsync(oi => oi.Sasia * oi.Cmimi);

        var order = await _context.Orders.FindAsync(orderId);
        if (order != null)
        {
            order.ShumaTotale = total;
            await _context.SaveChangesAsync();
        }
    }

    private bool OrderItemExists(int id)
    {
        return _context.OrderItems.Any(e => e.Id == id);
    }
}