using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Build.Evaluation;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ProjectSem3.Model;

namespace ProjectSem3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly ShopDbContext _dbcontext;

        public OrdersController(ShopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            try
            {
                // Lấy tất cả đơn đặt hàng từ database với OrderItems và Account được kèm theo
                var orders = await _dbcontext.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product) // Bao gồm thông tin sản phẩm
                        .ThenInclude(p => p.Category) // Bao gồm thông tin danh mục
                    .Include(o => o.Account) // Kèm theo thông tin tài khoản
                    .ToListAsync();

                return orders;
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrderById(int id)
        {
            try
            {
                // Lấy thông tin đơn đặt hàng từ database theo OrderID với OrderItems và Account được kèm theo
                var order = await _dbcontext.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product) // Bao gồm thông tin sản phẩm
                        .ThenInclude(p => p.Category) // Bao gồm thông tin danh mục
                    .Include(o => o.Account) // Kèm theo thông tin tài khoản
                    .Where(o => o.OrderID == id)
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    return NotFound();
                }

                return order;
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet("ByAccount/{accountId}")]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrdersByAccount(int accountId)
        {
            try
            {
                // Lấy tất cả đơn đặt hàng từ database theo AccountID với OrderItems và Account được kèm theo
                var orders = await _dbcontext.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product) // Bao gồm thông tin sản phẩm
                        .ThenInclude(p => p.Category) // Bao gồm thông tin danh mục
                    .Include(o => o.Account) // Kèm theo thông tin tài khoản
                    .Where(o => o.AccountID == accountId)
                    .ToListAsync();

                return orders;
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }



        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("PlaceOrder/{accountId}")]
        [Consumes("application/json")]
        public IActionResult PlaceOrder(int accountId, [FromBody] List<int> cartIds)
        {
            try
            {
                if (accountId == 0 || cartIds == null || !cartIds.Any())
                {
                    return BadRequest("Invalid request data");
                }

                // Lấy thông tin giỏ hàng từ database
                var cartItems = _dbcontext.Carts
                    .Where(c => c.AccountID == accountId && cartIds.Contains(c.CartID))
                    .Include(c => c.Product) // Bao gồm thông tin sản phẩm
                    .ToList();

                if (cartItems == null || cartItems.Count == 0)
                {
                    return BadRequest("No items in the cart.");
                }

                // Lấy thông tin tài khoản
                var account = _dbcontext.Accounts.Find(accountId);

                // Tạo đối tượng Order
                var order = new Order
                {
                    Status = true,
                    AccountID = accountId,
                    CreateAt = DateTime.Now,
                    Account = account // Gán thông tin tài khoản vào đơn hàng
                };

                // Thêm Order vào database
                _dbcontext.Orders.Add(order);
                _dbcontext.SaveChanges();

                var orderId = order.OrderID;

                // Tạo đối tượng OrderItem cho mỗi sản phẩm trong giỏ hàng
                foreach (var cartItem in cartItems)
                {
                    var newOrderItem = new OrderItem
                    {
                        OrderID = orderId,
                        ProductID = cartItem.ProductID,
                        Quantity = cartItem.Quantity,
                        Product = cartItem.Product // Gán thông tin sản phẩm từ CartItem
                    };

                    // Thêm OrderItem vào danh sách OrderItems của Order
                    order.OrderItems.Add(newOrderItem);

                    // Lưu OrderItem vào cơ sở dữ liệu
                    _dbcontext.OrderItems.Add(newOrderItem);
                }

                // Lưu thay đổi vào cơ sở dữ liệu
                _dbcontext.SaveChanges();

                // Xóa các sản phẩm đã được đặt hàng khỏi giỏ hàng
                _dbcontext.Carts.RemoveRange(cartItems);
                _dbcontext.SaveChanges();

                return Ok("Order placed successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Class để bind dữ liệu từ body request
        public class PlaceOrderRequest
        {
            public List<int> CartItemIds { get; set; }
        }






        // DELETE: api/Orders/5
        // DELETE: api/Orders/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            if (_dbcontext.Orders == null)
            {
                return NotFound();
            }
            var order = await _dbcontext.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            _dbcontext.Orders.Remove(order);
            await _dbcontext.SaveChangesAsync();

            return NoContent();
        }


        private bool OrderExists(int id)
        {
            return (_dbcontext.Orders?.Any(e => e.OrderID == id)).GetValueOrDefault();
        }
    }
}
