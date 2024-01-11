using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectSem3.Model;

namespace ProjectSem3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartsController : ControllerBase
    {
        private readonly ShopDbContext _dbcontext;

        public CartsController(ShopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        // GET: api/Carts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cart>>> GetCarts()
        {
          if (_dbcontext.Carts == null)
          {
              return NotFound();
          }
            return await _dbcontext.Carts.ToListAsync();
        }

        // GET: api/Carts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Cart>> GetCart(int id)
        {
          if (_dbcontext.Carts == null)
          {
              return NotFound();
          }
            var cart = await _dbcontext.Carts.FindAsync(id);

            if (cart == null)
            {
                return NotFound();
            }

            return cart;
        }

        // Get products by accountid
        [HttpGet("getitems/{accountId}")]
        public async Task<IActionResult> GetCartItems(int accountId)
        {
            try
            {
                var cartItems = await _dbcontext.Carts
                    .Where(c => c.AccountID == accountId)
                    .ToListAsync();

                return Ok(cartItems);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT: api/Carts/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCart(int id, Cart updatedCartItem)
        {   
            try
            {
                var cartItem = await _dbcontext.Carts.FindAsync(id);
                if (cartItem == null)
                {
                    return NotFound();
                }

                // Cập nhật thông tin mục trong giỏ hàng
                cartItem.ProductID = updatedCartItem.ProductID;
                cartItem.AccountID = updatedCartItem.AccountID;
                cartItem.Quantity = updatedCartItem.Quantity;
                cartItem.CreateAt = updatedCartItem.CreateAt;
                cartItem.LastUpdateAt = updatedCartItem.LastUpdateAt;

                await _dbcontext.SaveChangesAsync();
                return Ok(cartItem);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        // POST: api/Carts them 1 san pham
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("AddSingleProductToCart")]
        [Consumes("application/json")]
        public async Task<ActionResult<Cart>> AddSingleProductToCart(Cart cart)
        {
            try
            {
                var accountIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);

                if (accountIdClaim == null || !int.TryParse(accountIdClaim.Value, out int accountId))
                {
                    return Unauthorized(new { message = "Invalid or missing AccountID in token", statusCode = 401 });
                }

                if (_dbcontext.Carts == null)
                {
                    return Problem("Entity set 'ShopDbdbcontext.Carts' is null.");
                }

                // Assign AccountID from token to cart
                cart.AccountID = accountId;

                var existingCartItem = _dbcontext.Carts.FirstOrDefault(c => c.AccountID == accountId && c.ProductID == cart.ProductID);

                if (existingCartItem != null)
                {
                    // Increment quantity if the product already exists in the cart
                    existingCartItem.Quantity += 1;
                }
                else
                {
                    // Otherwise, add the product to the cart with quantity 1
                    cart.Quantity = 1;
                    _dbcontext.Carts.Add(cart);
                }

                await _dbcontext.SaveChangesAsync();

                return CreatedAtAction("GetCart", new { id = cart.CartID }, cart);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        // them nhieu san pham cung loai vao cart
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("AddMultipleProductsToCart")]
        [Consumes("application/json")]
        public async Task<ActionResult<Cart>> AddMultipleProductsToCart(Cart cart)
        {
            try
            {
                var accountIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);

                if (accountIdClaim == null || !int.TryParse(accountIdClaim.Value, out int accountId))
                {
                    return Unauthorized(new { message = "Invalid or missing AccountID in token", statusCode = 401 });
                }

                if (_dbcontext.Carts == null)
                {
                    return Problem("Entity set 'ShopDbdbcontext.Carts' is null.");
                }

                // Assign AccountID from token to cart
                cart.AccountID = accountId;

                var existingCartItem = _dbcontext.Carts.FirstOrDefault(c => c.AccountID == accountId && c.ProductID == cart.ProductID);

                if (existingCartItem != null)
                {
                    // Increment quantity based on the incoming quantity in the request
                    existingCartItem.Quantity += cart.Quantity;
                }
                else
                {
                    // Otherwise, add the product to the cart with the specified quantity
                    _dbcontext.Carts.Add(cart);
                }

                await _dbcontext.SaveChangesAsync();

                return CreatedAtAction("GetCart", new { id = cart.CartID }, cart);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }



        // DELETE: api/Carts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCart(int id)
        {
            if (_dbcontext.Carts == null)
            {
                return NotFound();
            }
            var cart = await _dbcontext.Carts.FindAsync(id);
            if (cart == null)
            {
                return NotFound();
            }

            _dbcontext.Carts.Remove(cart);
            await _dbcontext.SaveChangesAsync();

            return NoContent();
        }

        private bool CartExists(int id)
        {
            return (_dbcontext.Carts?.Any(e => e.CartID == id)).GetValueOrDefault();
        }
    }
}
