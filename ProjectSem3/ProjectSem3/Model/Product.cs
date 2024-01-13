using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectSem3.Model
{
    [Table("Product")]
    public class Product
    {
        [Key]
        public int ProductID { get; set; }

        [ForeignKey("Category")]
        public int CategoryID { get; set; }

        [Required]
        public string ProductName { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string? Image { get; set; }

        [Required]
        public float Price { get; set; }

        [Required]
        public int Quantity { get; set; }

        public bool Status { get; set; } = true;

        public DateTime CreateAt { get; set; } = DateTime.Now;

        public DateTime? LastUpdateAt { get; set; } = DateTime.Now;

        public Category Category { get; set; }
    }

    public class ProductDto
    {
        public int ProductID { get; set; }

        public int CategoryID { get; set; }

        public string ProductName { get; set; }

        public string Description { get; set; }

        public string? Image { get; set; }

        // Add the following property for ImageUrl
        public string ImageUrl => Image; // This assumes you want to expose Image as ImageUrl

        public float Price { get; set; }

        public int Quantity { get; set; }

        public bool Status { get; set; } = true;

        public DateTime CreateAt { get; set; } = DateTime.Now;

        public DateTime? LastUpdateAt { get; set; } = DateTime.Now;

        public CategoryDto Category { get; set; }
    }

    // Include the CategoryDto class
    public class CategoryDto
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
    }
}
