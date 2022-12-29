using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace UploadGL.Models
{
    public class UserModel
    {
        public int UserID { get; set; }

        [Required(ErrorMessage = "UserName Should not be Empty")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password Should not be Empty")]
        public string Password { get; set; }
    }
}