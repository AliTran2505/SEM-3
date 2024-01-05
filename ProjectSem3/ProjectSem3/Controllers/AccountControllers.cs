using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProjectSem3.Model;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace ProjectSem3.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class AccountControllers : ControllerBase
    {
        private readonly UserManager<Account> _userManager;
        private readonly SignInManager<Account> _signInManager;
        private readonly byte[] _key;
        private readonly ShopDbContext _dbContext;


        public AccountControllers(UserManager<Account> userManager, SignInManager<Account> signInManager, ShopDbContext dbContext)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _signInManager = signInManager;
            _key = Encoding.UTF8.GetBytes("HieuAliCuongHieuAliCuongHieuAliCuong"); // Replace with your secret key
        }

        [HttpPost("/register")]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            var user = new Account { UserName = model.UserName, Password = model.Password, RoleName = model.RoleName,  FirstName = model.FirstName, MiddleName = model.MiddleName , LastName = model.LastName,BirthDay = model.BirthDay, Email = model.Email, PhoneNumber = model.PhoneNumber, Address = model.Address };

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
        [HttpPut("{accountId}")]
        public async Task<IActionResult> UpdateAccount(int accountId, UpdateAccountModel model)
        {
            var account = await _dbContext.Accounts.FindAsync(accountId);

            if (account == null)
            {
                return NotFound("Account not found");
            }

            // Cập nhật thông tin tài khoản từ dữ liệu mới
            account.FirstName = model.FirstName;
            account.MiddleName = model.MiddleName;
            account.LastName = model.LastName;
            account.BirthDay = model.BirthDay;
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

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginModel model)
        {
            var user = await _dbContext.Accounts.FirstOrDefaultAsync(u => u.UserName == model.UserName);

            if (user == null || user.Password != model.Password)
            {
                return Unauthorized("Invalid username or password");
            }

            var token = GenerateJwtToken(user.AccountID, model.UserName, _key);
            return Ok(new { Token = token });
        }


        private string GenerateJwtToken(int accountId, string username, byte[] key)
        {
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, accountId.ToString()), // Thêm AccountID vào claim
        new Claim(ClaimTypes.Name, username)
        // Các claims khác nếu cần
    };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

}
