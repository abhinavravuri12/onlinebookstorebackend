using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using MyBookShopAPI.Data;

using MyBookShopAPI.Models;

namespace OnlineBookStoreAPI.Controllers

{

    [ApiController]

    [Route("api/[controller]")]

    [Authorize(Roles = "Admin")]

    public class AdminController : ControllerBase

    {

        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)

        {

            _context = context;

        }

        [HttpGet("customers")]

        public async Task<IActionResult> GetAllCustomers()

        {

            var customers = await _context.Users

                .Where(u => u.Role == "Customer" || u.Role == "Admin")

                .Select(u => new

                {

                    u.Id,

                    u.Username,

                    u.Email,

                    u.Role,

                    u.Status,

                    OrderCount = _context.Orders.Count(o => o.UserId == u.Id),

                    LastOrderDate = _context.Orders

                        .Where(o => o.UserId == u.Id)

                        .OrderByDescending(o => o.OrderDate)

                        .Select(o => o.OrderDate)

                        .FirstOrDefault()

                })

                .ToListAsync();

            return Ok(customers);

        }

        [HttpGet("customers/ordercount")]

        public async Task<IActionResult> GetTotalOrderCount()

        {

            var totalOrderCount = await _context.Orders.CountAsync();

            return Ok(new { totalOrderCount });

        }


        [HttpPut("customers/{id}")]

        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CustomerUpdateModel model)

        {

            var user = await _context.Users.FindAsync(id);

            if (user == null || (user.Role != "Customer" && user.Role != "Admin"))

                return NotFound(new { message = "Customer not found" });

            user.Email = model.Email;

            user.Role = model.Role;

            user.Status = model.Status;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Customer updated successfully." });

        }

        [HttpGet("orders/summary")]

        public async Task<IActionResult> GetOrderSummary()

        {

            var totalOrders = await _context.Orders.CountAsync();

            var totalCustomers = await _context.Users.CountAsync(u => u.Role == "Customer");

            return Ok(new

            {

                totalOrders,

                totalCustomers

            });

        }

        [HttpGet("queries")]

        public async Task<IActionResult> GetAllQueries()

        {

            var queries = await _context.CustomerQueries

                .OrderByDescending(q => q.CreatedAt)

                .ToListAsync();

            return Ok(queries);

        }

        [HttpPut("queries/reply/{id}")]

        public async Task<IActionResult> ReplyToQuery(int id, [FromBody] string reply)

        {

            var query = await _context.CustomerQueries.FindAsync(id);

            if (query == null)

                return NotFound(new { message = "Query not found" });

            query.AdminReply = reply;

            query.RepliedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Reply sent successfully!" });

        }

    }

    public class CustomerUpdateModel

    {

        public string Email { get; set; }

        public string Role { get; set; }

        public string Status { get; set; }

    }

}

