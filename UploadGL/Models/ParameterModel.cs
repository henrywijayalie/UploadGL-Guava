using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace UploadGL.Models
{
    public class ParameterModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "TimeOutValidasi Should not be Empty")]
        public string TimeOutValidasi { get; set; }
        [Required(ErrorMessage = "TimeOutPosting Should not be Empty")]
        public string TimeOutPosting { get; set; }
    }
}