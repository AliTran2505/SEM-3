using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
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
    public class ProductsController : ControllerBase
    {
        private readonly ShopDbContext _context;

        public ProductsController(ShopDbContext context)
        {
            _context = context;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
          if (_context.Products == null)
          {
              return NotFound();
          }
            return await _context.Products.ToListAsync();
        }

        // GET: api/Products/5
        [HttpGet]
        [Route("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
          if (_context.Products == null)
          {
              return NotFound();
          }
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return product;
        }

        [HttpGet]
        [Route("Search-by-categoryname")]
        public async Task<ActionResult<Product>> SearchProduct(string catName)
        {
            var products = _context.Products
             .Where(p => p.Category.CategoryName == catName).ToList();

            return Ok(products);
        }


        // PUT: api/Products/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, Product product)
        {
            if (id != product.ProductID)
            {
                return BadRequest();
            }

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Products
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Consumes("application/json")]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
          if (_context.Products == null)
          {
              return Problem("Entity set 'ShopDbContext.Products'  is null.");
          }
            _context.Products.Add(product);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException) 
            {
                if (!ProductExists(product.ProductID))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }
           

            return CreatedAtAction("GetProduct", new { id = product.ProductID }, product);
        }
        //POST IMAGE
        [HttpPost]
        [Route("uploadfile")]
        public async Task<IActionResult> PostWithImageAsync([FromForm] ProductImage p)
        {
                
                var findP = _context.Products.Find(p.ProductID);
                if (findP != null)
                {
                    return Ok("Mã sản phẩm này đã có rồi");
                }
                else
                {
                var product = new Product {  CategoryID = p.CategoryID, Description = p.Description, Price = p.Price, ProductName = p.ProductName, Quantity = p.Quantity, Status = p.Status, CreateAt = p.CreateAt, LastCreateAt = p.LastCreateAt };
                    if (p.ImageFile.Length > 0)
                    {
                        var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", p.ImageFile.FileName);
                        using (var stream = System.IO.File.Create(path))
                        {
                            await p.ImageFile.CopyToAsync(stream);
                        }
                        product.Image = "/images/" + p.ImageFile.FileName;
                    }
                    else
                    {
                        product.Image = "";
                    }
                    await _context.Products.AddAsync(product);
                    await _context.SaveChangesAsync();
                    return Ok(product);
                }
        }

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProductExists(int id)
        {
            return (_context.Products?.Any(e => e.ProductID == id)).GetValueOrDefault();
        }
    }
}
