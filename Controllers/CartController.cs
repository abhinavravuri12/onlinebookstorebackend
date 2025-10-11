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
    public class CartController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
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

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetCart()
        {
            var userId = GetUserIdFromClaims();
            var items = await _context.CartItems
                .Include(ci => ci.Book)
                .Where(ci => ci.UserId == userId && ci.OrderId == null)
                .ToListAsync();

            var result = items.Select(ci => new
            {
                ci.Id,
                ci.BookId,
                BookTitle = ci.Book.Title,
                BookAuthor = ci.Book.Author,
                UnitPrice = ci.Book.Price,
                ci.Quantity,
                Subtotal = ci.Book.Price * ci.Quantity
            });

            var total = result.Sum(r => r.Subtotal);

            return Ok(new { items = result, total });
        }

        [HttpPost("add")]
        [Authorize]
        public async Task<IActionResult> AddToCart([FromBody] CartItemDto dto)
        {
            if (dto.Quantity <= 0)
                return BadRequest(new { message = "Quantity must be greater than 0." });

            var userId = GetUserIdFromClaims();

            var book = await _context.Books.FindAsync(dto.BookId);
            if (book == null)
                return NotFound(new { message = "Book not found." });

            if (book.StockQuantity < dto.Quantity)
                return BadRequest(new { message = "Not enough stock." });

            // If existing cart item for same book (not checked out), update it
            var existing = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.UserId == userId && ci.BookId == dto.BookId && ci.OrderId == null);

            if (existing != null)
            {
                existing.Quantity += dto.Quantity;
            }
            else
            {
                var cartItem = new CartItem
                {
                    BookId = dto.BookId,
                    Quantity = dto.Quantity,
                    UserId = userId
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Added to cart." });
        }

        [HttpPut("update")]
        [Authorize]
        public async Task<IActionResult> UpdateCartItem([FromBody] UpdateCartItemDto dto)
        {
            if (dto.Quantity <= 0)
                return BadRequest(new { message = "Quantity must be greater than 0." });

            var userId = GetUserIdFromClaims();

            var cartItem = await _context.CartItems
                .Include(ci => ci.Book)
                .FirstOrDefaultAsync(ci => ci.Id == dto.CartItemId && ci.UserId == userId && ci.OrderId == null);

            if (cartItem == null)
                return NotFound(new { message = "Cart item not found." });

            if (cartItem.Book.StockQuantity < dto.Quantity)
                return BadRequest(new { message = "Not enough stock." });

            cartItem.Quantity = dto.Quantity;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Cart updated." });
        }

        [HttpDelete("{cartItemId}")]
        [Authorize]
        public async Task<IActionResult> RemoveCartItem(int cartItemId)
        {
            var userId = GetUserIdFromClaims();
            var cartItem = await _context.CartItems.FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.UserId == userId && ci.OrderId == null);

            if (cartItem == null)
                return NotFound(new { message = "Cart item not found." });

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Removed from cart." });
        }

        [HttpDelete("clear")]
        [Authorize]
        public async Task<IActionResult> ClearCart()
        {
            var userId = GetUserIdFromClaims();
            var items = _context.CartItems.Where(ci => ci.UserId == userId && ci.OrderId == null);
            _context.CartItems.RemoveRange(items);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Cart cleared." });
        }
    }
}

