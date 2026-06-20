using FoodDeliveryyy.Data;
using FoodDeliveryyy.Models.Entities;
using FoodDeliveryyy.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace FoodDeliveryyy.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BranchRequestController : ControllerBase
{
    private readonly AppDbContext _context;

    public BranchRequestController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("request-edit")]
    [Authorize(Roles = AppRoles.Merchant)]
    public async Task<IActionResult> RequestEditBranch([FromBody] EditBranchRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var branch = await _context.RestaurantAddresses
            .Include(b => b.Restaurant)
            .FirstOrDefaultAsync(b => b.Id == request.BranchId);

        if (branch == null)
            return NotFound("Branch not found");

        if (branch.Restaurant.UserId != userId)
            return Forbid("You don't own this branch");

        var modificationRequest = new BranchModificationRequest
        {
            BranchId = request.BranchId,
            RequestType = "Edit",
            Status = "Pending",
            NewAddress = request.NewAddress,
            NewCity = request.NewCity,
            NewZone = request.NewZone,
            NewDeliveryFee = request.NewDeliveryFee,
            NewIsActive = request.NewIsActive,
            Reason = request.Reason,
            RequestedBy = userId,
            RequestedAt = DateTime.UtcNow
        };

        _context.BranchModificationRequests.Add(modificationRequest);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Edit request sent to admin", requestId = modificationRequest.Id });
    }

    [HttpPost("request-delete/{branchId}")]
    [Authorize(Roles = AppRoles.Merchant)]
    public async Task<IActionResult> RequestDeleteBranch(int branchId, [FromBody] string? reason)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var branch = await _context.RestaurantAddresses
            .Include(b => b.Restaurant)
            .FirstOrDefaultAsync(b => b.Id == branchId);

        if (branch == null)
            return NotFound("Branch not found");

        if (branch.Restaurant.UserId != userId)
            return Forbid("You don't own this branch");

        var modificationRequest = new BranchModificationRequest
        {
            BranchId = branchId,
            RequestType = "Delete",
            Status = "Pending",
            Reason = reason,
            RequestedBy = userId,
            RequestedAt = DateTime.UtcNow
        };

        _context.BranchModificationRequests.Add(modificationRequest);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Delete request sent to admin", requestId = modificationRequest.Id });
    }

    [HttpGet("pending")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> GetPendingRequests()
    {
        var requests = await _context.BranchModificationRequests
            .Include(r => r.Branch)
            .ThenInclude(b => b.Restaurant)
            .Include(r => r.Requester)
            .Where(r => r.Status == "Pending")
            .OrderBy(r => r.RequestedAt)
            .ToListAsync();

        return Ok(requests);
    }

    [HttpPost("approve/{requestId}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> ApproveRequest(int requestId)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var request = await _context.BranchModificationRequests
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null)
            return NotFound("Request not found");

        if (request.Status != "Pending")
            return BadRequest("Request already processed");

        request.Status = "Approved";
        request.ApprovedBy = adminId;
        request.ProcessedAt = DateTime.UtcNow;

        var branch = await _context.RestaurantAddresses.FindAsync(request.BranchId);
        if (branch != null)
        {
            if (request.RequestType == "Edit")
            {
                if (request.NewAddress != null) branch.Adresa = request.NewAddress;
                if (request.NewCity != null) branch.Qyteti = request.NewCity;
                if (request.NewZone != null) branch.Zona = request.NewZone;
                if (request.NewDeliveryFee.HasValue) branch.TarifaDorezimit = request.NewDeliveryFee.Value;
                if (request.NewIsActive.HasValue) branch.IsActive = request.NewIsActive.Value;
            }
            else if (request.RequestType == "Delete")
            {
                _context.RestaurantAddresses.Remove(branch);
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = $"Request {request.RequestType} approved successfully" });
    }

    [HttpPost("reject/{requestId}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> RejectRequest(int requestId, [FromBody] string? rejectionReason)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var request = await _context.BranchModificationRequests
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null)
            return NotFound("Request not found");

        if (request.Status != "Pending")
            return BadRequest("Request already processed");

        request.Status = "Rejected";
        request.ApprovedBy = adminId;
        request.ProcessedAt = DateTime.UtcNow;
        request.Reason = rejectionReason ?? request.Reason;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Request rejected" });
    }
}

public class EditBranchRequest
{
    public int BranchId { get; set; }
    public string? NewAddress { get; set; }
    public string? NewCity { get; set; }
    public string? NewZone { get; set; }
    public decimal? NewDeliveryFee { get; set; }
    public bool? NewIsActive { get; set; }
    public string? Reason { get; set; }
}