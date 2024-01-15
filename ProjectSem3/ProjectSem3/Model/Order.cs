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
        public bool Status { get; set; } = false;
        public DateTime? LastUpdateAt { get; set; } = DateTime.Now;
        public DateTime CreateAt { get; set; } = DateTime.Now;
        [ForeignKey("Account")]
        public int AccountID { get; set; }
        public Account Account { get; set; }

        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
       

    }

    public class OrderItem
    {
        [Key]
        public int ID { get; set; }

        [ForeignKey("Order")]
        public int OrderID { get; set; }
        [ForeignKey("Product")]
        public int ProductID { get; set; }

        public int Quantity { get; set; }

        public  Product Product { get; set; }
    }


        public class FullOrderDto
    {
        public int OrderID { get; set; }
        public bool Status { get; set; }
        public DateTime CreateAt { get; set; }
        public int AccountID { get; set; }
        public DateTime? LastUpdateAt { get; set; }
        public AccountDto Account { get; set; }
        public List<OrderDto> OrderItems { get; set; }
    }

    public class AccountDto
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }

    }
    public class OrderDto
    {
        public int ID { get; set; }

        public int OrderID { get; set; }
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public Product Product { get; set; }
    }

}
