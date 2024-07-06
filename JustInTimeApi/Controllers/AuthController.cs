using JustInTimeApi.Context;
using JustInTimeApi.Dto;
using JustInTimeApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace JustInTimeApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly string _secretKey;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _secretKey = configuration["Jwt:SecretKey"];
        }

        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] UserLoginDto userLoginDto)
        {
            if (userLoginDto == null)
                return BadRequest("Invalid user data.");

            
            
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == userLoginDto.Email);

            if (user == null || !PasswordHasher.VerifyPassword(userLoginDto.Password, user.Password))
            {
                return Unauthorized(new { Message = "Invalid credentials." });
            }

            // Tworzenie tokenów
            var accessToken = CreateJwt(user);
            var refreshToken = CreateRefreshToken();

            user.Token = accessToken;
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return Ok(new TokenApiDto()
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserId = user.Id // Dodajemy identyfikator użytkownika
            });
        }

        [HttpPost("authenticate-supplier")]
        public async Task<IActionResult> AuthenticateSupplier([FromBody] UserLoginDto userLoginDto)
        {
            if (userLoginDto == null)
                return BadRequest("Invalid user data.");


            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(x => x.Email == userLoginDto.Email);

            if (supplier == null || !PasswordHasher.VerifyPassword(userLoginDto.Password, supplier.Password))
            {
                return Unauthorized(new { Message = "Invalid credentials." });
            }

            // Tworzenie tokenów
            var accessToken = CreateJwtForSupplier(supplier);
            var refreshToken = CreateRefreshToken();

            supplier.Token = accessToken;
            supplier.RefreshToken = refreshToken;
            supplier.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return Ok(new TokenApiDto()
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserId = supplier.Id
            });
        }
        
        private string CreateJwtForSupplier(Supplier supplier)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, supplier.Email),
                    new Claim(ClaimTypes.Role, supplier.Role),
                    new Claim(ClaimTypes.NameIdentifier, supplier.Id.ToString()) // Używamy ClaimTypes.NameIdentifier

                    // new Claim("UserId", user.Id.ToString()) // Dodajemy identyfikator użytkownika jako niestandardowy Claim
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserDto userDto)
        {
            if (userDto == null)
                return BadRequest("Invalid user data.");

            if (await _context.Users.AnyAsync(x => x.Email == userDto.Email))
            {
                return BadRequest(new { Message = "Email already exists." });
            }

            if (userDto.Password.Length < 8)
            {
                return BadRequest(new { Message = "Password must be at least 8 characters long." });
            }

            var newUser = new User
            {
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                Email = userDto.Email,
                PhoneNumber = userDto.PhoneNumber,
                Password = PasswordHasher.HashPassword(userDto.Password),
                // Role = userDto.Role,
                Role = "Admin",
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "User registered successfully." });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] TokenApiDto tokenApiDto)
        {
            if (tokenApiDto == null)
                return BadRequest("Invalid token data.");

            var accessToken = tokenApiDto.AccessToken;
            var refreshToken = tokenApiDto.RefreshToken;

            var principal = GetPrincipalFromExpiredToken(accessToken);
            var email = principal.Identity.Name;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return BadRequest("Invalid refresh token.");
            }

            var newAccessToken = CreateJwt(user);
            var newRefreshToken = CreateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return Ok(new TokenApiDto()
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                UserId = user.Id // Dodajemy identyfikator użytkownika
            });
        }

        private string CreateJwt(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) // Używamy ClaimTypes.NameIdentifier

                    // new Claim("UserId", user.Id.ToString()) // Dodajemy identyfikator użytkownika jako niestandardowy Claim
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        
        private string CreateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }

            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = false
            };

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                return principal;
            }
            catch
            {
                throw new SecurityTokenException("Invalid token.");
            }
        }
    }
}
