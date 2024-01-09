﻿using System.ComponentModel;
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
        public int Quantity { get; set;}
        [DefaultValue("true")]
        public bool Status { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public DateTime? LastUpdateAt { get; set; } = DateTime.Now;

        public virtual Category Category { get; set; }
    }
}
