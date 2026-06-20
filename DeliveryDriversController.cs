using FoodDeliveryyy.Data;
using FoodDeliveryyy.Models.Entities;
using FoodDeliveryyy.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodDeliveryyy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeliveryDriversController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DeliveryDriversController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DeliveryDrivers>>> GetDeliveryDrivers()
        {
            var driversDb = _context.DeliveryDrivers;
            if (driversDb == null)
            {
                return Ok(new List<DeliveryDrivers>());
            }

            return await driversDb!
                .Include(d => d.User!)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DeliveryDrivers>> GetDeliveryDriver(int id)
        {
            var driversDb = _context.DeliveryDrivers;
            if (driversDb == null)
            {
                return NotFound();
            }

            var driver = await driversDb!
                .Include(d => d.User!)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (driver == null)
            {
                return NotFound();
            }

            return driver;
        }

        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<DeliveryDrivers>>> GetAvailableDrivers()
        {
            var driversDb = _context.DeliveryDrivers;
            if (driversDb == null)
            {
                return Ok(new List<DeliveryDrivers>());
            }

            var drivers = await driversDb!
                .Where(d => d.Statusi == DriverStatus.Available)
                .Include(d => d.User!)
                .ToListAsync();

            return Ok(drivers);
        }

        [HttpGet("by-zone/{zone}")]
        public async Task<ActionResult<IEnumerable<DeliveryDrivers>>> GetDriversByZone(string zone)
        {
            var driversDb = _context.DeliveryDrivers;
            if (driversDb == null)
            {
                return Ok(new List<DeliveryDrivers>());
            }

            var drivers = await driversDb!
                .Where(d => d.Zona == zone)
                .Include(d => d.User!)
                .ToListAsync();

            return Ok(drivers);
        }

        [HttpGet("by-status/{status}")]
        public async Task<ActionResult<IEnumerable<DeliveryDrivers>>> GetDriversByStatus(string status)
        {
            if (!Enum.TryParse<DriverStatus>(status, true, out var parsedStatus))
            {
                return BadRequest("Status i papranueshëm për shoferin");
            }

            var driversDb = _context.DeliveryDrivers;
            if (driversDb == null)
            {
                return Ok(new List<DeliveryDrivers>());
            }

            var drivers = await driversDb!
                .Where(d => d.Statusi == parsedStatus)
                .Include(d => d.User!)
                .ToListAsync();

            return Ok(drivers);
        }

        [HttpPost]
        public async Task<ActionResult<DeliveryDrivers>> CreateDeliveryDriver(DeliveryDrivers driver)
        {
            if (driver == null)
            {
                return BadRequest("Driver payload is required");
            }

            var usersDb = _context.Users;
            if (usersDb == null)
            {
                return BadRequest("Users store is not available");
            }

            var user = await usersDb!.FindAsync(driver.UserId);
            if (user == null)
            {
                return BadRequest("Përdoruesi nuk ekziston");
            }

            driver.Statusi = DriverStatus.Available;

            var driversDb = _context.DeliveryDrivers;
            if (driversDb == null)
            {
                return BadRequest("Delivery drivers store is not available");
            }

            driversDb!.Add(driver);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDeliveryDriver), new { id = driver.Id }, driver);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDeliveryDriver(int id, DeliveryDrivers driver)
        {
            if (driver == null || id != driver.Id)
            {
                return BadRequest();
            }

            var driversDb = _context.DeliveryDrivers;
            if (driversDb == null)
            {
                return NotFound();
            }

            // Load the tracked entity, update fields, then save.
            var existing = await driversDb.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            // Update only allowed/simple properties (avoid replacing navigation properties directly)
            existing.Zona = driver.Zona;
            existing.Statusi = driver.Statusi;
            existing.UserId = driver.UserId;
            // ...copy any other updatable scalar properties here...

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DeliveryDriverExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateDriverStatus(int id, [FromBody] string status)
        {
            var driversDb = _context.DeliveryDrivers;
            if (driversDb == null)
            {
                return NotFound();
            }

            var driver = await driversDb!.FindAsync(id);
            if (driver == null)
            {
                return NotFound();
            }

            if (!Enum.TryParse<DriverStatus>(status, true, out var parsedStatus))
            {
                return BadRequest("Statusi duhet të jetë: Available, Busy, ose Offline");
            }

            driver.Statusi = parsedStatus;
            await _context.SaveChangesAsync();

            return Ok(new { id = driver.Id, statusi = driver.Statusi });
        }

        [HttpPatch("{id}/zone")]
        public async Task<IActionResult> UpdateDriverZone(int id, [FromBody] string zone)
        {
            var driversDb = _context.DeliveryDrivers;
            if (driversDb == null)
            {
                return NotFound();
            }

            var driver = await driversDb!.FindAsync(id);
            if (driver == null)
            {
                return NotFound();
            }

            driver.Zona = zone;
            await _context.SaveChangesAsync();

            return Ok(new { id = driver.Id, zona = driver.Zona });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDeliveryDriver(int id)
        {
            var driversDb = _context.DeliveryDrivers;
            if (driversDb == null)
            {
                return NotFound();
            }

            var driver = await driversDb!.FindAsync(id);
            if (driver == null)
            {
                return NotFound();
            }

            var deliveriesDb = _context.Deliveries;
            if (deliveriesDb == null)
            {
                // if deliveries set is not available assume no active deliveries
                _context.DeliveryDrivers.Remove(driver);
                await _context.SaveChangesAsync();
                return NoContent();
            }

            var hasActiveDeliveries = await deliveriesDb!
                .AnyAsync(d => d.DriverId == id && d.Statusi != DeliveryStatus.Delivered && d.Statusi != DeliveryStatus.Failed);

            if (hasActiveDeliveries)
            {
                return BadRequest("Shoferi ka dërgesa aktive dhe nuk mund të fshihet");
            }

            _context.DeliveryDrivers.Remove(driver);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("{id}/deliveries")]
        public async Task<ActionResult<IEnumerable<Deliveries>>> GetDriverDeliveries(int id)
        {
            var deliveriesDb = _context.Deliveries;
            if (deliveriesDb == null)
            {
                return Ok(new List<Deliveries>());
            }

            var deliveries = await deliveriesDb!
                .Where(d => d.DriverId == id)
                .Include(d => d.Order!)
                .ThenInclude(o => o!.Restaurant!)
                .OrderByDescending(d => d.DataMarrjes)
                .ToListAsync();

            return Ok(deliveries);
        }

        private bool DeliveryDriverExists(int id)
        {
            return _context.DeliveryDrivers?.Any(e => e.Id == id) ?? false;
        }
    }
}