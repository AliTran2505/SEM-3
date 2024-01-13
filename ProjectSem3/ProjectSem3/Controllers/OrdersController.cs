using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<ActionResult<IEnumerable<FullOrderDto>>> GetOrders()
        {
            try
            {
                // Lấy tất cả đơn đặt hàng từ database với OrderItems được kèm theo
                var orders = await _dbcontext.Orders
                    .Include(o => o.OrderItems)
                    .Include(o => o.Account)  // Kèm theo thông tin tài khoản
                    .ToListAsync();

                // Chuyển đổi đối tượng Order sang đối tượng DTO để trả về
                var orderDtos = orders.Select(order => new FullOrderDto
                {
                    OrderID = order.OrderID,
                    Status = order.Status,
                    CreateAt = order.CreateAt,
                    AccountID = order.AccountID,
                    LastUpdateAt = order.LastUpdateAt,
                    OrderItems = order.OrderItems.Select(oi => new OrderDto
                    {
                        OrderID = oi.OrderID,
                        ProductID = oi.ProductID,
                        ProductName = oi.ProductName,
                        ProductPrice = oi.ProductPrice,
                        Quantity = oi.Quantity,
                        Image = oi.Image,
                    }).ToList(),
                    Account = new AccountDto
                    {
                        UserName = order.Account.UserName,
                        Phone = order.Account.PhoneNumber,
                        Email = order.Account.Email,
                        Adress = order.Account.Address
                        // Thêm các trường thông tin tài khoản khác (nếu cần)
                    }
                }).ToList();

                return orderDtos;
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<FullOrderDto>> GetOrderById(int id)
        {
            try
            {
                // Lấy thông tin đơn đặt hàng từ database theo OrderID với OrderItems được kèm theo
                var order = await _dbcontext.Orders
                    .Include(o => o.OrderItems)
                    .Include(o => o.Account)  // Kèm theo thông tin tài khoản
                    .Where(o => o.OrderID == id)
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    return NotFound();
                }

                // Chuyển đổi đối tượng Order sang đối tượng DTO để trả về
                var orderDto = new FullOrderDto
                {
                    OrderID = order.OrderID,
                    Status = order.Status,
                    CreateAt = order.CreateAt,
                    AccountID = order.AccountID,
                    LastUpdateAt = order.LastUpdateAt,
                    OrderItems = order.OrderItems.Select(oi => new OrderDto
                    {
                        OrderID = oi.OrderID,
                        ProductID = oi.ProductID,
                        ProductName = oi.ProductName,
                        ProductPrice = oi.ProductPrice,
                        Quantity = oi.Quantity,
                        Image = oi.Image
                    }).ToList(),
                    Account = new AccountDto
                    {
                        UserName = order.Account.UserName,
                        Phone = order.Account.PhoneNumber,
                        Email = order.Account.Email,
                        Adress = order.Account.Address
                        // Thêm các trường thông tin tài khoản khác (nếu cần)
                    }
                };

                return orderDto;
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("ByAccount/{accountId}")]
        public async Task<ActionResult<IEnumerable<FullOrderDto>>> GetOrdersByAccount(int accountId)
        {
            try
            {
                // Lấy tất cả đơn đặt hàng từ database theo AccountID với OrderItems được kèm theo
                var orders = await _dbcontext.Orders
                    .Include(o => o.OrderItems)
                    .Include(o => o.Account)  // Kèm theo thông tin tài khoản
                    .Where(o => o.AccountID == accountId)
                    .ToListAsync();

                // Chuyển đổi đối tượng Order sang đối tượng DTO để trả về
                var orderDtos = orders.Select(order => new FullOrderDto
                {
                    OrderID = order.OrderID,
                    Status = order.Status,
                    CreateAt = order.CreateAt,
                    AccountID = order.AccountID,
                    LastUpdateAt = order.LastUpdateAt,
                    OrderItems = order.OrderItems.Select(oi => new OrderDto
                    {
                        OrderID = oi.OrderID,
                        ProductID = oi.ProductID,
                        ProductName = oi.ProductName,
                        ProductPrice = oi.ProductPrice,
                        Quantity = oi.Quantity,
                        Image = oi.Image
                    }).ToList(),
                    Account = new AccountDto
                    {
                        UserName = order.Account.UserName,
                        Phone = order.Account.PhoneNumber,
                        Email = order.Account.Email,
                        Adress = order.Account.Address
                        // Thêm các trường thông tin tài khoản khác (nếu cần)
                    }
                }).ToList();

                return orderDtos;
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("PlaceOrder/{accountId}")]
        [Consumes("application/json")]
        public IActionResult PlaceOrder(int accountId, [FromBody] List<int> cartItemIds)
        {
            try
            {
                // Lấy thông tin giỏ hàng từ database
                var cartItems = _dbcontext.Carts
                    .Where(c => c.AccountID == accountId && cartItemIds.Contains(c.CartID))
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

                // Duyệt qua từng CartItem và tạo OrderItem tương ứng
                foreach (var cartItem in cartItems)
                {
                    var product = cartItem.Product;

                    if (product != null)
                    {
                        var newOrderItem = new OrderItem
                        {
                            OrderID = orderId,
                            ProductID = cartItem.ProductID,
                            ProductName = product.ProductName,
                            ProductPrice = product.Price,
                            Quantity = cartItem.Quantity,
                            Image = product.Image
                        };

                        // Thêm OrderItem vào danh sách OrderItems của Order
                        order.OrderItems.Add(newOrderItem);
                    }
                }

                // Lưu danh sách OrderItems vào cơ sở dữ liệu
                _dbcontext.SaveChanges();

                // Xóa toàn bộ thông tin các Cart có AccountID khớp với AccountID được truyền vào
                _dbcontext.Carts.RemoveRange(cartItems);
                _dbcontext.SaveChanges();

                // Tạo đối tượng OrderDto để trả về
                var orderDto = new FullOrderDto
                {
                    OrderID = order.OrderID,
                    Status = order.Status,
                    CreateAt = order.CreateAt,
                    AccountID = order.AccountID,
                    LastUpdateAt = order.LastUpdateAt,
                    OrderItems = order.OrderItems.Select(oi => new OrderDto
                    {
                        ProductID = oi.ProductID,
                        ProductName = oi.ProductName,
                        ProductPrice = oi.ProductPrice,
                        Quantity = oi.Quantity,
                        Image = oi.Image
                    }).ToList(),
                    Account = new AccountDto
                    {
                        UserName = order.Account.UserName,
                        Phone = order.Account.PhoneNumber,
                        Email = order.Account.Email,
                        Adress = order.Account.Address

                        // Thêm các trường thông tin tài khoản khác (nếu cần)
                    }
                };

                // Trả về đối tượng OrderDto
                return Ok(orderDto);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
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
