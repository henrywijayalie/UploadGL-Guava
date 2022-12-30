using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace UploadGL.Models
{
    public class ScheduleTaskModel
    {
        [Display(Name = "Job Name :")]
        public string Name { get; set; }
        [Display(Name = "Cron Expression :")]
        public string CronExpression { get; set; }
        [Display(Name = "Cron Description : ")]
        public string CronDescription { get; set; }
        [Display(Name = "Enabled : ")]
        public bool Enabled { get; set; }
        [Display(Name = "Next Execution : ")]
        public DateTime? NextRun { get; set; }
    }
}