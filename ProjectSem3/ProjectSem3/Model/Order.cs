using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectSem3.Model
{
    // Enum để xác định trạng thái của đơn hàng
    public enum OrderStatus
    {
        OrderPlacedSuccessfully = 0,  // Đặt hàng thành công
        Processing = 1,               // Đang xử lý
        Delivered = 2,                // Đã giao hàng
        OrderReceivedSuccessfully = 3 , // Nhận hàng thành công
        Canceled = 4                  // Huỷ
    }

    [Table("Order")]
    public class Order
    {
        [Key]
        public int OrderID { get; set; }

        // Sử dụng enum cho trạng thái của đơn hàng
        public OrderStatus Status { get; set; } = OrderStatus.OrderPlacedSuccessfully; // Giá trị mặc định

        public DateTime? LastUpdateAt { get; set; } = DateTime.Now;

        public DateTime CreateAt { get; set; } = DateTime.Now;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public float Price { get; set; }

        [ForeignKey("Account")]
        public int AccountID { get; set; }

        public Account Account { get; set; }

        public List<OrderItem> OrderItem { get; set; } = new List<OrderItem>();
    }

    [Table("OrderItem")]
    public class OrderItem
    {
        [Key]
        public int ID { get; set; }

        [ForeignKey("Order")]
        public int OrderID { get; set; }
        public int Quantity { get; set; }

        // Sử dụng chuỗi để lưu thông tin sản phẩm đã được serial hóa
        public string SerializedProduct { get; init; }
    }

    public class FullOrderDto
    {
        public int OrderID { get; set; }
        public OrderStatus Status { get; set; }  // Sử dụng enum cho trạng thái
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
        public int Quantity { get; set; }
        public Product Product { get; set; } // Đảm bảo Product đã được định nghĩa
    }
}
