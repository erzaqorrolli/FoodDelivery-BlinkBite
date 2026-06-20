using FoodDeliveryyy.Data;
using FoodDeliveryyy.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FoodDeliveryyy.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]

public class InvoiceController : ControllerBase { 

private readonly AppDbContext _appDbContext;

    public InvoiceController (AppDbContext appDbContext )
    {
        _appDbContext = appDbContext;
    }

    [HttpGet("order/{orderId}")]
    public async Task<IActionResult> GetInvoiceByOrder(int orderId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;


        var order = await _appDbContext.Orders
            .Include(o=>o.User)
            .Include(o=>o.Restaurant)
            .FirstOrDefaultAsync(o=>o.Id==orderId);

        if (order == null)
            return NotFound("Order not found");

        if (role != "Admin" && order.UserId != userId && role != "Merchant")
            return Forbid();

        var invoice = await _appDbContext.Invoices
            .FirstOrDefaultAsync(i => i.OrderId == orderId);

        if (invoice == null)
            return NotFound("Invoice not found for this order");

        return Ok(invoice);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetInvoice(int id)
    {
        var invoice = await _appDbContext.Invoices
            .Include(i => i.Order)
            .ThenInclude(o => o.Restaurant)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
            return NotFound();

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (role != "Admin" && invoice.Order?.UserId != userId && role != "Merchant")
            return Forbid();

        return Ok(invoice);
    }
}