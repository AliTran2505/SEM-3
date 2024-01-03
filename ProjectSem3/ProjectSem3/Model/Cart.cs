using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectSem3.Model
{
    [Table("Cart")]
    public class Cart
    {
        [Key]
        public int CartID { get; set; }
        [ForeignKey("Product")]
        public int ProductID { get; set; }
        [ForeignKey("Account")]
        public int AccountID { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime? LastCreateAt { get; set; }

    }
}
