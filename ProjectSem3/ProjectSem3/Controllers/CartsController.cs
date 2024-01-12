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
        public async Task<ActionResult<IEnumerable<object>>> GetCarts()
        {
            if (_dbcontext.Carts == null)
            {
                return NotFound();
            }

            var carts = await _dbcontext.Carts
                .Include(c => c.Product) // Bao gồm thông tin sản phẩm
                .Select(cart => new
                {
                    CartID = cart.CartID,
                    ProductID = cart.ProductID,
                    Quantity = cart.Quantity,
                    Product = new
                    {
                        ProductName = cart.Product.ProductName,
                        Price = cart.Product.Price,
                        Description = cart.Product.Description,
                        Image = cart.Product.Image,
                        CategoryID = cart.Product.CategoryID,
                        Category = new
                        {
                            CategoryName = cart.Product.Category.CategoryName,
                            Description = cart.Product.Category.Description,
                        }
                        // Thêm các thông tin khác của Product nếu cần
                    }
                })
                .ToListAsync();

            return Ok(carts);
        }

        // GET: api/Carts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetCart(int id)
        {
            try
            {
                var cart = await _dbcontext.Carts
                    .Where(c => c.CartID == id)
                    .Include(c => c.Product) // Bao gồm thông tin sản phẩm
                    .Select(cart => new
                    {
                        CartID = cart.CartID,
                        ProductID = cart.ProductID,
                        Quantity = cart.Quantity,
                        Image = cart.Product.Image,
                        Product = new
                        {
                            
                            ProductName = cart.Product.ProductName,
                            Price = cart.Product.Price,
                            Description = cart.Product.Description,
                            CategoryID = cart.Product.CategoryID,
                            Category = new
                            {
                                CategoryName = cart.Product.Category.CategoryName,
                                Description = cart.Product.Category.Description,
                            }
                            // Thêm các thông tin khác của Product nếu cần
                        }
                    })
                    .FirstOrDefaultAsync();

                if (cart == null)
                {
                    return NotFound();
                }

                return Ok(cart);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ... Các API khác không thay đổi


        // PUT: api/Carts/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        // PUT: api/Carts/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCart(int id, [FromQuery(Name = "type")] string type)
        {
            try
            {
                var cartItem = await _dbcontext.Carts
                    .Where(c => c.CartID == id)
                    .Include(c => c.Product) // Bao gồm thông tin sản phẩm
                    .FirstOrDefaultAsync();

                if (cartItem == null)
                {
                    return NotFound();
                }

                // Kiểm tra giá trị type và cập nhật số lượng sản phẩm
                if (type.ToLower() == "plus")
                {
                    // Nếu type là "plus", tăng 1 đơn vị
                    cartItem.Quantity += 1;
                }
                else if (type.ToLower() == "minus")
                {
                    // Nếu type là "minus", giảm 1 đơn vị
                    cartItem.Quantity -= 1;

                    // Kiểm tra số lượng để xem có cần xóa mục không
                    if (cartItem.Quantity <= 0)
                    {
                        _dbcontext.Carts.Remove(cartItem);
                        await _dbcontext.SaveChangesAsync();
                        return NoContent(); // Trả về 204 No Content khi xóa thành công
                    }
                }
                else
                {
                    return BadRequest("Invalid type. Type must be 'plus' or 'minus'.");
                }

                await _dbcontext.SaveChangesAsync();
                return Ok(cartItem);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // them nhieu san pham cung loai vao cart
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("AddProductToCart/{productId}/{quantity}")]
        public async Task<ActionResult<Cart>> AddProductToCart(int productId, int quantity)
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

                // Kiểm tra nếu có sản phẩm có ProductID tương ứng
                var existingProduct = await _dbcontext.Products.FindAsync(productId);

                if (existingProduct != null)
                {
                    // Kiểm tra nếu đã có cartItem tương ứng
                    var existingCartItem = await _dbcontext.Carts
                        .Where(c => c.AccountID == accountId && c.ProductID == productId)
                        .Include(c => c.Product) // Bao gồm thông tin sản phẩm
                        .FirstOrDefaultAsync();

                    if (existingCartItem != null)
                    {
                        // Increment quantity based on the incoming quantity in the request
                        existingCartItem.Quantity += quantity;

                        // Update total quantity in the database
                        await UpdateTotalQuantity(existingCartItem.ProductID, accountId);

                        // Lấy lại thông tin của cart sau khi cập nhật
                        existingCartItem = await _dbcontext.Carts
                            .Where(c => c.AccountID == accountId && c.ProductID == productId)
                            .Include(c => c.Product) // Bao gồm thông tin sản phẩm
                            .FirstOrDefaultAsync();

                        return existingCartItem;
                    }
                    else
                    {
                        // Tạo đối tượng Cart và gán thông tin
                        var cart = new Cart
                        {
                            AccountID = accountId,
                            ProductID = existingProduct.ProductID,
                            Quantity = quantity,
                        };

                        // Bao gồm thông tin sản phẩm
                        cart.Product = existingProduct;

                        // Otherwise, add the product to the cart with the specified quantity
                        _dbcontext.Carts.Add(cart);
                        await _dbcontext.SaveChangesAsync();

                        // Lấy lại thông tin của cart sau khi thêm mới
                        existingCartItem = await _dbcontext.Carts
                            .Where(c => c.AccountID == accountId && c.ProductID == productId)
                            .Include(c => c.Product) // Bao gồm thông tin sản phẩm
                            .FirstOrDefaultAsync();

                        return existingCartItem;
                    }
                }
                else
                {
                    return BadRequest("Invalid ProductID");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        private async Task UpdateTotalQuantity(int productId, int accountId)
        {
            // Lấy tất cả các mục trong giỏ hàng với ProductID và AccountID tương ứng
            var cartItems = await _dbcontext.Carts.Where(c => c.ProductID == productId && c.AccountID == accountId).ToListAsync();

            // Tính toán và cập nhật tổng quantity
            var totalQuantity = cartItems.Sum(c => c.Quantity);

            foreach (var cartItem in cartItems)
            {
                cartItem.Quantity = totalQuantity;
            }

            await _dbcontext.SaveChangesAsync();
        }

        // DELETE: api/Carts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCart(int id)
        {
            if (_dbcontext.Carts == null)
            {
                return NotFound();
            }

            var cart = await _dbcontext.Carts
                .Where(c => c.CartID == id)
                .Include(c => c.Product) // Bao gồm thông tin sản phẩm
                .FirstOrDefaultAsync();

            if (cart == null)
            {
                return NotFound();
            }

            _dbcontext.Carts.Remove(cart);
            await _dbcontext.SaveChangesAsync();

            return NoContent();
        }

    }
}
