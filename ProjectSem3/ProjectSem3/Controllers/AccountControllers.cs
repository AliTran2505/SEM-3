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

namespace ProjectSem3.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowOrigin")]
    //public static class JwtExtensions
    //{
    //    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, string secretKey)
    //    {
    //        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    //            .AddJwtBearer(options =>
    //            {
    //                options.TokenValidationParameters = new TokenValidationParameters
    //                {
    //                    ValidateIssuer = false,
    //                    ValidateAudience = false,
    //                    ValidateLifetime = true,
    //                    ValidateIssuerSigningKey = true,
    //                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    //                };

    //                options.Events = new JwtBearerEvents
    //                {
    //                    OnTokenValidated = context =>
    //                    {
    //                        // Lấy thông tin từ token và set AccountID vào ClaimsPrincipal
    //                        if (context.SecurityToken is JwtSecurityToken jwtSecurityToken)
    //                        {
    //                            if (jwtSecurityToken.Payload.TryGetValue("AccountID", out var accountId))
    //                            {
    //                                var claims = new List<Claim>
    //                                {
    //                                new Claim("AccountID", accountId.ToString())
    //                                };

    //                                var identity = new ClaimsIdentity(claims, context.Principal.Identity.ToString());
    //                                context.Principal = new ClaimsPrincipal(identity);
    //                            }
    //                        }

    //                        return Task.CompletedTask;
    //                    }
    //                };
    //            });

    //        return services;
    //    }
    //}
    public class AccountControllers : ControllerBase
    {
        private readonly UserManager<Account> _userManager;
        private readonly SignInManager<Account> _signInManager;
        private readonly byte[] _key;
        private readonly ShopDbContext _dbContext;
        private IConfiguration _config;

        private IHttpContextAccessor _httpContextAccessor;




        public AccountControllers(UserManager<Account> userManager, IHttpContextAccessor httpContextAccessor, SignInManager<Account> signInManager, ShopDbContext dbContext)
        {
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
            var x = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier); //get in the constructor
            _userManager = userManager;
            _signInManager = signInManager;
            _key = Encoding.UTF8.GetBytes("HieuAliCuongHieuAliCuongHieuAliCuong"); // Replace with your secret key
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

        //[HttpGet("accountID")]
        //public IActionResult GetAccountID()
        //{
        //    var accountId = User.FindFirst("AccountID")?.Value;

        //    if (accountId != null)
        //    {
        //        return Ok($"AccountID: {accountId}");
        //    }

        //    return BadRequest("AccountID not found");
        //}

        //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("/accounts/me")]
        public async Task<IActionResult> GetAccountByToken()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var check = HttpContext.User.Identity;
            var x = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier); //get in the constructor
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

            _dbContext.Accounts.Remove(account);
            await _dbContext.SaveChangesAsync();

            return Ok("Account deleted successfully");
        }

        private string GenerateJwtToken(int accountId, string username, string roleName, string phoneNumber, string email, string address, byte[] key)
        {

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, accountId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, roleName),
                new Claim("phone", phoneNumber), // Thay đổi key của claim phone
                new Claim(ClaimTypes.Email, email),
                new Claim("address", address)
                // Các claims khác nếu cần
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
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

            var token = GenerateJwtToken(user.AccountID, model.UserName, user.RoleName, user.PhoneNumber, user.Email, user.Address, _key);

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
                    // Gán các giá trị cần thiết khác từ đối tượng user
                }
            };
            var my = HttpContext.User.Claims.ToList();

            return Ok(loginResponse);
        }



    }

}
