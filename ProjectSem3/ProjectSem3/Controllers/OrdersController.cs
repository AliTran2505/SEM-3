using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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


        [HttpGet("ByAccount")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrdersByAccount()
        {
            try
            {
                // Lấy thông tin AccountID từ token
                var accountIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);

                if (accountIdClaim == null || !int.TryParse(accountIdClaim.Value, out int accountId))
                {
                    return Unauthorized(new { message = "Invalid or missing AccountID in token", statusCode = 401 });
                }

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




        [HttpPost("PlaceOrder")]
        [Consumes("application/json")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult PlaceOrder([FromBody] PlaceOrderRequest request)
        {
            try
            {
                // Lấy thông tin AccountID từ token
                var accountIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);

                if (accountIdClaim == null || !int.TryParse(accountIdClaim.Value, out int accountId))
                {
                    return Unauthorized(new { message = "Invalid or missing AccountID in token", statusCode = 401 });
                }

                if (accountId == 0 || request.CartItemIds == null || !request.CartItemIds.Any())
                {
                    return BadRequest("Invalid request data");
                }

                // Lấy thông tin giỏ hàng từ database
                var cartItems = _dbcontext.Carts
                    .Where(c => c.AccountID == accountId && request.CartItemIds.Contains(c.CartID))
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
                    Status = false,
                    AccountID = accountId,
                    CreateAt = DateTime.Now,
                    Account = account // Gán thông tin tài khoản vào đơn hàng
                };

                // Tính tổng giá trị của đơn hàng
                float orderPrice = 0;

                // Tạo đối tượng OrderItem cho mỗi sản phẩm trong giỏ hàng
                foreach (var cartItem in cartItems)
                {
                    var newOrderItem = new OrderItem
                    {
                        OrderID = order.OrderID,
                        ProductID = cartItem.ProductID,
                        Quantity = cartItem.Quantity,
                        Product = cartItem.Product
                    };

                    // Cộng dồn giá trị của OrderItem vào tổng giá trị của đơn hàng
                    orderPrice += newOrderItem.Quantity * cartItem.Product.Price; // Giả sử bạn có thuộc tính Price trong Product

                    // Thêm OrderItem vào danh sách OrderItems của Order
                    order.OrderItems.Add(newOrderItem);

                    // Lưu OrderItem vào cơ sở dữ liệu
                    _dbcontext.OrderItems.Add(newOrderItem);
                }

                // Gán OrderPrice cho đơn hàng
                order.OrderPrice = orderPrice; // Giả sử bạn có thuộc tính OrderPrice trong Order

                // Thêm Order vào database
                _dbcontext.Orders.Add(order);
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



        [HttpPut("UpdateOrderStatus/{orderId}")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId)
        {
            try
            {
                // Lấy đơn hàng từ database theo OrderID
                var order = await _dbcontext.Orders.FindAsync(orderId);

                if (order == null)
                {
                    return NotFound("Order not found");
                }

                // Cập nhật trạng thái của đơn hàng
                order.Status = !order.Status;

                // Lưu thay đổi vào cơ sở dữ liệu
                await _dbcontext.SaveChangesAsync();

                return Ok($"Order with OrderID {orderId} has been updated. Status is now {order.Status}.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet("GetOrdersByYear/{year}")]
        public async Task<IActionResult> GetOrdersByYear(int year)
        {
            try
            {
                // Kiểm tra năm hợp lệ
                if (year < 1)
                {
                    return BadRequest("Invalid year parameter.");
                }

                // Tính toán ngày bắt đầu và ngày kết thúc của năm
                var startDate = new DateTime(year, 1, 1);
                var endDate = new DateTime(year, 12, 31, 23, 59, 59);

                // Lấy các đơn hàng được tạo trong khoảng thời gian
                var orders = await _dbcontext.Orders
                    .Where(o => o.CreateAt >= startDate && o.CreateAt <= endDate)
                    .ToListAsync();

                // Kiểm tra nếu không có đơn hàng nào
                if (orders == null || !orders.Any())
                {
                    return NotFound("No orders found for the specified year.");
                }

                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
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
