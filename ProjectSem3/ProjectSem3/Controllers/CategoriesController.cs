using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectSem3.Model;

namespace ProjectSem3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowOrigin")]
    public class CategoriesController : ControllerBase
    {
        private readonly ShopDbContext _dbcontext;

        public CategoriesController(ShopDbContext context)
        {
            _dbcontext = context;
        }

        // GET: api/Categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
          if (_dbcontext.Categories == null)
          {
              return NotFound();
          }
            return await _dbcontext.Categories.ToListAsync();
        }

        // GET: api/Categories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
          if (_dbcontext.Categories == null)
          {
              return NotFound();
          }
            var category = await _dbcontext.Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            return category;
        }

        // PUT: api/Categories/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCategory(int id, Category updatedCategory)
        {
            try
            {
                // Lấy Category theo ID từ URL
                var existingCategory = await _dbcontext.Categories.FindAsync(id);

                if (existingCategory == null)
                {
                    return NotFound();
                }

                // Cập nhật toàn bộ thông tin của Category từ updatedCategory
                existingCategory.CategoryName = updatedCategory.CategoryName;
                existingCategory.Description = updatedCategory.Description;
                existingCategory.Status = updatedCategory.Status;
                // Cập nhật các trường khác tùy thuộc vào cấu trúc của đối tượng Category

                _dbcontext.Entry(existingCategory).State = EntityState.Modified;

                await _dbcontext.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, "Internal Server Error");
            }
        }


        // POST: api/Categories
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Consumes("application/json")]
        public async Task<ActionResult<Category>> PostCategory(Category category)
        {
          if (_dbcontext.Categories == null)
          {
              return Problem("Entity set 'ShopDbContext.Categories'  is null.");
          }
            _dbcontext.Categories.Add(category);
            try
            {
                await _dbcontext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (!CategoryExists(category.CategoryID))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }
            
            return CreatedAtAction("GetCategory", new { id = category.CategoryID }, category);
        }

        // DELETE: api/Categories/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            using (var transaction = await _dbcontext.Database.BeginTransactionAsync())
            {
                try
                {
                    var category = await _dbcontext.Categories.FindAsync(id);

                    if (category == null)
                    {
                        return NotFound();
                    }

                    // Tìm và xóa sản phẩm thuộc Category cần xóa
                    var productsToRemove = _dbcontext.Products.Where(p => p.CategoryID == id).ToList();
                    _dbcontext.Products.RemoveRange(productsToRemove);

                    // Tìm và xóa giỏ hàng (Carts) có chứa sản phẩm thuộc Category cần xóa
                    var cartsToRemove = _dbcontext.Carts.Where(c => productsToRemove.Any(p => p.ProductID == c.ProductID));
                    _dbcontext.Carts.RemoveRange(cartsToRemove);

                    // Xóa danh mục
                    _dbcontext.Categories.Remove(category);
                    await _dbcontext.SaveChangesAsync();

                    transaction.Commit();

                    return NoContent();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Internal Server Error: {ex.Message}");
                }
            }
        }



        private bool CategoryExists(int id)
        {
            return (_dbcontext.Categories?.Any(e => e.CategoryID == id)).GetValueOrDefault();
        }
    }
}
