﻿using System.ComponentModel.DataAnnotations;

namespace ProjectSem3.Model
{
    public class RegisterModel
    {
        [Key]
        public int AccountID { get; set; }
        [Required]
        public string UserName { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public string RoleName { get; set; } = "user";
        [Required]

        public string Email { get; set; }
        
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
         
        public string Address { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public DateTime? LastUpdateAt { get; set; } = DateTime.Now;
    }
}