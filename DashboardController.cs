using FoodDeliveryyy.Data;
using FoodDeliveryyy.Models.Entities;
using FoodDeliveryyy.Models.Enums;
using FoodDeliveryyy.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FoodDeliveryyy.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public DashboardController(AppDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpGet("Admin")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> GetAdminDashboard()
    {
        var today = DateTime.Today;
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
        var startOfMonth = new DateTime(today.Year, today.Month, 1);

        var allUsers = await _context.Users.ToListAsync();
        var userRoles = await _context.UserRoles.ToListAsync();
        var roles = await _context.Roles.ToListAsync();

        var customerRoleId = roles.FirstOrDefault(r => r.Name == "Customer")?.Id;
        var merchantRoleId = roles.FirstOrDefault(r => r.Name == "Merchant")?.Id;
        var courierRoleId = roles.FirstOrDefault(r => r.Name == "Courier")?.Id;

        var customers = userRoles.Count(ur => ur.RoleId == customerRoleId);
        var merchants = userRoles.Count(ur => ur.RoleId == merchantRoleId);
        var couriers = userRoles.Count(ur => ur.RoleId == courierRoleId);

        var dashboard = new
        {
            Orders = new
            {
                Total = await _context.Orders.CountAsync(),
                Today = await _context.Orders.CountAsync(o => o.DataPorosis.Date == today),
                Pending = await _context.Orders.CountAsync(o => o.Statusi == OrderStatus.Pending),
                Delivered = await _context.Orders.CountAsync(o => o.Statusi == OrderStatus.Delivered),
                Cancelled = await _context.Orders.CountAsync(o => o.Statusi == OrderStatus.Cancelled)
            },
            Revenue = new
            {
                Today = await _context.Orders.Where(o => o.DataPorosis.Date == today).SumAsync(o => o.ShumaTotale),
                ThisMonth = await _context.Orders.Where(o => o.DataPorosis.Date >= startOfMonth).SumAsync(o => o.ShumaTotale),
                Total = await _context.Orders.SumAsync(o => o.ShumaTotale)
            },
            Users = new
            {
                Total = allUsers.Count(),
                Customers = customers,
                Merchants = merchants,
                Couriers = couriers,
                NewToday = await _context.Users.CountAsync(u => u.CreatedAt.Date == today)
            },
            Restaurants = new
            {
                Total = await _context.Restaurants.CountAsync(),
                Active = await _context.Restaurants.CountAsync(r => r.Statusi == RestaurantStatus.Active),
                Pending = await _context.Restaurants.CountAsync(r => r.Statusi == RestaurantStatus.Pending)
            },
            Reviews = new
            {
                AverageRating = await _context.Reviews.AverageAsync(r => r.Vlersimi),
                Total = await _context.Reviews.CountAsync(),
                Today = await _context.Reviews.CountAsync(r => r.DataKrijimit.Date == today)
            }
        };

        return Ok(dashboard);
    }

    [HttpGet("Merchant")]
    [Authorize(Roles = AppRoles.Merchant + "," + AppRoles.BranchManager)]
    public async Task<IActionResult> GetMerchantDashboard()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

            Restaurant? restaurant;
            List<dynamic> addresses;
            List<int> addressIds;

            if (role == AppRoles.BranchManager)
            {
                var branchAddresses = await _context.RestaurantAddresses
                    .Where(a => a.MerchantUserId == userId)
                    .ToListAsync();

                if (!branchAddresses.Any())
                    return NotFound("No branch found for this branch manager");

                // Sigurohu që po merr restorantin nga branch-i i parë
                var restaurantId = branchAddresses[0].RestaurantId;
                restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == restaurantId);

                if (restaurant == null)
                    return NotFound("No restaurant found for this branch manager");

                addresses = branchAddresses.Select(a => new
                {
                    a.Id,
                    a.Adresa,
                    a.Qyteti,
                    a.Zona,
                    a.IsMain,
                    a.IsActive,
                    a.Latitude,
                    a.Longitude,
                    a.TarifaDorezimit,
                    a.MerchantUserId
                }).Cast<dynamic>().ToList();
                addressIds = branchAddresses.Select(a => a.Id).ToList();
            }
            else
            {
                restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.UserId == userId);
                if (restaurant == null)
                    return NotFound("No restaurant found for this merchant");

                var ownerAddresses = await _context.RestaurantAddresses
                    .Where(a => a.RestaurantId == restaurant.Id)
                    .Select(a => new
                    {
                        a.Id,
                        a.Adresa,
                        a.Qyteti,
                        a.Zona,
                        a.IsMain,
                        a.IsActive,
                        a.Latitude,
                        a.Longitude,
                        a.TarifaDorezimit,
                        a.MerchantUserId
                    })
                    .ToListAsync();

                addresses = ownerAddresses.Cast<dynamic>().ToList();
                addressIds = ownerAddresses.Select(a => a.Id).ToList();
            }

            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var previousMonthStart = startOfMonth.AddMonths(-1);
            var previousMonthEnd = startOfMonth.AddDays(-1);

            var scopedOrders = _context.Orders.AsQueryable();
            if (role == AppRoles.BranchManager)
            {
                scopedOrders = scopedOrders.Where(o => o.RestaurantAddressId.HasValue && addressIds.Contains(o.RestaurantAddressId.Value));
            }
            else
            {
                scopedOrders = scopedOrders.Where(o => o.RestaurantId == restaurant!.Id);
            }

            var ordersWithItems = await scopedOrders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .ToListAsync();

            var topProductsPerBranch = new Dictionary<int, List<TopProductDto>>();
            foreach (var branchId in addressIds)
            {
                var branchOrders = ordersWithItems
                    .Where(o => o.RestaurantAddressId == branchId)
                    .ToList();

                var productSales = branchOrders
                    .SelectMany(o => o.OrderItems)
                    .GroupBy(oi => oi.MenuItemId)
                    .Select(g => new TopProductDto
                    {
                        MenuItemId = g.Key,
                        Name = g.First().MenuItem?.Emertimi ?? $"Item {g.Key}",
                        TotalQuantity = g.Sum(oi => oi.Sasia),
                        TotalRevenue = g.Sum(oi => oi.Cmimi * oi.Sasia)
                    })
                    .OrderByDescending(p => p.TotalQuantity)
                    .Take(5)
                    .ToList();

                topProductsPerBranch[branchId] = productSales;
            }

            var last7Days = Enumerable.Range(0, 7)
                .Select(i => today.AddDays(-i))
                .OrderBy(d => d)
                .ToList();

            var revenueTrend = last7Days.Select(day => new
            {
                Date = day.ToString("yyyy-MM-dd"),
                Revenue = scopedOrders
                    .Where(o => o.DataPorosis.Date == day)
                    .Sum(o => o.ShumaTotale)
            }).ToList();

            // Kjo është e saktë tani
            var revenueByBranch = addressIds.Select(branchId => new
            {
                BranchId = branchId,
                BranchName = addresses.FirstOrDefault(a => (int)a.Id == branchId)?.Adresa ?? "Unknown",
                Revenue = scopedOrders
                    .Where(o => o.RestaurantAddressId == branchId)
                    .Sum(o => o.ShumaTotale)
            }).OrderByDescending(r => r.Revenue).ToList();

            var currentMonthRevenue = scopedOrders
                .Where(o => o.DataPorosis.Date >= startOfMonth)
                .Sum(o => o.ShumaTotale);

            var previousMonthRevenue = scopedOrders
                .Where(o => o.DataPorosis.Date >= previousMonthStart && o.DataPorosis.Date <= previousMonthEnd)
                .Sum(o => o.ShumaTotale);

            var growthPercentage = previousMonthRevenue > 0
                ? ((currentMonthRevenue - previousMonthRevenue) / previousMonthRevenue) * 100
                : 0;

            var primaryAddressId = addresses.FirstOrDefault(a => (bool)(a?.IsMain ?? false))?.Id ?? addresses.FirstOrDefault()?.Id;
            var dashboard = new
            {
                Restaurant = new
                {
                    restaurant!.Id,
                    restaurant.Emertimi,
                    restaurant.Statusi,
                    restaurant.Rating,
                    restaurant.Logo
                },
                PrimaryAddressId = primaryAddressId,
                Addresses = addresses,
                Scope = role == AppRoles.BranchManager ? "branch" : "owner",
                Orders = new
                {
                    Total = scopedOrders.Count(),
                    Today = scopedOrders.Count(o => o.DataPorosis.Date == today),
                    ThisWeek = scopedOrders.Count(o => o.DataPorosis.Date >= startOfWeek),
                    ThisMonth = scopedOrders.Count(o => o.DataPorosis.Date >= startOfMonth),
                    Pending = scopedOrders.Count(o => o.Statusi == OrderStatus.Pending),
                    Accepted = scopedOrders.Count(o => o.Statusi == OrderStatus.Accepted),
                    Preparing = scopedOrders.Count(o => o.Statusi == OrderStatus.Preparing),
                    Ready = scopedOrders.Count(o => o.Statusi == OrderStatus.Ready),
                    Delivered = scopedOrders.Count(o => o.Statusi == OrderStatus.Delivered),
                    Cancelled = scopedOrders.Count(o => o.Statusi == OrderStatus.Cancelled)
                },
                Revenue = new
                {
                    Today = scopedOrders.Where(o => o.DataPorosis.Date == today).Sum(o => o.ShumaTotale),
                    ThisWeek = scopedOrders.Where(o => o.DataPorosis.Date >= startOfWeek).Sum(o => o.ShumaTotale),
                    ThisMonth = scopedOrders.Where(o => o.DataPorosis.Date >= startOfMonth).Sum(o => o.ShumaTotale),
                    Total = scopedOrders.Sum(o => o.ShumaTotale),
                    GrowthPercentage = Math.Round(growthPercentage, 2)
                },
                RevenueByBranch = role == AppRoles.BranchManager ? null : revenueByBranch,
                RevenueTrend = revenueTrend,
                BranchTopProducts = role == AppRoles.BranchManager
                    ? topProductsPerBranch.GetValueOrDefault(addressIds.FirstOrDefault(), new List<TopProductDto>())
                    : null,
                AllBranchesTopProducts = role == AppRoles.BranchManager
                    ? null
                    : topProductsPerBranch,
                BranchComparison = role == AppRoles.BranchManager ? null : new
                {
                    BestBranch = revenueByBranch.FirstOrDefault(),
                    WorstBranch = revenueByBranch.LastOrDefault(),
                    TotalBranches = addressIds.Count,
                    ActiveBranches = addresses.Count(a => (bool)a.IsActive)
                },
                RecentOrders = await scopedOrders
                    .OrderByDescending(o => o.DataPorosis)
                    .Take(10)
                    .Select(o => new
                    {
                        o.Id,
                        o.ShumaTotale,
                        o.Statusi,
                        o.DataPorosis,
                        CustomerName = o.User.UserName,
                        BranchName = o.RestaurantAddress != null ? o.RestaurantAddress.Adresa : "Unknown"
                    })
                    .ToListAsync(),
                Reviews = new
                {
                    Average = restaurant.Rating,
                    Total = await _context.Reviews.CountAsync(r => r.RestaurantId == restaurant.Id)
                }
            };

            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("Merchant/upload-logo")]
    [Authorize(Roles = AppRoles.Merchant + "," + AppRoles.BranchManager)]
    public async Task<IActionResult> UploadLogo([FromQuery] int restaurantId, IFormFile logo)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

            Restaurant? restaurant = null;

            if (role == AppRoles.BranchManager)
            {
                var branchAddress = await _context.RestaurantAddresses
                    .FirstOrDefaultAsync(a => a.Id == restaurantId && a.MerchantUserId == userId);
                if (branchAddress == null)
                    return NotFound(new { message = "Restaurant not found for this branch manager" });
                restaurant = await _context.Restaurants
                    .FirstOrDefaultAsync(r => r.Id == branchAddress.RestaurantId);
            }
            else
            {
                restaurant = await _context.Restaurants
                    .FirstOrDefaultAsync(r => r.Id == restaurantId && r.UserId == userId);
            }

            if (restaurant == null)
                return NotFound(new { message = "Restaurant not found" });

            if (logo == null || logo.Length == 0)
                return BadRequest(new { message = "Please select an image" });

            var webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsFolder = Path.Combine(webRootPath, "uploads", "logos");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(logo.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await logo.CopyToAsync(stream);
            }

            var oldLogoPath = !string.IsNullOrEmpty(restaurant.Logo)
                ? Path.Combine(webRootPath, restaurant.Logo.TrimStart('/'))
                : null;

            restaurant.Logo = $"/uploads/logos/{uniqueFileName}";
            await _context.SaveChangesAsync();

            if (oldLogoPath != null && System.IO.File.Exists(oldLogoPath))
            {
                try { System.IO.File.Delete(oldLogoPath); } catch { }
            }

            return Ok(new { message = "Logo uploaded successfully!", logoUrl = restaurant.Logo });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error uploading logo: {ex.Message}" });
        }
    }

    [HttpGet("Driver")]
    [Authorize(Roles = AppRoles.Courier)]
    public async Task<IActionResult> GetDriverDashboard()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var driver = await _context.DeliveryDrivers.FirstOrDefaultAsync(d => d.UserId == userId);
        if (driver == null)
        {
            driver = new DeliveryDrivers
            {
                UserId = userId!,
                Automjeti = "N/A",
                Targa = "N/A",
                Zona = "N/A",
                Statusi = DriverStatus.Available,
                Vlersimi = 0
            };
            _context.DeliveryDrivers.Add(driver);
            await _context.SaveChangesAsync();
        }

        var today = DateTime.Today;
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);

        // ✅ ADD THIS - Available orders (Ready dhe pa shofer)
        var availableOrders = await _context.Orders
            .Where(o => o.Statusi == OrderStatus.Ready
                     && o.AssignedCourierId == null
                     && !_context.Deliveries.Any(d => d.OrderId == o.Id && d.Statusi != DeliveryStatus.Delivered))
            .Include(o => o.Restaurant)
            .Select(o => new
            {
                o.Id,
                o.AdresaDorezimit,
                o.ShumaTotale,
                RestaurantName = o.Restaurant.Emertimi,
                o.Statusi,
                o.DataPorosis
            })
            .ToListAsync();

        var dashboard = new
        {
            Driver = new
            {
                driver.Id,
                driver.Automjeti,
                driver.Statusi,
                driver.Vlersimi
            },
            Deliveries = new
            {
                Total = await _context.Deliveries.CountAsync(d => d.DriverId == driver.Id),
                Today = await _context.Deliveries.CountAsync(d => d.DriverId == driver.Id && d.DataMarrjes != null && d.DataMarrjes.Value.Date == today),
                ThisWeek = await _context.Deliveries.CountAsync(d => d.DriverId == driver.Id && d.DataMarrjes != null && d.DataMarrjes.Value.Date >= startOfWeek),
                Completed = await _context.Deliveries.CountAsync(d => d.DriverId == driver.Id && d.Statusi == DeliveryStatus.Delivered)
            },

            AvailableOrders = availableOrders,

            CurrentOrders = await _context.Orders
.Where(o => o.AssignedCourierId == userId
         && o.Statusi != OrderStatus.Delivered
         && o.Statusi != OrderStatus.Cancelled)
.Include(o => o.Restaurant)
.Select(o => new
{
    o.Id,
    o.AdresaDorezimit,
    o.ShumaTotale,
    RestaurantName = o.Restaurant.Emertimi,
    o.Statusi,
    o.DataPorosis,
    o.AssignedAt
})
.ToListAsync(),

            DeliveryHistory = await _context.Orders
    .Where(o => o.AssignedCourierId == userId
             && o.Statusi == OrderStatus.Delivered)
    .Include(o => o.Restaurant)
    .OrderByDescending(o => o.AssignedAt)
    .Take(50)
    .Select(o => new
    {
        o.Id,
        o.AdresaDorezimit,
        o.ShumaTotale,
        RestaurantName = o.Restaurant.Emertimi,
        o.DataPorosis,
        DeliveredAt = o.AssignedAt
    })
    .ToListAsync(),
            Performance = new
            {
                Rating = driver.Vlersimi,
                TotalEarnings = await _context.Deliveries
                    .Where(d => d.DriverId == driver.Id && d.Statusi == DeliveryStatus.Delivered)
                    .SelectMany(d => _context.Orders.Where(o => o.Id == d.OrderId).Select(o => o.ShumaTotale))
                    .SumAsync()
            }
        };

        return Ok(dashboard);
    }

    [HttpPost("Driver/accept/{orderId}")]
    [Authorize(Roles = AppRoles.Courier)]
    public async Task<IActionResult> AcceptOrder(int orderId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized("Courier user id not found.");

        var driver = await _context.DeliveryDrivers.FirstOrDefaultAsync(d => d.UserId == userId);
        if (driver == null)
            return NotFound("Driver profile not found.");

        var order = await _context.Orders
            .Include(o => o.Delivery)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            return NotFound("Order not found.");

        if (order.Statusi != OrderStatus.Ready)
            return BadRequest("Order is not in Ready status.");

        if (!string.IsNullOrWhiteSpace(order.AssignedCourierId) || order.Delivery != null)
            return BadRequest("Order already has a driver assigned.");

        order.AssignedCourierId = userId;
        order.AssignedAt = DateTime.UtcNow;

        var delivery = new Deliveries
        {
            OrderId = orderId,
            DriverId = driver.Id,
            Statusi = DeliveryStatus.Pending,
            DataMarrjes = DateTime.UtcNow
        };

        _context.Deliveries.Add(delivery);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Order accepted.",
            deliveryId = delivery.Id,
            assignedCourierId = order.AssignedCourierId,
            assignedAt = order.AssignedAt
        });
    }
 
[HttpPost("Driver/mark-delivered/{orderId}")]
    [Authorize(Roles = AppRoles.Courier)]
    public async Task<IActionResult> MarkOrderDelivered(int orderId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized("Courier user id not found.");

        var driver = await _context.DeliveryDrivers.FirstOrDefaultAsync(d => d.UserId == userId);
        if (driver == null)
            return NotFound("Driver profile not found.");

        var delivery = await _context.Deliveries
            .FirstOrDefaultAsync(d => d.OrderId == orderId && d.DriverId == driver.Id);

        if (delivery == null)
            return NotFound("Delivery not found for this driver.");

        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
            return NotFound("Order not found.");

        // Update delivery status
        delivery.Statusi = DeliveryStatus.Delivered;
        delivery.DataDorezimit = DateTime.UtcNow;  // ← Përdor DataDorezimit, jo DataMbarimit!

        // Update order status
        order.Statusi = OrderStatus.Delivered;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Order marked as delivered.", orderId = order.Id });
    }
}
    public class TopProductDto
{
    public int MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class TopProductListItemDto
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public List<TopProductDto> Products { get; set; } = new List<TopProductDto>();
}