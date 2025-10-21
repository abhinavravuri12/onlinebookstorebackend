using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBookShopAPI.Data;
using MyBookShopAPI.Models;
using System.Security.Claims;

namespace MyBookShopAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CustomerQueryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CustomerQueryController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateQuery([FromBody] CustomerQuery model)
        {
            var userId = int.Parse(User.FindFirst("userId").Value);
            model.UserId = userId;
            model.CreatedAt = DateTime.UtcNow;
            _context.CustomerQueries.Add(model);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Query submitted successfully!" });
        }

        [HttpGet("my-queries")]
        public async Task<IActionResult> GetUserQueries()
        {
            var userId = int.Parse(User.FindFirst("userId").Value);
            var queries = await _context.CustomerQueries
                .Where(q => q.UserId == userId)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();
            return Ok(queries);
        }
    }
}
