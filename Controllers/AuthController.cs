using EduSync.Configurations;
using EduSync.Data;
using EduSync.DTOs;
using EduSync.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EduSync.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtConfig _jwtConfig;

        public AuthController(ApplicationDbContext context, IOptions<JwtConfig> jwtConfig)
        {
            _context = context;
            _jwtConfig = jwtConfig.Value;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                return BadRequest(new { message = "Email is already registered" });
            }

            // Create password hash
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

            // Create new user
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Name = model.Name,
                Email = model.Email,
                PasswordHash = passwordHash,
                Role = model.Role,
                CreatedAt = DateTime.UtcNow
            };

            // Add user to database
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Generate JWT token
            var token = GenerateJwtToken(user);
            var expirationTime = DateTime.UtcNow.AddMinutes(_jwtConfig.ExpiryInMinutes);

            return Ok(new AuthResponseDto
            {
                Success = true,
                Token = token,
                Expiration = expirationTime,
                User = new UserDto
                {
                    UserId = user.UserId,
                    Name = user.Name,
                    Email = user.Email,
                    Role = user.Role
                },
                Message = "Registration successful"
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Find user by email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Verify password
            bool passwordValid = false;
            
            try
            {
                // Try BCrypt verification first
                passwordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // If BCrypt verification fails with SaltParseException, it might be an old format password
                // For transition, let's use a simple comparison (assuming old format was just the password itself)
                // This is just for transition and should be updated with your actual legacy verification method
                passwordValid = user.PasswordHash == model.Password;
                
                // If valid, update to new BCrypt format for future logins
                if (passwordValid)
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                    await _context.SaveChangesAsync();
                }
            }
            
            if (!passwordValid)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Generate JWT token
            var token = GenerateJwtToken(user);
            var expirationTime = DateTime.UtcNow.AddMinutes(_jwtConfig.ExpiryInMinutes);

            return Ok(new AuthResponseDto
            {
                Success = true,
                Token = token,
                Expiration = expirationTime,
                User = new UserDto
                {
                    UserId = user.UserId,
                    Name = user.Name,
                    Email = user.Email,
                    Role = user.Role
                },
                Message = "Login successful"
            });
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetUserProfile()
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var userGuid = Guid.Parse(userId);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userGuid);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new UserDto
            {
                UserId = user.UserId,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role
            });
        }

        [Authorize]
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var userGuid = Guid.Parse(userId);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userGuid);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
            {
                return BadRequest(new { message = "Current password is incorrect" });
            }

            // Create new password hash
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

            // Update user password
            user.PasswordHash = passwordHash;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully" });
        }
        // No longer needed as we're using BCrypt

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtConfig.Secret);

            var claims = new List<Claim>
            {
                new Claim("UserId", user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtConfig.ExpiryInMinutes),
                Issuer = _jwtConfig.Issuer,
                Audience = _jwtConfig.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
