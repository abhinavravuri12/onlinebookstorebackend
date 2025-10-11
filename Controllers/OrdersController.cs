using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBookShopAPI.Data;
using MyBookShopAPI.DTOs;
using MyBookShopAPI.Models;
using System.Security.Claims;

namespace MyBookShopAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetUserIdFromClaims()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId");
            if (userIdClaim == null)
                throw new Exception("userId claim missing");
            return int.Parse(userIdClaim.Value);
        }

        // ✅ Checkout Endpoint
        [HttpPost("checkout")]
        [Authorize]
        public async Task<IActionResult> Checkout([FromBody] CheckoutDto dto)
        {
            var userId = GetUserIdFromClaims();

            // Get user's cart items (not ordered yet)
            var cartItems = await _context.CartItems
                .Include(ci => ci.Book)
                .Where(ci => ci.UserId == userId && ci.OrderId == null)
                .ToListAsync();

            if (!cartItems.Any())
                return BadRequest(new { message = "Cart is empty." });

            // Check stock
            foreach (var ci in cartItems)
            {
                if (ci.Book.StockQuantity < ci.Quantity)
                    return BadRequest(new { message = $"Not enough stock for '{ci.Book.Title}'." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // ✅ Create Order
                var order = new Order
                {
                    UserId = userId,
                    OrderDate = DateTime.UtcNow,
                    Status = "Pending",
                    ShippingAddress = dto.ShippingAddress,
                    PaymentMethod = dto.PaymentMethod
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                decimal total = 0m;

                // ✅ Create OrderItems & update stock
                foreach (var ci in cartItems)
                {
                    var book = await _context.Books.FindAsync(ci.BookId);
                    if (book == null)
                        return BadRequest(new { message = $"Book with ID {ci.BookId} not found." });

                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        BookId = book.Id,
                        BookTitle = book.Title, // ✅ Added BookTitle
                        Quantity = ci.Quantity,
                        Price = book.Price
                    };

                    _context.OrderItems.Add(orderItem);

                    // Update stock
                    book.StockQuantity -= ci.Quantity;
                    _context.Books.Update(book);

                    total += ci.Quantity * book.Price;

                    // Link CartItem to order
                    ci.OrderId = order.Id;
                    _context.CartItems.Update(ci);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // ✅ Build Response
                var orderItems = await _context.OrderItems
                    .Include(oi => oi.Book)
                    .Where(oi => oi.OrderId == order.Id)
                    .ToListAsync();

                var orderDto = new OrderDto
                {
                    Id = order.Id,
                    OrderDate = order.OrderDate,
                    Status = order.Status,
                    TotalAmount = total,
                    Items = orderItems.Select(oi => new OrderItemDto
                    {
                        BookId = oi.BookId,
                        Quantity = oi.Quantity,
                        Price = oi.Price,
                        Title = oi.BookTitle // ✅ Use stored BookTitle
                    }).ToList()
                };

                return Ok(new { message = "Order placed successfully!", order = orderDto });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    message = "Error placing order.",
                    detail = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        // ✅ Get logged-in user’s orders
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUserOrders()
        {
            var userId = GetUserIdFromClaims();

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Book)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var orderDtos = orders.Select(o => new OrderDto
            {
                Id = o.Id,
                OrderDate = o.OrderDate,
                Status = o.Status,
                TotalAmount = o.OrderItems.Sum(oi => oi.Price * oi.Quantity),
                Items = o.OrderItems.Select(oi => new OrderItemDto
                {
                    BookId = oi.BookId,
                    Quantity = oi.Quantity,
                    Price = oi.Price,
                    Title = oi.BookTitle ?? oi.Book.Title
                }).ToList()
            }).ToList();

            return Ok(orderDtos);
        }

        // ✅ Get a specific order by ID (user or admin)
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var userId = GetUserIdFromClaims();

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Book)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound(new { message = "Order not found." });

            var isAdmin = User.IsInRole("Admin");
            if (order.UserId != userId && !isAdmin)
                return Forbid();

            var orderDto = new OrderDto
            {
                Id = order.Id,
                OrderDate = order.OrderDate,
                Status = order.Status,
                TotalAmount = order.OrderItems.Sum(oi => oi.Price * oi.Quantity),
                Items = order.OrderItems.Select(oi => new OrderItemDto
                {
                    BookId = oi.BookId,
                    Quantity = oi.Quantity,
                    Price = oi.Price,
                    Title = oi.BookTitle ?? oi.Book.Title
                }).ToList()
            };

            return Ok(orderDto);
        }

        // ✅ Admin: Get all orders
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Book)
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var orderDtos = orders.Select(o => new
            {
                o.Id,
                o.User.Username,
                o.OrderDate,
                o.Status,
                TotalAmount = o.OrderItems.Sum(oi => oi.Price * oi.Quantity),
                Items = o.OrderItems.Select(oi => new
                {
                    oi.BookId,
                    Title = oi.BookTitle ?? oi.Book.Title,
                    oi.Quantity,
                    oi.Price
                })
            });

            return Ok(orderDtos);
        }

        // ✅ Admin: Update order status
        [HttpPut("admin/update-status/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound(new { message = "Order not found." });

            order.Status = status;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order status updated successfully.", orderId = id, status });
        }
    }
}
