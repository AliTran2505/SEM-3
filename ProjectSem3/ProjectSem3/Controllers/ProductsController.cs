using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectSem3.Model;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectSem3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ShopDbContext _context;

        public ProductsController(ShopDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IQueryable<ProductDto>>> GetProducts()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Select(p => ProductToDto(p))
                .ToListAsync();

            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductID == id);

            if (product == null)
            {
                return NotFound();
            }

            return Ok(ProductToDto(product));
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> PostProduct([FromForm] ProductImage productDto)
        {
            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductName == productDto.ProductName);

            if (existingProduct != null)
            {
                return BadRequest("Product with the same name already exists.");
            }

            var product = new Product
            {
                CategoryID = productDto.CategoryID,
                ProductName = productDto.ProductName,
                Description = productDto.Description,
                Price = productDto.Price,
                Quantity = productDto.Quantity,
                Status = productDto.Status,
                CreateAt = DateTime.Now,
                LastUpdateAt = DateTime.Now

            };
            if (productDto.ImageFile != null && productDto.ImageFile.Length > 0)
            {
                var imageName = Guid.NewGuid().ToString() + Path.GetExtension(productDto.ImageFile.FileName);
                var imagePath = Path.Combine("wwwroot/images/", imageName);

                using (var stream = System.IO.File.Create(imagePath))
                {
                    await productDto.ImageFile.CopyToAsync(stream);
                }

                product.Image = "/images/" + imageName;
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = product.ProductID }, ProductToDto(product));
        }

        [HttpPut("{productId}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> PutProduct(int productId, [FromForm] ProductImage productDto)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var product = await _context.Products.FindAsync(productId);

                    if (product == null)
                    {
                        return NotFound("Product not found");
                    }

                    // Lưu thông tin cũ của sản phẩm
                    var oldProductName = product.ProductName;
                    var oldProductPrice = product.Price;

                    // Cập nhật thông tin sản phẩm
                    product.CategoryID = productDto.CategoryID;
                    product.ProductName = productDto.ProductName;
                    product.Description = productDto.Description;
                    product.Price = productDto.Price;
                    product.Quantity = productDto.Quantity;
                    product.Status = productDto.Status;
                    product.LastUpdateAt = DateTime.Now;

                    if (productDto.ImageFile != null && productDto.ImageFile.Length > 0)
                    {
                        var imageName = Guid.NewGuid().ToString() + Path.GetExtension(productDto.ImageFile.FileName);
                        var imagePath = Path.Combine("wwwroot/images/", imageName);

                        using (var stream = System.IO.File.Create(imagePath))
                        {
                            await productDto.ImageFile.CopyToAsync(stream);
                        }

                        product.Image = "/images/" + imageName;
                    }

                    await _context.SaveChangesAsync();

                    // Cập nhật thông tin sản phẩm trong đơn hàng (Orders)
                    var ordersToUpdate = await _context.Orders
                        .Where(o => o.OrderItems.Any(oi => oi.ProductID == productId))
                        .ToListAsync();

                    foreach (var order in ordersToUpdate)
                    {
                        var orderItem = order.OrderItems.First(oi => oi.ProductID == productId);
                        orderItem.ProductName = productDto.ProductName;
                        orderItem.ProductPrice = productDto.Price;
                    }

                    // Cập nhật thông tin sản phẩm trong giỏ hàng (Carts)
                    var cartsToUpdate = await _context.Carts
                        .Where(c => c.ProductID == productId)
                        .ToListAsync();

                    foreach (var cart in cartsToUpdate)
                    {
                        cart.Product.ProductName = productDto.ProductName;
                        cart.Product.Price = productDto.Price;
                        cart.Product.Description = productDto.Description;
                    }

                    await _context.SaveChangesAsync();

                    transaction.Commit();

                    return Ok(ProductToDto(product));
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return StatusCode(500, $"Internal Server Error: {ex.Message}");
                }
            }
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            // Remove product from all carts
            var carts = _context.Carts.Where(c => c.ProductID == id);
            _context.Carts.RemoveRange(carts);

            // Remove product from all orders
            var orderItems = _context.OrderItems.Where(oi => oi.ProductID == id);
            var orders = _context.Orders.Where(o => orderItems.Select(oi => oi.OrderID).Contains(o.OrderID));
            _context.OrderItems.RemoveRange(orderItems);
            _context.Orders.RemoveRange(orders);

            // Remove product
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static ProductDto ProductToDto(Product product) => new ProductDto
        {
            ProductID = product.ProductID,
            CategoryID = product.CategoryID,
            ProductName = product.ProductName,
            Description = product.Description,
            Image = product.Image,
            Price = product.Price,
            Quantity = product.Quantity,
            Status = product.Status,
            CreateAt = product.CreateAt,
            LastUpdateAt = product.LastUpdateAt,
        };

        
           
        
    }
}
