using Hangfire;
using Hangfire.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Web;
using System.Web.Mvc;
using UploadGL.Helpers;
using UploadGL.Models;

namespace UploadGL.Controllers
{
    public class ScheduleTaskController : Controller
    {
        Logger logger = new Logger();
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult List()
        {
            var model = new ScheduleTaskModel();

            if (ModelState.IsValid)
            {
                model.CronExpression = GetSchedule();
                model.Name = "Jadwal Upload GL";
            }

            return View(model);
        }


        //  [HttpPost]
        public ActionResult FutureScheduleModels(string expression)
        {
            try
            {
                var now = DateTime.Now;
                var model = CronExpression.GetFutureSchedules(expression, now, now.AddYears(1), 20);
                ViewBag.Description = CronExpression.GetFriendlyDescription(expression);
                return PartialView(model);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                ViewBag.CronScheduleModelParseError = ex.Message;
                return PartialView(Enumerable.Empty<DateTime>());
            }
        }


        [HttpPost]
        public ActionResult SaveSetting(ScheduleTaskModel model)
        {
            try
            {
                ScheduleModel jadwal = new ScheduleModel();

                jadwal.Proses = "Upload GL";
                jadwal.Jadwal = model.CronExpression;

                SaveJadwal(jadwal);
                AddNewJobId();

                return RedirectToAction("List");
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return RedirectToAction("Index");
            }
        }
        public static string GetSchedule()
        {
            string setting = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/");
            ScheduleModel jadwal = JsonConvert.DeserializeObject<ScheduleModel>(System.IO.File.ReadAllText(setting + "Schedule.json"));

            if (jadwal.Jadwal == null)
            {
                return "0 * * * *";
            }
            else
            {
                return jadwal.Jadwal.ToString();
            }
        }
        public static void SaveJadwal(ScheduleModel jadwal)
        {
            string setting = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/");

            System.IO.File.WriteAllText(setting + "Schedule.json", JsonConvert.SerializeObject(jadwal));
            /*
            // serialize JSON directly to a file
            using (StreamWriter file = File.CreateText(setting + "Schedule.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, jadwal);
            }
         */
        }

        public static void AddNewJobId()
        {
            List<RecurringJobDto> list;

            using (var connection = JobStorage.Current.GetConnection())
            {
                list = connection.GetRecurringJobs();
            }

            var jobs = list?.FirstOrDefault(j => j.Id == "UploadGL");
            if (jobs != null && !string.IsNullOrEmpty(jobs.LastJobId))
            {
                BackgroundJob.Delete(jobs.LastJobId);
            }

            RecurringJob.RemoveIfExists("UploadGL");
            RecurringJob.AddOrUpdate("UploadGL", () => MainProcess.ProceedBySchedule(), GetSchedule, TimeZoneInfo.Local);
        }
    }
}