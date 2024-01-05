using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectSem3.Model
{
    public class ProductImage
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
        public float Price { get; set; }
        [Required]
        public int Quantity { get; set; }
        [Required]
        public bool Status { get; set; }
        public DateTime  CreateAt  { get; set; } 
        public DateTime? LastCreateAt { get; set; }

        public IFormFile ImageFile { get; set; }
    }
}
