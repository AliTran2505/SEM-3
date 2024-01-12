using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectSem3.Model
{
    [Table("Order")]
    public class Order
    {
        [Key]
        public int OrderID { get; set; }
        public bool Status { get; set; } = true;
        public DateTime CreateAt { get; set; } = DateTime.Now;
        [ForeignKey("AccountID")]
        public int AccountID { get; set; }
        public DateTime? LastUpdateAt { get; set; } = DateTime.Now;
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    }

    public class OrderItem
    {
        [Key]
        public int ID { get; set; }

        [ForeignKey("Order")]
        public int OrderID { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public float ProductPrice { get; set; }
        public int Quantity { get; set; }
    }
    
}
