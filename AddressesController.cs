using FoodDeliveryyy.Data;
using FoodDeliveryyy.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FoodDeliveryyy.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AddressesController : ControllerBase
{
    private readonly AppDbContext _context;

    public AddressesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<Addresses>>> GetAddresses()
    {
        return await _context.Addresses
            .Include(a => a.User)
            .ToListAsync();
    }

    [HttpGet("my")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<IEnumerable<Addresses>>> GetMyAddresses()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var addresses = await _context.Addresses
            .Where(a => a.UserId == userId)
            .ToListAsync();
        return Ok(addresses);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Customer")]
    public async Task<ActionResult<Addresses>> GetAddress(int id)
    {
        var address = await _context.Addresses
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (address == null)
            return NotFound();

        if (User.IsInRole("Customer") && address.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            return Forbid();

        return address;
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<Addresses>> CreateAddress(Addresses address)
    {
        
        address.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (address.EshteKryesore)
        {
            var existingPrimary = await _context.Addresses
                .FirstOrDefaultAsync(a => a.UserId == address.UserId && a.EshteKryesore);
            if (existingPrimary != null)
                existingPrimary.EshteKryesore = false;
        }

        _context.Addresses.Add(address);
        await _context.SaveChangesAsync();

        var createdAddress = await _context.Addresses
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == address.Id);

        return CreatedAtAction(nameof(GetAddress), new { id = address.Id }, createdAddress);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> UpdateAddress(int id, Addresses address)
    {
        if (id != address.Id)
            return BadRequest();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (address.UserId != userId)
            return Forbid();

        if (address.EshteKryesore)
        {
            var existingPrimary = await _context.Addresses
                .FirstOrDefaultAsync(a => a.UserId == address.UserId && a.EshteKryesore && a.Id != id);
            if (existingPrimary != null)
                existingPrimary.EshteKryesore = false;
        }

        _context.Entry(address).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!AddressExists(id))
                return NotFound();
            throw;
        }

        return NoContent();
    }

    [HttpPatch("{id}/set-primary")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> SetPrimaryAddress(int id)
    {
        var address = await _context.Addresses.FindAsync(id);
        if (address == null)
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (address.UserId != userId)
            return Forbid();

        var userAddresses = await _context.Addresses
            .Where(a => a.UserId == userId)
            .ToListAsync();

        foreach (var addr in userAddresses)
            addr.EshteKryesore = (addr.Id == id);

        await _context.SaveChangesAsync();
        return Ok(new { id = address.Id, eshteKryesore = true });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> DeleteAddress(int id)
    {
        var address = await _context.Addresses.FindAsync(id);
        if (address == null)
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (address.UserId != userId)
            return Forbid();

        var wasPrimary = address.EshteKryesore;
        _context.Addresses.Remove(address);
        await _context.SaveChangesAsync();

        if (wasPrimary)
        {
            var anotherAddress = await _context.Addresses
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (anotherAddress != null)
            {
                anotherAddress.EshteKryesore = true;
                await _context.SaveChangesAsync();
            }
        }

        return NoContent();
    }

    private bool AddressExists(int id)
    {
        return _context.Addresses.Any(e => e.Id == id);
    }
}