using FoodDeliveryyy.Data;
using FoodDeliveryyy.Models.Entities;
using FoodDeliveryyy.Models.Enums;
using FoodDeliveryyy.Models.Identity;
using FoodDeliveryyy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FoodDeliveryyy.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IOrderService _orderService;

    public OrdersController(AppDbContext context, IOrderService orderService)
    {
        _context = context;
        _orderService = orderService;
    }

    // GET: api/orders
    [HttpGet]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Merchant + "," + AppRoles.BranchManager)]
    public async Task<ActionResult<IEnumerable<Orders>>> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        IQueryable<Orders> query = _context.Orders
            .Include(o => o.User)
            .Include(o => o.Restaurant)
            .Include(o => o.OrderItems);

        if (role == AppRoles.Merchant)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.UserId == userId);
            if (restaurant != null)
            {
                query = query.Where(o => o.RestaurantId == restaurant.Id);
            }
        }
        else if (role == AppRoles.BranchManager)
        {
            var managedAddressIds = await _context.RestaurantAddresses
                .Where(a => a.MerchantUserId == userId)
                .Select(a => a.Id)
                .ToListAsync();
            query = query.Where(o => o.RestaurantAddressId.HasValue && managedAddressIds.Contains(o.RestaurantAddressId.Value));
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var orders = await query.OrderByDescending(o => o.DataPorosis).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new
        {
            Data = orders,
            Pagination = new
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasPrevious = page > 1,
                HasNext = page < totalPages
            }
        });
    }

    // GET: api/orders/pending
    [HttpGet("pending")]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Merchant + "," + AppRoles.BranchManager)]
    public async Task<ActionResult<IEnumerable<Orders>>> GetPendingOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        IQueryable<Orders> query = _context.Orders
            .Where(o => o.Statusi == OrderStatus.Pending)
            .Include(o => o.Restaurant)
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.DataPorosis);

        if (role == AppRoles.Merchant)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.UserId == userId);
            if (restaurant != null)
            {
                query = query.Where(o => o.RestaurantId == restaurant.Id);
            }
            else
            {
                return Ok(new List<Orders>());
            }
        }
        else if (role == AppRoles.BranchManager)
        {
            var managedAddressIds = await _context.RestaurantAddresses
                .Where(a => a.MerchantUserId == userId)
                .Select(a => a.Id)
                .ToListAsync();
            query = query.Where(o => o.RestaurantAddressId.HasValue && managedAddressIds.Contains(o.RestaurantAddressId.Value));
        }

        var totalCount = await query.CountAsync();
        var orders = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new
        {
            Data = orders,
            Pagination = new
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            }
        });
    }

    // GET: api/orders/in-progress
    [HttpGet("in-progress")]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Merchant + "," + AppRoles.BranchManager)]
    public async Task<ActionResult<IEnumerable<Orders>>> GetInProgressOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        IQueryable<Orders> query = _context.Orders
            .Where(o => o.Statusi == OrderStatus.Accepted || o.Statusi == OrderStatus.Preparing || o.Statusi == OrderStatus.Ready)
            .Include(o => o.Restaurant)
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.DataPorosis);

        if (role == AppRoles.Merchant)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.UserId == userId);
            if (restaurant != null)
            {
                query = query.Where(o => o.RestaurantId == restaurant.Id);
            }
            else
            {
                return Ok(new List<Orders>());
            }
        }
        else if (role == AppRoles.BranchManager)
        {
            var managedAddressIds = await _context.RestaurantAddresses
                .Where(a => a.MerchantUserId == userId)
                .Select(a => a.Id)
                .ToListAsync();
            query = query.Where(o => o.RestaurantAddressId.HasValue && managedAddressIds.Contains(o.RestaurantAddressId.Value));
        }

        var totalCount = await query.CountAsync();
        var orders = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new
        {
            Data = orders,
            Pagination = new
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            }
        });
    }

    // GET: api/orders/all
    [HttpGet("all")]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Merchant + "," + AppRoles.BranchManager + "," + AppRoles.Courier)]
    public async Task<ActionResult<IEnumerable<Orders>>> GetAllOrdersForRole(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        IQueryable<Orders> query = _context.Orders
            .Include(o => o.Restaurant)
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.MenuItem)
            .OrderByDescending(o => o.DataPorosis);

        if (role == AppRoles.Merchant)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.UserId == userId);
            if (restaurant != null)
            {
                query = query.Where(o => o.RestaurantId == restaurant.Id);
            }
            else
            {
                return Ok(new List<Orders>());
            }
        }
        else if (role == AppRoles.BranchManager)
        {
            var managedAddressIds = await _context.RestaurantAddresses
                .Where(a => a.MerchantUserId == userId)
                .Select(a => a.Id)
                .ToListAsync();
            query = query.Where(o => o.RestaurantAddressId.HasValue && managedAddressIds.Contains(o.RestaurantAddressId.Value));
        }
        else if (role == AppRoles.Courier)
        {
            query = query.Where(o => o.Statusi == OrderStatus.Ready || o.Statusi == OrderStatus.Delivered);
        }

        var totalCount = await query.CountAsync();
        var orders = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new
        {
            Data = orders,
            Pagination = new
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            }
        });
    }

    // GET: api/orders/{id}
    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<Orders>> GetOrder(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Restaurant)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.MenuItem)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        if (role != AppRoles.Admin && order.UserId != userId && role != AppRoles.Merchant && role != AppRoles.BranchManager)
        {
            return Forbid();
        }

        if (role == AppRoles.Merchant)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.UserId == userId);
            if (restaurant == null || order.RestaurantId != restaurant.Id)
            {
                return Forbid();
            }
        }

        if (role == AppRoles.BranchManager)
        {
            var branch = await _context.RestaurantAddresses.FirstOrDefaultAsync(a => a.Id == order.RestaurantAddressId && a.MerchantUserId == userId);
            if (branch == null)
            {
                return Forbid();
            }
        }

        return order;
    }

    // GET: api/orders/by-user/{userId}
    [HttpGet("by-user/{userId}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<Orders>>> GetOrdersByUser(string userId)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (role != AppRoles.Admin && currentUserId != userId)
        {
            return Forbid();
        }

        var orders = await _context.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.DataPorosis)
            .Select(o => new
            {
                id = o.Id,
                userId = o.UserId,
                restaurantId = o.RestaurantId,
                restaurantAddressId = o.RestaurantAddressId,
                adresaDorezimit = o.AdresaDorezimit,
                shumaTotale = o.ShumaTotale,
                tarifaDorezimit = o.TarifaDorezimit,
                zbritja = o.Zbritja,
                statusi = o.Statusi.ToString(),
                metodaPageses = o.MetodaPageses.ToString(),
                dataPorosis = o.DataPorosis,
                shenimet = o.Shenimet,
                restaurant = o.Restaurant == null ? null : new
                {
                    id = o.Restaurant.Id,
                    emertimi = o.Restaurant.Emertimi,
                },
                orderItems = o.OrderItems.Select(oi => new
                {
                    id = oi.Id,
                    menuItemId = oi.MenuItemId,
                    sasia = oi.Sasia,
                    cmimi = oi.Cmimi,
                    shenimet = oi.Shenimet,
                }).ToList(),
            })
            .ToListAsync();

        return Ok(orders);
    }

    // GET: api/orders/my
    [HttpGet("my")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<Orders>>> GetMyOrders()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized("User identity is missing.");
        }

        var orders = await _context.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.DataPorosis)
            .Select(o => new
            {
                id = o.Id,
                userId = o.UserId,
                restaurantId = o.RestaurantId,
                restaurantAddressId = o.RestaurantAddressId,
                adresaDorezimit = o.AdresaDorezimit,
                shumaTotale = o.ShumaTotale,
                tarifaDorezimit = o.TarifaDorezimit,
                zbritja = o.Zbritja,
                statusi = o.Statusi.ToString(),
                metodaPageses = o.MetodaPageses.ToString(),
                dataPorosis = o.DataPorosis,
                shenimet = o.Shenimet,
                restaurant = o.Restaurant == null ? null : new
                {
                    id = o.Restaurant.Id,
                    emertimi = o.Restaurant.Emertimi,
                },
                orderItems = o.OrderItems.Select(oi => new
                {
                    id = oi.Id,
                    menuItemId = oi.MenuItemId,
                    sasia = oi.Sasia,
                    cmimi = oi.Cmimi,
                    shenimet = oi.Shenimet,
                }).ToList(),
            })
            .ToListAsync();

        return Ok(orders);
    }

    // GET: api/orders/by-restaurant/{restaurantId}
    [HttpGet("by-restaurant/{restaurantId}")]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Merchant + "," + AppRoles.BranchManager)]
    public async Task<ActionResult<IEnumerable<Orders>>> GetOrdersByRestaurant(int restaurantId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        if (role == AppRoles.Merchant)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.UserId == userId);
            if (restaurant == null || restaurant.Id != restaurantId)
            {
                return Forbid();
            }
        }

        if (role == AppRoles.BranchManager)
        {
            var canAccessRestaurant = await _context.RestaurantAddresses.AnyAsync(a => a.RestaurantId == restaurantId && a.MerchantUserId == userId);
            if (!canAccessRestaurant)
            {
                return Forbid();
            }
        }

        var orders = await _context.Orders
            .Where(o => o.RestaurantId == restaurantId)
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.DataPorosis)
            .ToListAsync();

        if (role == AppRoles.BranchManager)
        {
            orders = orders.Where(o => o.RestaurantAddressId.HasValue).ToList();
            var managedAddressIds = await _context.RestaurantAddresses
                .Where(a => a.MerchantUserId == userId)
                .Select(a => a.Id)
                .ToListAsync();
            orders = orders.Where(o => o.RestaurantAddressId.HasValue && managedAddressIds.Contains(o.RestaurantAddressId.Value)).ToList();
        }

        return Ok(orders);
    }

    // GET: api/orders/by-status/{status}
    [HttpGet("by-status/{status}")]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Merchant + "," + AppRoles.BranchManager)]
    public async Task<ActionResult<IEnumerable<Orders>>> GetOrdersByStatus(string status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (!Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
        {
            return BadRequest("Status is not valid. Allowed values: Pending, Accepted, Preparing, Ready, Delivered, Cancelled.");
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        IQueryable<Orders> query = _context.Orders
            .Where(o => o.Statusi == parsedStatus)
            .Include(o => o.Restaurant)
            .Include(o => o.User)
            .OrderByDescending(o => o.DataPorosis);

        if (role == AppRoles.Merchant)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.UserId == userId);
            if (restaurant != null)
            {
                query = query.Where(o => o.RestaurantId == restaurant.Id);
            }
            else
            {
                return Ok(new List<Orders>());
            }
        }
        else if (role == AppRoles.BranchManager)
        {
            var managedAddressIds = await _context.RestaurantAddresses
                .Where(a => a.MerchantUserId == userId)
                .Select(a => a.Id)
                .ToListAsync();
            query = query.Where(o => o.RestaurantAddressId.HasValue && managedAddressIds.Contains(o.RestaurantAddressId.Value));
        }

        var totalCount = await query.CountAsync();
        var orders = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new
        {
            Data = orders,
            Pagination = new
            {
                CurrentPage = page,
                PageSize = pageSize,
                totalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            }
        });
    }

    // POST: api/orders
    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<Orders>> CreateOrder(Orders order)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId) && string.IsNullOrWhiteSpace(order.UserId))
        {
            return Unauthorized("User identity is missing for this order.");
        }

        order.DataPorosis = DateTime.Now;
        order.Statusi = OrderStatus.Pending;
        order.UserId = !string.IsNullOrWhiteSpace(userId) ? userId : order.UserId;

        var restaurant = await _context.Restaurants.FindAsync(order.RestaurantId);
        if (restaurant == null)
        {
            return NotFound("Restaurant not found");
        }

        RestaurantAddress? branch = null;
        if (order.RestaurantAddressId.HasValue)
        {
            branch = await _context.RestaurantAddresses.FirstOrDefaultAsync(a => a.Id == order.RestaurantAddressId.Value);
            if (branch == null || branch.RestaurantId != restaurant.Id)
            {
                return BadRequest("Restaurant address not found");
            }
        }

        if (!order.OrderItems.Any())
        {
            return BadRequest("Order must contain at least one item.");
        }

        // Server-side price validation
        var menuItemIds = order.OrderItems.Select(oi => oi.MenuItemId).Distinct().ToList();
        var menuItems = await _context.MenuItems
            .Where(m => menuItemIds.Contains(m.Id))
            .ToListAsync();

        var branchDetails = order.RestaurantAddressId.HasValue
            ? await _context.MenuItemBranch
                .Where(b => b.RestaurantAddressId == order.RestaurantAddressId.Value && menuItemIds.Contains(b.MenuItemId))
                .ToListAsync()
            : new List<MenuItemBranch>();

        foreach (var item in order.OrderItems)
        {
            var menuItem = menuItems.FirstOrDefault(m => m.Id == item.MenuItemId);
            if (menuItem == null)
                return BadRequest($"Menu item {item.MenuItemId} not found.");

            var branchDetail = branchDetails.FirstOrDefault(b => b.MenuItemId == item.MenuItemId);
            item.Cmimi = branchDetail?.Cmimi ?? menuItem.Cmimi;
        }

        order.TarifaDorezimit = branch?.TarifaDorezimit ?? 0;
        order.ShumaTotale = order.OrderItems.Sum(oi => oi.Sasia * oi.Cmimi) + order.TarifaDorezimit - order.Zbritja;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var invoice = new Invoice
        {
            OrderId = order.Id,
            InvoiceNumber = GenerateInvoiceNumber(order.Id),
            Subtotal = order.ShumaTotale - order.TarifaDorezimit + order.Zbritja,
            DeliveryFee = order.TarifaDorezimit,
            Discount = order.Zbritja,
            Total = order.ShumaTotale,
            InvoiceDate = DateTime.UtcNow,
            PaymentMethod = order.MetodaPageses == PaymentMethod.Cash ? "Cash" : "Other",
            Notes = order.Shenimet
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        var createdOrderResponse = new
        {
            id = order.Id,
            userId = order.UserId,
            restaurantId = order.RestaurantId,
            restaurantAddressId = order.RestaurantAddressId,
            adresaDorezimit = order.AdresaDorezimit,
            shumaTotale = order.ShumaTotale,
            tarifaDorezimit = order.TarifaDorezimit,
            zbritja = order.Zbritja,
            statusi = order.Statusi.ToString(),
            metodaPageses = order.MetodaPageses.ToString(),
            dataPorosis = order.DataPorosis,
            shenimet = order.Shenimet,
            orderItems = order.OrderItems.Select(oi => new
            {
                menuItemId = oi.MenuItemId,
                sasia = oi.Sasia,
                cmimi = oi.Cmimi,
                shenimet = oi.Shenimet,
            }).ToList(),
        };

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, createdOrderResponse);
    }

    // PUT: api/orders/{id}/status
    [HttpPut("{id}/status")]
    [Authorize]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        var order = await _context.Orders.FindAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var parsedStatus))
        {
            return BadRequest("Status is not valid. Accepted values: Pending, Accepted, Preparing, Ready, Delivered, Cancelled");
        }

        var result = await _orderService.UpdateOrderStatusAsync(id, parsedStatus, userId, role, request.Comment);
        if (!result)
        {
            return BadRequest("Status update not allowed or order not found");
        }

        return Ok(new
        {
            id = id,
            statusi = parsedStatus.ToString(),
            updatedAt = DateTime.UtcNow,
        });
    }

    // POST: api/orders/{id}/accept
    [HttpPost("{id}/accept")]
    [Authorize(Roles = AppRoles.Merchant + "," + AppRoles.BranchManager)]
    public async Task<IActionResult> AcceptOrder(int id, [FromBody] string? comment = null)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        var order = await _context.Orders.Include(o => o.Restaurant).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null)
        {
            return NotFound();
        }

        if (role == AppRoles.Merchant)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.UserId == userId);
            if (restaurant == null || order.RestaurantId != restaurant.Id)
            {
                return Forbid();
            }
        }
        else if (role == AppRoles.BranchManager)
        {
            var branch = await _context.RestaurantAddresses.FirstOrDefaultAsync(a => a.Id == order.RestaurantAddressId && a.MerchantUserId == userId);
            if (branch == null)
            {
                return Forbid();
            }
        }

        var result = await _orderService.UpdateOrderStatusAsync(id, OrderStatus.Accepted, userId, role, comment);
        if (!result) return BadRequest("Cannot accept order. Make sure order is pending");

        return Ok(new { message = "Order accepted", status = "Accepted" });
    }

    // POST: api/orders/{id}/prepare
    [HttpPost("{id}/prepare")]
    [Authorize(Roles = AppRoles.Merchant + "," + AppRoles.BranchManager)]
    public async Task<IActionResult> StartPreparing(int id, [FromBody] string? comment = null)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        var order = await _context.Orders.Include(o => o.Restaurant).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) { return NotFound(); }

        if (role == AppRoles.Merchant)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.UserId == userId);
            if (restaurant == null || order.RestaurantId != restaurant.Id)
            {
                return Forbid();
            }
        }
        else if (role == AppRoles.BranchManager)
        {
            var branch = await _context.RestaurantAddresses.FirstOrDefaultAsync(a => a.Id == order.RestaurantAddressId && a.MerchantUserId == userId);
            if (branch == null)
            {
                return Forbid();
            }
        }

        var result = await _orderService.UpdateOrderStatusAsync(id, OrderStatus.Preparing, userId, role, comment);
        if (!result) return BadRequest("Cannot start preparing order. Make sure order is accepted");

        return Ok(new { message = "Order is being prepared", status = "Preparing" });
    }

    // POST: api/orders/{id}/ready
    [HttpPost("{id}/ready")]
    [Authorize(Roles = AppRoles.Merchant + "," + AppRoles.BranchManager)]
    public async Task<IActionResult> MarkAsReady(int id, [FromBody] string? comment = null)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = AppRoles.Normalize(User.FindFirst(ClaimTypes.Role)?.Value);

        var order = await _context.Orders.Include(o => o.Restaurant).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();

        if (role == AppRoles.Merchant)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.UserId == userId);
            if (restaurant == null || order.RestaurantId != restaurant.Id)
            {
                return Forbid();
            }
        }
        else if (role == AppRoles.BranchManager)
        {
            var branch = await _context.RestaurantAddresses.FirstOrDefaultAsync(a => a.Id == order.RestaurantAddressId && a.MerchantUserId == userId);
            if (branch == null)
            {
                return Forbid();
            }
        }

        var result = await _orderService.UpdateOrderStatusAsync(id, OrderStatus.Ready, userId, role, comment);
        if (!result) return BadRequest("Cannot mark as ready. Order must be preparing first.");

        return Ok(new { message = "Order is ready for delivery", status = "Ready" });
    }

    // POST: api/orders/{id}/deliver
    [HttpPost("{id}/deliver")]
    [Authorize(Roles = AppRoles.Courier)]
    public async Task<IActionResult> DeliverOrder(int id, [FromBody] string? comment = null)
    {
        // ===== LOGJET E REJA =====
        Console.WriteLine("=========================================");
        Console.WriteLine("🚚 DELIVER ORDER METHOD CALLED!");
        Console.WriteLine($"Time: {DateTime.Now}");
        Console.WriteLine($"Order ID: {id}");
        Console.WriteLine($"Comment: {comment ?? "null"}");
        // =========================

        var courierId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Console.WriteLine($"Courier ID from token: '{courierId}'");

        if (string.IsNullOrEmpty(courierId))
        {
            Console.WriteLine("❌ ERROR: Courier ID is NULL!");
            return Unauthorized(new { error = "Courier ID not found" });
        }

        var order = await _context.Orders.FindAsync(id);
        if (order == null)
        {
            Console.WriteLine($"❌ ERROR: Order {id} not found!");
            return NotFound();
        }

        Console.WriteLine($"Order found - Current Status: {order.Statusi}");

        if (order.Statusi != OrderStatus.Ready)
        {
            Console.WriteLine($"❌ ERROR: Order status is {order.Statusi}, must be Ready");
            return BadRequest($"Order must be ready first. Current status: {order.Statusi}");
        }

        // Shëno se cili courier e dorëzoi porosinë
        Console.WriteLine("✅ Updating order...");
        order.AssignedCourierId = courierId;
        order.AssignedAt = DateTime.Now;
        order.Statusi = OrderStatus.Delivered;

        await _context.SaveChangesAsync();

        Console.WriteLine($"✅ SUCCESS! Order {id} delivered by {courierId} at {order.AssignedAt}");
        Console.WriteLine("=========================================");

        return Ok(new
        {
            message = "Order delivered successfully",
            status = "Delivered",
            deliveredBy = courierId,
            deliveredAt = DateTime.Now
        });
    }

    // POST: api/orders/{id}/cancel
    [HttpPost("{id}/cancel")]
    [Authorize]
    public async Task<IActionResult> CancelOrder(int id, [FromBody] string? comment = null)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        var order = await _context.Orders
            .Include(o => o.Restaurant)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        if (order.Statusi != OrderStatus.Pending)
            return BadRequest("Only pending orders can be cancelled");

        if (role == AppRoles.Customer && order.UserId != userId)
            return Forbid();

        if (role == AppRoles.Merchant)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.UserId == userId);
            if (restaurant == null || order.RestaurantId != restaurant.Id)
                return Forbid();
        }

        if (role == AppRoles.BranchManager)
        {
            var branch = await _context.RestaurantAddresses.FirstOrDefaultAsync(a => a.Id == order.RestaurantAddressId && a.MerchantUserId == userId);
            if (branch == null)
                return Forbid();
        }

        if (role != AppRoles.Customer && role != AppRoles.Merchant && role != AppRoles.BranchManager && role != AppRoles.Admin)
            return Forbid();

        var result = await _orderService.UpdateOrderStatusAsync(id, OrderStatus.Cancelled, userId, role, comment);
        if (!result) return BadRequest("Cannot cancel order");

        return Ok(new { message = "Order cancelled", status = "Cancelled" });
    }

    // DELETE: api/orders/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        var orderItems = await _context.OrderItems.Where(oi => oi.OrderId == id).ToListAsync();
        _context.OrderItems.RemoveRange(orderItems);
        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // GET: api/orders/{id}/total
    [HttpGet("{id}/total")]
    [Authorize]
    public async Task<ActionResult<decimal>> GetOrderTotal(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();

        if (role != AppRoles.Admin && order.UserId != userId && role != AppRoles.Merchant && role != AppRoles.Courier)
        {
            return Forbid();
        }

        var total = await _context.OrderItems
            .Where(oi => oi.OrderId == id)
            .SumAsync(oi => oi.Sasia * oi.Cmimi);

        return Ok(new { orderId = id, total = total + order.TarifaDorezimit - order.Zbritja });
    }

    // GET: api/orders/{id}/history
    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetHistory(int id)
    {
        if (!await _orderService.OrderExistsAsync(id))
            return NotFound($"Order with ID {id} not found");

        var history = await _orderService.GetOrderHistoryAsync(id);
        return Ok(history);
    }

    // GET: api/orders/dashboard/stats
    [HttpGet("dashboard/stats")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> GetDashboardStats()
    {
        var today = DateTime.Today;
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
        var startOfMonth = new DateTime(today.Year, today.Month, 1);

        var stats = new
        {
            TodayOrders = await _context.Orders.CountAsync(o => o.DataPorosis.Date == today),
            TodayRevenue = await _context.Orders.Where(o => o.DataPorosis.Date == today).SumAsync(o => o.ShumaTotale),
            WeeklyOrders = await _context.Orders.CountAsync(o => o.DataPorosis.Date >= startOfWeek),
            WeeklyRevenue = await _context.Orders.Where(o => o.DataPorosis.Date >= startOfWeek).SumAsync(o => o.ShumaTotale),
            MonthlyOrders = await _context.Orders.CountAsync(o => o.DataPorosis.Date >= startOfMonth),
            MonthlyRevenue = await _context.Orders.Where(o => o.DataPorosis.Date >= startOfMonth).SumAsync(o => o.ShumaTotale),
            TotalOrders = await _context.Orders.CountAsync(),
            TotalRevenue = await _context.Orders.SumAsync(o => o.ShumaTotale),
            PendingOrders = await _context.Orders.CountAsync(o => o.Statusi == OrderStatus.Pending),
            AcceptedOrders = await _context.Orders.CountAsync(o => o.Statusi == OrderStatus.Accepted),
            PreparingOrders = await _context.Orders.CountAsync(o => o.Statusi == OrderStatus.Preparing),
            ReadyOrders = await _context.Orders.CountAsync(o => o.Statusi == OrderStatus.Ready),
            DeliveredOrders = await _context.Orders.CountAsync(o => o.Statusi == OrderStatus.Delivered),
            CancelledOrders = await _context.Orders.CountAsync(o => o.Statusi == OrderStatus.Cancelled)
        };

        return Ok(stats);
    }

    // GET: api/orders/health
    [HttpGet("health")]
    public IActionResult Health()
    {
        var isDbConnected = _context.Database.CanConnect();
        return Ok(new
        {
            status = "Healthy",
            timestamp = DateTime.Now,
            database = isDbConnected ? "Connected" : "Disconnected",
            message = "Food Delivery API is running!"
        });
    }

    // GET: api/orders/{id}/eta
    [HttpGet("{id}/eta")]
    [Authorize]
    public async Task<IActionResult> GetEstimatedDeliveryTime(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (role != AppRoles.Admin && order.UserId != userId && role != AppRoles.Courier)
            return Forbid();

        int estimatedMinutes = order.Statusi switch
        {
            OrderStatus.Pending => 45,
            OrderStatus.Accepted => 35,
            OrderStatus.Preparing => 25,
            OrderStatus.Ready => 15,
            OrderStatus.Delivered => 0,
            _ => 30
        };

        return Ok(new
        {
            OrderId = id,
            Status = order.Statusi.ToString(),
            EstimatedMinutes = estimatedMinutes,
            EstimatedTime = DateTime.Now.AddMinutes(estimatedMinutes)
        });
    }

    // GET: api/orders/top-restaurants
    [HttpGet("top-restaurants")]
    public async Task<ActionResult> GetTopRestaurants([FromQuery] int count = 5)
    {
        var topRestaurants = await _context.Orders
            .Where(o => o.Statusi == OrderStatus.Delivered)
            .GroupBy(o => o.RestaurantId)
            .Select(g => new
            {
                RestaurantId = g.Key,
                RestaurantName = g.First().Restaurant.Emertimi,
                TotalOrders = g.Count(),
                TotalRevenue = g.Sum(o => o.ShumaTotale)
            })
            .OrderByDescending(r => r.TotalOrders)
            .Take(count)
            .ToListAsync();

        return Ok(topRestaurants);
    }

    // GET: api/orders/busiest-hours
    [HttpGet("busiest-hours")]
    public async Task<IActionResult> GetBusiestHours()
    {
        var busiestHours = await _context.Orders
            .Where(o => o.Statusi == OrderStatus.Delivered)
            .GroupBy(o => o.DataPorosis.Hour)
            .Select(g => new
            {
                Hour = g.Key,
                OrderCount = g.Count(),
                TimeRange = $"{g.Key}:00 - {g.Key + 1:00}:00"
            })
            .OrderByDescending(h => h.OrderCount)
            .Take(5)
            .ToListAsync();

        return Ok(busiestHours);
    }

    // GET: api/orders/courier/my-deliveries
    [HttpGet("courier/my-deliveries")]
    [Authorize(Roles = AppRoles.Courier)]
    public async Task<IActionResult> GetMyDeliveries()
    {
        var courierId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var deliveries = await _context.Orders
            .Where(o => o.AssignedCourierId == courierId && o.Statusi == OrderStatus.Delivered)
            .Include(o => o.Restaurant)
            .OrderByDescending(o => o.AssignedAt)
            .Select(o => new
            {
                o.Id,
                RestaurantName = o.Restaurant != null ? o.Restaurant.Emertimi : "Unknown",
                o.AdresaDorezimit,
                o.ShumaTotale,
                o.AssignedAt,
                o.DataPorosis
            })
            .ToListAsync();

        return Ok(deliveries);
    }

    private string GenerateInvoiceNumber(int orderId)
    {
        return $"INV-{DateTime.UtcNow:yyyyMMdd}-{orderId:D6}";
    }
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Comment { get; set; }
}