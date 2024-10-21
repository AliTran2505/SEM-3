using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
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
        private readonly ShopDbContext _dbcontext;

        public ProductsController(ShopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        [HttpGet]
        public async Task<ActionResult<IQueryable<ProductDto>>> GetProducts()
        {
            try
            {
                var products = await _dbcontext.Products
                    .Include(p => p.Category)
                    .Select(p => ProductToDto(p)) // Use the updated mapping method
                    .ToListAsync();

                return Ok(products);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("searchByName")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> SearchProductsByName(string name)
        {
            try
            {
                // Use EF Core to perform the search on the server with case-insensitivity
                var products = await _dbcontext.Products
                    .Include(p => p.Category)
                    .Where(p => p.ProductName.ToLower().Contains(name.ToLower()))
                    .ToListAsync();

                // Check if no products were found
                if (!products.Any())
                {
                    return NotFound(new { message = "No products found", statusCode = 404 });
                }

                // Map the products to ProductDto
                var productDtos = products.Select(ProductToDto).ToList();

                return Ok(productDtos);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }



        [HttpGet("filterByCategory")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> FilterProductsByCategory(int categoryId)
        {
            try
            {
                // Lọc các sản phẩm theo ID của danh mục
                var products = await _dbcontext.Products
                    .Include(p => p.Category)
                    .Where(p => p.CategoryID == categoryId)
                    .ToListAsync();

                // Kiểm tra nếu không tìm thấy sản phẩm nào
                if (!products.Any())
                {
                    return NotFound(new { message = "No products found in this category", statusCode = 404 });
                }

                // Chuyển đổi các sản phẩm sang ProductDto
                var productDtos = products.Select(ProductToDto).ToList();

                return Ok(productDtos);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            try
            {
                var product = await _dbcontext.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.ProductID == id);

                if (product == null)
                {
                    return NotFound();
                }

                // Use the updated mapping method
                var productDto = ProductToDto(product);

                return Ok(productDto);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> PostProduct([FromForm] ProductImage productDto)
        {
            try
            {
                // Trích xuất CategoryID từ dữ liệu form
                var categoryId = Convert.ToInt32(productDto.CategoryID);

                // Check if the product with the same name already exists
                var existingProduct = await _dbcontext.Products
                    .FirstOrDefaultAsync(p => p.ProductName == productDto.ProductName);

                if (existingProduct != null)
                {
                    // Xử lý existingProduct nếu cần thiết
                }

                // Truy vấn category hiện tại hoặc tạo mới nếu chưa tồn tại
                var existingCategory = await _dbcontext.Categories.FindAsync(categoryId);

                if (existingCategory == null)
                {
                    // Tạo mới category nếu không tìm thấy
                    existingCategory = new Category
                    {
                        CategoryID = categoryId,
                        // Các thông tin khác của category
                    };

                    // Thêm category mới vào cơ sở dữ liệu
                    _dbcontext.Categories.Add(existingCategory);
                    await _dbcontext.SaveChangesAsync();
                }

                // Tạo một sản phẩm mới
                var product = new Product
                {
                    CategoryID = categoryId,
                    ProductName = productDto.ProductName,
                    Description = productDto.Description,
                    Price = productDto.Price,
                    Quantity = productDto.Quantity,
                    Status = productDto.Status,
                    CreateAt = DateTime.Now,
                    LastUpdateAt = DateTime.Now
                };

                // Lưu ảnh của sản phẩm
                if (productDto.ImageFile != null && productDto.ImageFile.Length > 0)
                {
                    var imageName = Guid.NewGuid().ToString() + Path.GetExtension(productDto.ImageFile.FileName);
                    var imagePath = Path.Combine("wwwroot/images/", imageName);

                    using (var stream = System.IO.File.Create(imagePath))
                    {
                        await productDto.ImageFile.CopyToAsync(stream);
                    }

                    product.Image = imageName;
                }

                // Liên kết sản phẩm với category hiện tại
                product.Category = existingCategory;

                // Thêm sản phẩm vào cơ sở dữ liệu
                _dbcontext.Products.Add(product);
                await _dbcontext.SaveChangesAsync();

                // Chuyển đổi sản phẩm sang DTO để trả về
                var productDtoResponse = ProductToDto(product);

                return CreatedAtAction(nameof(GetProduct), new { id = product.ProductID }, productDtoResponse);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }   
        }




        [HttpPut("{productId}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> PutProduct(int productId, [FromForm] ProductImage productDto)
        {
            using (var transaction = _dbcontext.Database.BeginTransaction())
            {
                try
                {
                    var product = await _dbcontext.Products
                        .Include(p => p.Category)
                        .FirstOrDefaultAsync(p => p.ProductID == productId);

                    if (product == null)
                    {
                        return NotFound("Product not found");
                    }

                    // Lưu thông tin cũ của sản phẩm
                    var oldProductName = product.ProductName;
                    var oldProductPrice = product.Price;
                    var oldImagePath = product.Image; // Lưu đường dẫn ảnh cũ

                    // Cập nhật thông tin sản phẩm
                    product.CategoryID = productDto.CategoryID;
                    product.ProductName = productDto.ProductName;
                    product.Description = productDto.Description;
                    product.Price = productDto.Price;
                    product.Quantity = productDto.Quantity;
                    product.Status = productDto.Status;
                    product.LastUpdateAt = DateTime.Now;

                    // Lưu ảnh mới của sản phẩm
                    if (productDto.ImageFile != null && productDto.ImageFile.Length > 0)
                    {
                        var imageName = Guid.NewGuid().ToString() + Path.GetExtension(productDto.ImageFile.FileName);
                        var imagePath = Path.Combine("wwwroot/images/", imageName);

                        using (var stream = System.IO.File.Create(imagePath))
                        {
                            await productDto.ImageFile.CopyToAsync(stream);
                        }

                        // Xóa ảnh cũ nếu tồn tại
                        if (!string.IsNullOrEmpty(oldImagePath))
                        {
                            var oldImageFullPath = Path.Combine("wwwroot", oldImagePath.TrimStart('/'));
                            if (System.IO.File.Exists(oldImageFullPath))
                            {
                                System.IO.File.Delete(oldImageFullPath);
                            }
                        }

                        product.Image = imageName;
                    }

                    await _dbcontext.SaveChangesAsync();


                    // Cập nhật thông tin sản phẩm trong giỏ hàng (Carts)
                    var cartsToUpdate = await _dbcontext.Carts
                        .Where(c => c.ProductID == productId)
                        .ToListAsync();

                    foreach (var cart in cartsToUpdate)
                    {
                        cart.Product.ProductName = productDto.ProductName;
                        cart.Product.Price = productDto.Price;
                        cart.Product.Description = productDto.Description;
                        cart.Product.Image = product.Image; // Cập nhật đường dẫn ảnh mới
                    }

                    await _dbcontext.SaveChangesAsync();

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
            var product = await _dbcontext.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductID == id);

            if (product == null)
            {
                return NotFound();
            }

            // Xóa ảnh cũ nếu tồn tại
            if (!string.IsNullOrEmpty(product.Image))
            {
                var oldImageFullPath = Path.Combine("wwwroot", product.Image.TrimStart('/'));
                if (System.IO.File.Exists(oldImageFullPath))
                {
                    System.IO.File.Delete(oldImageFullPath);
                }
            }

            // Remove product from all carts
            var carts = _dbcontext.Carts.Where(c => c.ProductID == id);
            _dbcontext.Carts.RemoveRange(carts);

            // Remove product
            _dbcontext.Products.Remove(product);
            await _dbcontext.SaveChangesAsync();

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
            Category = product.Category != null ? new CategoryDto
            {
                CategoryID = product.Category.CategoryID,
                CategoryName = product.Category.CategoryName,
                Description = product.Category.Description
            } : null
        };


    }
}
