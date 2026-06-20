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

[Route("api/[controller]")]
[ApiController]
public class PromotionsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PromotionsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Promotions>>> GetPromotions()
    {
        return await _context.Promotions
            .Include(p => p.Restaurant)
            .OrderByDescending(p => p.DataFillimit)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Promotions>> GetPromotion(int id)
    {
        var promotion = await _context.Promotions
            .Include(p => p.Restaurant)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (promotion == null)
        {
            return NotFound();
        }

        return promotion;
    }

    [HttpGet("by-restaurant/{restaurantId}")]
    public async Task<ActionResult<IEnumerable<Promotions>>> GetPromotionsByRestaurant(int restaurantId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        if (role == AppRoles.Merchant)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == restaurantId && r.UserId == userId);
            if (restaurant == null)
            {
                return Forbid();
            }
        }

        if (role == AppRoles.BranchManager)
        {
            var hasBranchAccess = await _context.RestaurantAddresses
                .AnyAsync(a => a.RestaurantId == restaurantId && a.MerchantUserId == userId);
            if (!hasBranchAccess)
            {
                return Forbid();
            }
        }

        var promotions = await _context.Promotions
            .Where(p => p.RestaurantId == restaurantId)
            .Include(p => p.Restaurant)
            .OrderByDescending(p => p.DataFillimit)
            .ToListAsync();

        return Ok(promotions);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<Promotions>>> GetActivePromotions()
    {
        var tani = DateTime.Now;

        var promotions = await _context.Promotions
            .Where(p => p.Statusi == PromotionStatus.Active &&
                        p.DataFillimit <= tani &&
                        p.DataPerfundimit >= tani)
            .Include(p => p.Restaurant)
            .ToListAsync();

        return Ok(promotions);
    }

    [HttpGet("active/by-restaurant/{restaurantId}")]
    public async Task<ActionResult<IEnumerable<Promotions>>> GetActivePromotionsByRestaurant(int restaurantId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        if (role == AppRoles.Merchant)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == restaurantId && r.UserId == userId);
            if (restaurant == null)
            {
                return Forbid();
            }
        }

        if (role == AppRoles.BranchManager)
        {
            var hasBranchAccess = await _context.RestaurantAddresses
                .AnyAsync(a => a.RestaurantId == restaurantId && a.MerchantUserId == userId);
            if (!hasBranchAccess)
            {
                return Forbid();
            }
        }

        var tani = DateTime.Now;

        var promotions = await _context.Promotions
            .Where(p => p.RestaurantId == restaurantId &&
                        p.Statusi == PromotionStatus.Active &&
                        p.DataFillimit <= tani &&
                        p.DataPerfundimit >= tani)
            .ToListAsync();

        return Ok(promotions);
    }

    [HttpGet("validate/{restaurantId}/{kodi}")]
    public async Task<ActionResult<object>> ValidatePromoCode(int restaurantId, string kodi)
    {
        var tani = DateTime.Now;

        var promotion = await _context.Promotions
            .FirstOrDefaultAsync(p => p.RestaurantId == restaurantId &&
                                       p.Kodi == kodi &&
                                       p.Statusi == PromotionStatus.Active &&
                                       p.DataFillimit <= tani &&
                                       p.DataPerfundimit >= tani);

        if (promotion == null)
        {
            return NotFound(new
            {
                valid = false,
                message = "Kodi promocional nuk është valid ose ka skaduar"
            });
        }

        return Ok(new
        {
            valid = true,
            zbritjaPerqind = promotion.ZbritjaPerqind,
            zbritjaMax = promotion.ZbritjaMax,
            message = $"Kodi valid! Zbritje {promotion.ZbritjaPerqind}%"
        });
    }
    [HttpPost]
    [Authorize(Roles = AppRoles.Merchant + "," + AppRoles.BranchManager + "," + AppRoles.Admin)]
    public async Task<ActionResult<Promotions>> CreatePromotion([FromBody] JsonElement promotionData)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        // Lexo RestaurantId
        if (!promotionData.TryGetProperty("restaurantId", out var restaurantIdProp))
            return BadRequest("RestaurantId is required");
        var restaurantId = restaurantIdProp.GetInt32();

        var restaurant = await _context.Restaurants.FindAsync(restaurantId);
        if (restaurant == null) return BadRequest("Restoranti nuk ekziston");

        if (role == AppRoles.Merchant && restaurant.UserId != userId)
            return Forbid();

        // Lexo Kodin
        if (!promotionData.TryGetProperty("kodi", out var kodiProp))
            return BadRequest("Kodi promocional është i detyrueshëm");
        var kodi = kodiProp.GetString();

        // Lexo ZbritjaPerqind
        if (!promotionData.TryGetProperty("zbritjaPerqind", out var zbritjaProp))
            return BadRequest("Zbritja është e detyrueshme");
        var zbritjaPerqind = zbritjaProp.GetDecimal();

        // Lexo ZbritjaMax (nëse nuk ekziston ose është null, vendos 0)
        decimal zbritjaMax = 0;
        if (promotionData.TryGetProperty("zbritjaMax", out var zbritjaMaxProp) && zbritjaMaxProp.ValueKind != JsonValueKind.Null)
        {
            zbritjaMax = zbritjaMaxProp.GetDecimal();
        }

        // Datat
        if (!promotionData.TryGetProperty("dataFillimit", out var dataFillimitProp))
            return BadRequest("Data e fillimit është e detyrueshme");
        var dataFillimit = dataFillimitProp.GetDateTime();

        if (!promotionData.TryGetProperty("dataPerfundimit", out var dataPerfundimitProp))
            return BadRequest("Data e përfundimit është e detyrueshme");
        var dataPerfundimit = dataPerfundimitProp.GetDateTime();

        if (dataFillimit >= dataPerfundimit)
            return BadRequest("Data e fillimit duhet të jetë para datës së përfundimit");

        // RestaurantAddressId opsional
        int? restaurantAddressId = null;
        if (promotionData.TryGetProperty("restaurantAddressId", out var addressIdProp) && addressIdProp.ValueKind != JsonValueKind.Null)
            restaurantAddressId = addressIdProp.GetInt32();

        if (role == AppRoles.BranchManager && !restaurantAddressId.HasValue)
            return BadRequest("Branch managers must provide restaurantAddressId.");

        if (restaurantAddressId.HasValue)
        {
            var address = await _context.RestaurantAddresses.FirstOrDefaultAsync(a => a.Id == restaurantAddressId.Value);
            if (address == null || address.RestaurantId != restaurantId)
                return BadRequest("Restaurant address does not exist");

            if (role == AppRoles.BranchManager && address.MerchantUserId != userId)
                return Forbid();
        }

        // Kontrollo nëse kodi ekziston për këtë restorant
        var existingPromo = await _context.Promotions
            .FirstOrDefaultAsync(p => p.RestaurantId == restaurantId && p.Kodi == kodi);
        if (existingPromo != null)
            return BadRequest("Ky kod promocional tashmë ekziston për këtë restorant");

        // Krijo promocionin
        var promotion = new Promotions
        {
            RestaurantId = restaurantId,
            Kodi = kodi,
            ZbritjaPerqind = zbritjaPerqind,
            ZbritjaMax = zbritjaMax,
            DataFillimit = dataFillimit,
            DataPerfundimit = dataPerfundimit,
            RestaurantAddressId = restaurantAddressId,
            Statusi = PromotionStatus.Inactive // inicial, do të rillogaritet më poshtë
        };

        // Llogarit statusin
        var tani = DateTime.Now;
        if (promotion.DataFillimit <= tani && promotion.DataPerfundimit >= tani)
            promotion.Statusi = PromotionStatus.Active;
        else if (promotion.DataPerfundimit < tani)
            promotion.Statusi = PromotionStatus.Expired;
        else
            promotion.Statusi = PromotionStatus.Inactive;

        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        var createdPromotion = await _context.Promotions
            .Include(p => p.Restaurant)
            .FirstOrDefaultAsync(p => p.Id == promotion.Id);

        return CreatedAtAction(nameof(GetPromotion), new { id = promotion.Id }, createdPromotion);
    }
    [HttpPut("{id}")]
    [Authorize(Roles = AppRoles.Merchant + "," + AppRoles.BranchManager + "," + AppRoles.Admin)]
    public async Task<IActionResult> UpdatePromotion(int id, [FromBody] JsonElement promotionData)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        // Gjej promocionin ekzistues
        var promotion = await _context.Promotions.FindAsync(id);
        if (promotion == null) return NotFound();

        // Verifiko autorizimin
        var restaurant = await _context.Restaurants.FindAsync(promotion.RestaurantId);
        if (restaurant == null) return BadRequest("Restoranti nuk ekziston");

        if (role == AppRoles.Merchant && restaurant.UserId != userId)
            return Forbid();

        if (role == AppRoles.BranchManager)
        {
            if (!promotion.RestaurantAddressId.HasValue)
                return Forbid();
            var address = await _context.RestaurantAddresses.FirstOrDefaultAsync(a => a.Id == promotion.RestaurantAddressId.Value);
            if (address == null || address.MerchantUserId != userId)
                return Forbid();
        }

        // Përditëso fushat që vijnë nga JSON
        if (promotionData.TryGetProperty("kodi", out var kodiProp))
            promotion.Kodi = kodiProp.GetString();

        if (promotionData.TryGetProperty("zbritjaPerqind", out var zbritjaProp))
            promotion.ZbritjaPerqind = zbritjaProp.GetDecimal();

        if (promotionData.TryGetProperty("zbritjaMax", out var zbritjaMaxProp) && zbritjaMaxProp.ValueKind != JsonValueKind.Null)
            promotion.ZbritjaMax = zbritjaMaxProp.GetDecimal();
        else
            promotion.ZbritjaMax = 0; // ose vlera ekzistuese nëse nuk dërgohet? Nëse nuk dërgohet, mos e ndrysho. Ndrysho vetëm nëse vjen.

        if (promotionData.TryGetProperty("dataFillimit", out var dataFillimitProp))
            promotion.DataFillimit = dataFillimitProp.GetDateTime();

        if (promotionData.TryGetProperty("dataPerfundimit", out var dataPerfundimitProp))
            promotion.DataPerfundimit = dataPerfundimitProp.GetDateTime();

        // Validimi i datave
        if (promotion.DataFillimit >= promotion.DataPerfundimit)
            return BadRequest("Data e fillimit duhet të jetë para datës së përfundimit");

        // Përditëso RestaurantAddressId nëse vjen
        if (promotionData.TryGetProperty("restaurantAddressId", out var addressIdProp) && addressIdProp.ValueKind != JsonValueKind.Null)
            promotion.RestaurantAddressId = addressIdProp.GetInt32();

        // Rillogarit statusin
        var tani = DateTime.Now;
        if (promotion.DataFillimit <= tani && promotion.DataPerfundimit >= tani)
            promotion.Statusi = PromotionStatus.Active;
        else if (promotion.DataPerfundimit < tani)
            promotion.Statusi = PromotionStatus.Expired;
        else
            promotion.Statusi = PromotionStatus.Inactive;

        // Nëse ke propertin Status (i cili nuk është mapped? Nëse është në model, mund ta sinkronizosh)
        // promotion.Status = promotion.Statusi; // nëse dëshiron t'i mbash të dyja

        await _context.SaveChangesAsync();
        return Ok(promotion);
    }
    [HttpPatch("{id}/status")]
    [Authorize(Roles = AppRoles.Merchant + "," + AppRoles.BranchManager + "," + AppRoles.Admin)]
    public async Task<IActionResult> UpdatePromotionStatus(int id, [FromBody] PromotionStatus status)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        var promotion = await _context.Promotions.FindAsync(id);
        if (promotion == null)
        {
            return NotFound();
        }

        if (role == AppRoles.Merchant)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == promotion.RestaurantId);
            if (restaurant == null || restaurant.UserId != userId)
            {
                return Forbid();
            }
        }

        if (role == AppRoles.BranchManager)
        {
            if (!promotion.RestaurantAddressId.HasValue)
            {
                return Forbid();
            }

            var address = await _context.RestaurantAddresses.FirstOrDefaultAsync(a => a.Id == promotion.RestaurantAddressId.Value);
            if (address == null || address.MerchantUserId != userId)
            {
                return Forbid();
            }
        }

        if (!Enum.IsDefined(typeof(PromotionStatus), status))
        {
            return BadRequest("Statusi duhet të jetë: Active, Expired, ose Inactive");
        }

        promotion.Statusi = status;
        await _context.SaveChangesAsync();

        return Ok(new { id = promotion.Id, statusi = promotion.Statusi.ToString() });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = AppRoles.Merchant + "," + AppRoles.BranchManager + "," + AppRoles.Admin)]
    public async Task<IActionResult> DeletePromotion(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        var promotion = await _context.Promotions.FindAsync(id);
        if (promotion == null)
        {
            return NotFound();
        }

        if (role == AppRoles.Merchant)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == promotion.RestaurantId);
            if (restaurant == null || restaurant.UserId != userId)
            {
                return Forbid();
            }
        }

        if (role == AppRoles.BranchManager)
        {
            if (!promotion.RestaurantAddressId.HasValue)
            {
                return Forbid();
            }

            var address = await _context.RestaurantAddresses.FirstOrDefaultAsync(a => a.Id == promotion.RestaurantAddressId.Value);
            if (address == null || address.MerchantUserId != userId)
            {
                return Forbid();
            }
        }

        _context.Promotions.Remove(promotion);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("expired")]
    public async Task<ActionResult<IEnumerable<Promotions>>> GetExpiredPromotions()
    {
        var tani = DateTime.Now;

        var promotions = await _context.Promotions
            .Where(p => p.DataPerfundimit < tani || p.Statusi == PromotionStatus.Expired)
            .Include(p => p.Restaurant)
            .ToListAsync();

        return Ok(promotions);
    }

    private bool PromotionExists(int id)
    {
        return _context.Promotions.Any(e => e.Id == id);
    }
}