﻿using Microsoft.AspNetCore.Identity;
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

        public string RoleName { get; set; } = "user";
        [Required]
        public string Email { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public string Address { get; set; }

        
           public bool Status {  get; set; } = true;
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public DateTime? LastUpdateAt { get; set; } = DateTime.Now;
    }
    public class LoginResponse
    {
        public string Token { get; set; }
        public Account UserData { get; set; }
    }
}
