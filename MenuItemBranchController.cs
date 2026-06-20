using Microsoft.AspNetCore.Mvc;
using FoodDeliveryyy.Models.Entities;
using FoodDeliveryyy.Data;
using Microsoft.AspNetCore.Authorization;
using FoodDeliveryyy.Models.Identity;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace FoodDeliveryyy.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Merchant + "," + AppRoles.BranchManager + "," + AppRoles.Admin)]
public class MenuItemBranchController : ControllerBase
{
    private readonly AppDbContext _context;

    public MenuItemBranchController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{itemId}/branch/{branchId}")]
    public async Task<IActionResult> GetBranchMenuItem(int itemId, int branchId)
    {
        var mib = await _context.MenuItemBranch
            .FirstOrDefaultAsync(x => x.MenuItemId == itemId && x.RestaurantAddressId == branchId);

        if (mib == null)
            return NotFound();

        return Ok(mib);
    }

    [HttpPut("{itemId}/branch/{branchId}")]
    public async Task<IActionResult> UpdateBranchMenuItem(int itemId, int branchId, [FromBody] MenuItemBranchUpdateDto branchData)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        if (role == AppRoles.BranchManager)
        {
            var branch = await _context.RestaurantAddresses
                .FirstOrDefaultAsync(b => b.Id == branchId && b.MerchantUserId == userId);
            if (branch == null)
                return Forbid();
        }

        var mib = await _context.MenuItemBranch
            .FirstOrDefaultAsync(x => x.MenuItemId == itemId && x.RestaurantAddressId == branchId);

        if (mib == null)
        {
            mib = new MenuItemBranch
            {
                MenuItemId = itemId,
                RestaurantAddressId = branchId
            };
            _context.MenuItemBranch.Add(mib);
        }

        if (branchData.Cmimi.HasValue)
            mib.Cmimi = branchData.Cmimi.Value;

        if (branchData.Disponueshme.HasValue)
            mib.Disponueshme = branchData.Disponueshme.Value;

        if (branchData.Perberesit != null)
            mib.Perberesit = branchData.Perberesit;

        if (branchData.RequestOptions != null)
            mib.RequestOptions = branchData.RequestOptions;

        if (branchData.PromotionId.HasValue)
            mib.PromotionId = branchData.PromotionId;

        await _context.SaveChangesAsync();

        // Kthe të dhënat e plota për refresh
        return Ok(new
        {
            message = "Branch menu item updated successfully",
            data = mib
        });
    }

    [HttpDelete("{itemId}/branch/{branchId}")]
    public async Task<IActionResult> DeleteBranchMenuItem(int itemId, int branchId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        if (role == AppRoles.BranchManager)
        {
            var branch = await _context.RestaurantAddresses
                .FirstOrDefaultAsync(b => b.Id == branchId && b.MerchantUserId == userId);
            if (branch == null)
                return Forbid();
        }

        var mib = await _context.MenuItemBranch
            .FirstOrDefaultAsync(x => x.MenuItemId == itemId && x.RestaurantAddressId == branchId);

        if (mib == null)
            return NotFound();

        _context.MenuItemBranch.Remove(mib);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Branch customization removed" });
    }
}

public class MenuItemBranchUpdateDto
{
    public decimal? Cmimi { get; set; }
    public bool? Disponueshme { get; set; }
    public string? Perberesit { get; set; }
    public string? RequestOptions { get; set; }
    public int? PromotionId { get; set; }
}