﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectSem3.Model
{
    [Table("Order")]
    public class Order
    {
        [Key]
        public int OrderID { get; set; }
        [ForeignKey("Cart")]
        public int CartID { get; set; }
        [Required]
        public float Total { get; set; }
        [DefaultValue("true")]
        public bool Status { get; set; }
        [Required]
        public string DeliveryType { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public DateTime? LastUpdateAt { get; set; } = DateTime.Now;
    }
}
