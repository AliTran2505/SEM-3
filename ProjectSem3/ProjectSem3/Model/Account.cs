using Microsoft.AspNetCore.Identity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectSem3.Model
{
    [Table("Account")]
    public class Account : IdentityUser
    {
        [Key]
        public int AccountID { get; set; }
        [Required]
        public override string UserName { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        [DefaultValue("user")]
        public string RoleName { get; set; } 
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string MiddleName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public DateTime BirthDay { get; set; }
        [Required]

        public string Email { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        
        [Required]
        public string Address { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime? LastUpdateAt { get; set; }
    }
}
