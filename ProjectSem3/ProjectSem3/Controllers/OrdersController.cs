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
using System.Text.Json;
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
                    .Include(o => o.OrderItem)
                    .Include(o => o.Account) // Kèm theo thông tin tài khoản
                    .ToListAsync();

                // Không cần parse lại SerializedProduct, trả về chuỗi JSON đã lưu
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
                    .Include(o => o.OrderItem) // Bao gồm OrderItems nhưng không cần ThenInclude để lấy Product chi tiết
                    .Include(o => o.Account) // Kèm theo thông tin tài khoản
                    .Where(o => o.OrderID == id)
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    return NotFound();
                }

                // Trả về order
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
                    .Include(o => o.OrderItem) // Bao gồm OrderItems nhưng không cần ThenInclude để lấy Product chi tiết
                    .Include(o => o.Account) // Kèm theo thông tin tài khoản
                    .Where(o => o.AccountID == accountId)
                    .ToListAsync();

                // Trả về orders
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
                    .Include(c => c.Product) // Tải sản phẩm kèm theo
                    .Where(c => c.AccountID == accountId && request.CartItemIds.Contains(c.CartID))
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
                    Status = OrderStatus.OrderPlacedSuccessfully, // Đặt trạng thái đơn hàng là "Đặt hàng thành công"
                    AccountID = accountId,
                    CreateAt = DateTime.Now,
                    Account = account // Gán thông tin tài khoản vào đơn hàng
                };

                // Tính tổng giá trị của đơn hàng
                float price = 0;

                // Tạo đối tượng OrderItem cho mỗi sản phẩm trong giỏ hàng
                foreach (var cartItem in cartItems)
                {
                    var product = cartItem.Product; // Lấy thông tin sản phẩm từ giỏ hàng

                    // Serialize sản phẩm thành chuỗi
                    var serializedProduct = JsonSerializer.Serialize(product);

                    var newOrderItem = new OrderItem
                    {
                        Quantity = cartItem.Quantity,
                        SerializedProduct = serializedProduct // Lưu chuỗi sản phẩm đã serialize
                    };

                    // Cộng dồn giá trị của OrderItem vào tổng giá trị của đơn hàng
                    price += newOrderItem.Quantity * cartItem.Product.Price; // Sử dụng giá của sản phẩm từ đối tượng

                    // Thêm OrderItem vào danh sách OrderItems của Order
                    order.OrderItem.Add(newOrderItem);
                }

                // Gán Price cho đơn hàng
                order.Price = price;

                // Thêm Order vào database
                // Thêm Order vào database
                _dbcontext.Orders.Add(order);
                _dbcontext.SaveChanges(); // Lưu Order trước để lấy OrderID

                // Cập nhật OrderID cho các OrderItem sau khi lưu Order
                foreach (var orderItem in order.OrderItem)
                {
                    orderItem.OrderID = order.OrderID; // Cập nhật OrderID cho từng OrderItem
                                                       // Không cần gán giá trị thủ công cho cột ID của OrderItem nếu nó là auto-increment
                }

                // Lưu các OrderItem vào cơ sở dữ liệu
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



        [HttpPut("UpdateOrderStatus/{orderId}/{status}")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, int status)
        {
            try
            {
                // Lấy đơn hàng từ database theo OrderID
                var order = await _dbcontext.Orders.FindAsync(orderId);

                if (order == null)
                {
                    return NotFound("Order not found");
                }

                // Kiểm tra xem status có nằm trong các giá trị hợp lệ của enum không
                if (!Enum.IsDefined(typeof(OrderStatus), status))
                {
                    return BadRequest("Invalid status value.");
                }

                // Cập nhật trạng thái của đơn hàng
                order.Status = (OrderStatus)status; // Chuyển đổi giá trị int sang enum

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

                // Tính toán ngày bắt đầu và kết thúc của năm
                var startDate = new DateTime(year, 1, 1);
                var endDate = new DateTime(year, 12, 31, 23, 59, 59);

                // Lấy giá trị int của enum OrderStatus.OrderPlacedSuccessfully
                var orderPlacedStatus = OrderStatus.OrderReceivedSuccessfully;

                // Lấy các đơn hàng có trạng thái OrderPlacedSuccessfully trong khoảng thời gian
                var orders = await _dbcontext.Orders
                    .Where(o => o.CreateAt >= startDate && o.CreateAt <= endDate && o.Status == orderPlacedStatus)
                    .ToListAsync();

                // Khởi tạo mảng kết quả với 12 đối tượng, mỗi đối tượng đại diện cho một tháng
                var result = Enumerable.Range(1, 12)
                    .Select(month => new { label = month, price = 0f })
                    .ToList();

                // Cộng dồn giá trị Price cho từng tháng
                foreach (var order in orders)
                {
                    var month = order.CreateAt.Month;
                    result[month - 1] = new
                    {
                        label = month,
                        price = result[month - 1].price + order.Price
                    };
                }

                // Trả về kết quả
                return Ok(result);
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
            // Kiểm tra xem Orders có tồn tại không
            if (_dbcontext.Orders == null)
            {
                return NotFound();
            }

            // Tìm order theo ID
            var order = await _dbcontext.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            // Lấy danh sách OrderItems có OrderID tương ứng
            var orderItems = await _dbcontext.OrderItems
                .Where(oi => oi.OrderID == id) // Sử dụng id đã truyền vào
                .ToListAsync();

            // Xóa tất cả OrderItems liên quan
            if (orderItems.Any())
            {
                _dbcontext.OrderItems.RemoveRange(orderItems);
            }

            // Xóa order
            _dbcontext.Orders.Remove(order);
            await _dbcontext.SaveChangesAsync();

            return NoContent();
        }

    }
}
