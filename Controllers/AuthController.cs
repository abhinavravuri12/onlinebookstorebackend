using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using Microsoft.IdentityModel.Tokens;

using MyBookShopAPI.Data;

using MyBookShopAPI.DTOs;

using MyBookShopAPI.Models;

using System.IdentityModel.Tokens.Jwt;

using System.Security.Claims;

using System.Security.Cryptography;

using System.Text;

namespace MyBookShopAPI.Controllers

{

    [ApiController]

    [Route("api/[controller]")]

    public class AuthController : ControllerBase

    {

        private readonly ApplicationDbContext _context;

        private readonly IConfiguration _config;

        private static RSA _privateRsa = RSA.Create(2048);

        public AuthController(ApplicationDbContext context, IConfiguration config)

        {

            _context = context;

            _config = config;

        }

        [HttpPost("register")]

        public async Task<IActionResult> Register([FromBody] RegisterDto dto)

        {

            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))

                return BadRequest(new { message = "Email already registered." });

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new User

            {

                Username = dto.Username,

                Email = dto.Email,

                PasswordHash = passwordHash,

                Role = dto.Role ?? "Customer"

            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Registration successful!" });

        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Username) || string.IsNullOrEmpty(dto.Password))
                return BadRequest(new { message = "Invalid request." });
        
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Username || u.Username == dto.Username);
            if (user == null)
                return Unauthorized(new { message = "Invalid username or password." });
           
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid username or password." });
          
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();
           
            var token = GenerateJwtToken(user);
            return Ok(new
            {
                message = "Login successful!",
                token,
                user = new { user.Id, user.Username, user.Email, user.Role, user.LastLogin }
            });
        }

        [HttpGet("public-key")]

        [AllowAnonymous]

        public IActionResult GetPublicKey()

        {

            var spki = _privateRsa.ExportSubjectPublicKeyInfo();

            var base64 = Convert.ToBase64String(spki);

            return Ok(new { key = base64 });

        }

        private string GenerateJwtToken(User user)

        {

            var jwtSettings = _config.GetSection("Jwt");

            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? throw new Exception("Missing jwt:Key"));

            var claims = new List<Claim>

            {

                new Claim(JwtRegisteredClaimNames.Sub, user.Email),

                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

                new Claim("userId", user.Id.ToString()),

                new Claim("username", user.Username),

                new Claim(ClaimTypes.Role, user.Role)

            };

            var token = new JwtSecurityToken(

                issuer: jwtSettings["Issuer"],

                audience: jwtSettings["Audience"],

                claims: claims,

                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpireMinutes"] ?? "60")),

                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)

            );

            return new JwtSecurityTokenHandler().WriteToken(token);

        }

    }

}
