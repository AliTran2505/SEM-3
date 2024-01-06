using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly ShopDbContext _context;

        public CartsController(ShopDbContext context)
        {
            _context = context;
        }

        // GET: api/Carts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cart>>> GetCarts()
        {
          if (_context.Carts == null)
          {
              return NotFound();
          }
            return await _context.Carts.ToListAsync();
        }

        // GET: api/Carts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Cart>> GetCart(int id)
        {
          if (_context.Carts == null)
          {
              return NotFound();
          }
            var cart = await _context.Carts.FindAsync(id);

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
                var cartItems = await _context.Carts
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
                var cartItem = await _context.Carts.FindAsync(id);
                if (cartItem == null)
                {
                    return NotFound();
                }

                // Cập nhật thông tin mục trong giỏ hàng
                cartItem.ProductID = updatedCartItem.ProductID;
                cartItem.AccountID = updatedCartItem.AccountID;
                cartItem.CreateAt = updatedCartItem.CreateAt;
                cartItem.LastUpdateAt = updatedCartItem.LastUpdateAt;

                await _context.SaveChangesAsync();
                return Ok(cartItem);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        // POST: api/Carts
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Consumes("application/json")]
        public async Task<ActionResult<Cart>> PostCart(Cart cart)
        {
          if (_context.Carts == null)
          {
              return Problem("Entity set 'ShopDbContext.Carts'  is null.");
          }
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCart", new { id = cart.CartID }, cart);
        }

        // DELETE: api/Carts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCart(int id)
        {
            if (_context.Carts == null)
            {
                return NotFound();
            }
            var cart = await _context.Carts.FindAsync(id);
            if (cart == null)
            {
                return NotFound();
            }

            _context.Carts.Remove(cart);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CartExists(int id)
        {
            return (_context.Carts?.Any(e => e.CartID == id)).GetValueOrDefault();
        }
    }
}
