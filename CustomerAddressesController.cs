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
public class CustomerAddressesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<CustomerAddressesController> _logger;

    public CustomerAddressesController(AppDbContext context, ILogger<CustomerAddressesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/customer-addresses/my
    [HttpGet("my")]
    public async Task<IActionResult> GetMyAddresses()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("GetMyAddresses: User ID not found in token");
            return Unauthorized(new { error = "User not authenticated" });
        }

        _logger.LogInformation($"GetMyAddresses: Fetching addresses for user {userId}");

        var addresses = await _context.CustomerAddresses
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();

        return Ok(addresses);
    }

    // POST: api/customer-addresses
    [HttpPost]
    public async Task<IActionResult> CreateAddress([FromBody] CustomerAddress address)
    {
        _logger.LogInformation("=== CREATE ADDRESS CALLED ===");

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("CreateAddress: User ID not found in token");
            return Unauthorized(new { error = "User not authenticated" });
        }

        _logger.LogInformation($"User ID: {userId}");
        _logger.LogInformation($"Received address - AddressLine: {address.AddressLine}, City: {address.City}, Country: {address.Country}, PostalCode: {address.PostalCode}, IsDefault: {address.IsDefault}");

        // Validimi manual
        if (string.IsNullOrWhiteSpace(address.AddressLine))
        {
            _logger.LogWarning("AddressLine is required");
            return BadRequest(new { error = "AddressLine is required" });
        }

        if (string.IsNullOrWhiteSpace(address.City))
        {
            _logger.LogWarning("City is required");
            return BadRequest(new { error = "City is required" });
        }

        if (string.IsNullOrWhiteSpace(address.Country))
        {
            _logger.LogWarning("Country is required");
            return BadRequest(new { error = "Country is required" });
        }

        // Cakto vlerat
        address.UserId = userId;
        address.CreatedAt = DateTime.UtcNow;

        // Nëse kjo është adresa e parë, bëje default
        var existingAddresses = await _context.CustomerAddresses
            .Where(a => a.UserId == userId)
            .ToListAsync();

        if (existingAddresses.Count == 0)
        {
            address.IsDefault = true;
            _logger.LogInformation("First address for user, setting as default");
        }
        else if (address.IsDefault)
        {
            // Bëji të gjitha adresat e tjera jo-default
            foreach (var existing in existingAddresses)
            {
                existing.IsDefault = false;
            }
            _logger.LogInformation($"Set {existingAddresses.Count} existing addresses to non-default");
        }

        _context.CustomerAddresses.Add(address);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Address saved successfully with ID: {address.Id}");

        return Ok(new
        {
            message = "Address saved successfully",
            id = address.Id,
            addressLine = address.AddressLine,
            city = address.City,
            country = address.Country,
            postalCode = address.PostalCode,
            isDefault = address.IsDefault,
            createdAt = address.CreatedAt
        });
    }

    // PUT: api/customer-addresses/{id}/default
    [HttpPut("{id}/default")]
    public async Task<IActionResult> SetDefaultAddress(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var address = await _context.CustomerAddresses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (address == null)
            return NotFound(new { error = "Address not found" });

        // Bëji të gjitha adresat e tjera jo-default
        var allAddresses = await _context.CustomerAddresses
            .Where(a => a.UserId == userId)
            .ToListAsync();

        foreach (var a in allAddresses)
        {
            a.IsDefault = false;
        }

        address.IsDefault = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Address {id} set as default for user {userId}");

        return Ok(new
        {
            message = "Default address updated",
            id = address.Id,
            isDefault = address.IsDefault
        });
    }

    // DELETE: api/customer-addresses/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAddress(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var address = await _context.CustomerAddresses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (address == null)
            return NotFound(new { error = "Address not found" });

        _context.CustomerAddresses.Remove(address);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Address {id} deleted for user {userId}");

        return Ok(new { message = "Address deleted successfully" });
    }
}