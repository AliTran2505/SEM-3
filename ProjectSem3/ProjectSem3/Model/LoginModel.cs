using System.ComponentModel.DataAnnotations;

namespace ProjectSem3.Model
{
    public class LoginModel
    {
      
        [Required]
        public string UserName { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
