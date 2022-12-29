using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace UploadGL.Models
{
    public class SettingModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "DataSource Should not be Empty")]
        public string DataSource { get; set; }
        [Required(ErrorMessage = "UserID Should not be Empty")]
        public string UserID { get; set; }
        [Required(ErrorMessage = "Password Should not be Empty")]
        public string Password { get; set; }
        [Required(ErrorMessage = "DefaultCollection Should not be Empty")]
        public string DefaultCollection { get; set; }
    }
}