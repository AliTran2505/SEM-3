using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProjectSem3.Model;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using NuGet.Protocol;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net;
using Newtonsoft.Json;


namespace ProjectSem3.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowOrigin")]
    public class AccountControllers : ControllerBase
    {
        private readonly UserManager<Account> _userManager;
        private readonly SignInManager<Account> _signInManager;
        private readonly byte[] _key;
        private readonly ShopDbContext _dbContext;
        private IConfiguration _config;
        private readonly ILogger<AccountControllers> _logger;
        private IHttpContextAccessor _httpContextAccessor;




        public AccountControllers(
            UserManager<Account> userManager,
            IConfiguration config,
            ILogger<AccountControllers> logger,
            IHttpContextAccessor httpContextAccessor,
            SignInManager<Account> signInManager,
            ShopDbContext dbContext)
        {
            _logger = logger;
            _config = config;
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
            var x = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier); //get in the constructor
            _userManager = userManager;
            _signInManager = signInManager;
            _key = Encoding.UTF8.GetBytes("E2m3O4r5U6g7e8n9K1e2y3F4o5r6Y7o8u9"); // Replace with your secret key
        }

        [HttpPost("/register")]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            var user = new Account { UserName = model.UserName, Password = model.Password, RoleName = model.RoleName, Email = model.Email, PhoneNumber = model.PhoneNumber, Address = model.Address };

            var result = await _userManager.CreateAsync(user);

            if (result.Succeeded)
            {
                return Ok("User created successfully");
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        [HttpGet("/accounts")]
        public IActionResult GetAllAccounts()
        {
            var accounts = _dbContext.Accounts.ToList();
            return Ok(accounts);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("/accounts/me")]
        public IActionResult GetAccountByToken()
        {
            var Logtoken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (string.IsNullOrEmpty(Logtoken))
            {
                _logger.LogWarning("Token not found in the request.");
                HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;

            }
            var accountIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);

            if (accountIdClaim == null)
            {
                return BadRequest("Account ID not found in token");
            }

            if (!int.TryParse(accountIdClaim.Value, out int accountId))
            {
                return BadRequest("Invalid Account ID in token");
            }

            var account = _dbContext.Accounts.FirstOrDefault(a => a.AccountID == accountId);

            if (account == null)
            {
                return NotFound("Account not found");
            }

            // Return the account details as response
            return Ok(account);
        }



        [HttpPut("{accountId}")]
        public async Task<IActionResult> UpdateAccount(int accountId, UpdateAccountModel model)
        {
            var account = await _dbContext.Accounts.FindAsync(accountId);

            if (account == null)
            {
                return NotFound("Account not found");
            }
            // Cập nhật thông tin tài khoản từ dữ liệu mới

            account.Email = model.Email;
            account.PhoneNumber = model.PhoneNumber;
            account.Address = model.Address;
            // ... Cập nhật các thuộc tính khác

            await _dbContext.SaveChangesAsync();

            return Ok("Account updated successfully");
        }
        [HttpDelete("{accountId}")]
        public async Task<IActionResult> DeleteAccount(int accountId)
        {
            var account = await _dbContext.Accounts.FindAsync(accountId);

            if (account == null)
            {
                return NotFound("Account not found");
            }

            var cartsToRemove = _dbContext.Carts.Where(c => c.AccountID == accountId);
            var ordersToRemove = _dbContext.Orders.Where(o => cartsToRemove.Any(c => c.CartID == o.CartID));

            _dbContext.Orders.RemoveRange(ordersToRemove);
            _dbContext.Carts.RemoveRange(cartsToRemove);
            _dbContext.Accounts.Remove(account);

            await _dbContext.SaveChangesAsync();

            return Ok("Account, associated carts, and associated orders deleted successfully");
        }

        private string GenerateJwtToken(int accountId, string username, string roleName, string phoneNumber, string email, string address, bool status, byte[] key)
        {
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, accountId.ToString()),
        new Claim(ClaimTypes.Name, username),
        new Claim(ClaimTypes.Role, roleName),
        new Claim(ClaimTypes.MobilePhone, phoneNumber),
        new Claim(ClaimTypes.Email, email),
        new Claim("address", address),
        new Claim("status", status.ToString()) // Chuyển trạng thái (status) thành chuỗi và thêm vào claim
        // Thêm các claims khác nếu cần
    };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(24),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),

                Issuer = _config["JWT:Issuer"],
                Audience = _config["JWT:Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginModel model)
        {
            var user = await _dbContext.Accounts.FirstOrDefaultAsync(u => u.UserName == model.UserName);

            if (user == null || user.Password != model.Password)
            {
                return Unauthorized("Invalid username or password");
            }

            var token = GenerateJwtToken(user.AccountID, model.UserName, user.RoleName, user.PhoneNumber, user.Email, user.Address, user.Status, _key);

            var loginResponse = new LoginResponse
            {
                Token = token,
                UserData = new Account
                {
                    AccountID = user.AccountID,
                    UserName = user.UserName,
                    RoleName = user.RoleName,
                    PhoneNumber = user.PhoneNumber,
                    Email = user.Email,
                    Address = user.Address,
                    Status = user.Status,
                    // Gán các giá trị cần thiết khác từ đối tượng user
                }
            };

            // Lưu LoginResponse vào Cookie
            Response.Cookies.Append("LoginResponse", JsonConvert.SerializeObject(loginResponse), new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTimeOffset.UtcNow.AddDays(1)
            });

            return Ok(loginResponse);
        }

    }

}
