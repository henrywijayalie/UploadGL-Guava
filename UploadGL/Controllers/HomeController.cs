using AngleSharp.Css.Values;
using Hangfire;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UploadGL.Helpers;
using UploadGL.Models;

using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Threading;
using Hangfire.Storage;
using IBM.Data.DB2.iSeries;
using System.Configuration;

namespace UploadGL.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Buffer()
        {
            return Content(TextBuffer.ToString());
        }

        public ActionResult ClearLog()
        {

            TextBuffer.Clear();
            return RedirectToAction("Index");
        }

        public ActionResult About()
        {
            ViewBag.Message = "UploadGL merupakan aplikasi berbasis web yang dibuat secara khusus oleh PT Warna Bintang Kreasi untuk mengunggah Jurnal Umum ke dalam sistem WINCORE";
            return View();
        }

        public ActionResult ClientUpload()
        {
            return View();
        }

        // generic file post method - use in MVC or WebAPI
        [HttpPost]
        public ActionResult UploadFile()
        {
            Logger logger = new Logger();
            bool valid = false;

            foreach (string file in Request.Files)
            {
                var allowedExtensions = new[] { ".csv" };
                var FileDataContent = Request.Files[file];
                var checkextension = Path.GetExtension(FileDataContent.FileName).ToLower();

                //  var FileDataContent = Request.Files[file];
                if (FileDataContent != null && FileDataContent.ContentLength > 0)
                {
                    // take the input stream, and save it to a temp folder using the original file.part name posted
                    var stream = FileDataContent.InputStream;
                    var fileName = Path.GetFileName(FileDataContent.FileName);
                    var UploadPath = string.Empty;
                    if (fileName.Contains("EOD"))
                    {
                        UploadPath = Server.MapPath("~/FileEOD/");
                    }
                    else
                    {
                        UploadPath = Server.MapPath("~/FileIntraday/");
                    }
                    if (!string.IsNullOrEmpty(UploadPath))
                    {
                        Directory.CreateDirectory(UploadPath);
                        string path = Path.Combine(UploadPath, fileName);
                        try
                        {
                            if (System.IO.File.Exists(path))
                                System.IO.File.Delete(path);
                            using (var fileStream = System.IO.File.Create(path))
                            {
                                stream.CopyTo(fileStream);
                            }
                            // Once the file part is saved, see if we have enough to merge it
                            Utils UT = new Utils();
                            UT.MergeFile(path);

                        }
                        catch (IOException ex)
                        {
                            valid = false;
                            logger.Error(ex.Message);
                        }

                        valid = true;
                    }
                    else
                    {
                        valid = false;
                    }
                }
                else
                {
                    valid = false;
                }
            }
            if (valid == true)
            {
                if (GetFlags() == true)
                {
                    FlagUtil rec = new FlagUtil
                    {
                        Flags = false
                    };

                    SaveFlags(rec);
                    return Json(new { success = true, message = "File Successfully Uploaded" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { success = false, message = "" }, JsonRequestBehavior.AllowGet); ;
                }
            }
            else
            {
                return Json(new { error = true, message = "Failed to upload file, please contact administrator" }, JsonRequestBehavior.AllowGet);
            }
        }

        public static bool GetFlags()
        {
            string setting = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/");
            FlagUtil flags = JsonConvert.DeserializeObject<FlagUtil>(System.IO.File.ReadAllText(setting + "Flag.json"));

            if (flags.Flags == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void SaveFlags(FlagUtil flags)
        {
            string setting = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/");
            System.IO.File.WriteAllText(setting + "Flag.json", JsonConvert.SerializeObject(flags));
        }

        [HttpPost]
        public ActionResult ManualProcess()
        {
            if (MainProcess.isInvalidProcess() == false)
            {
                TextBuffer.WriteLine("");
                TextBuffer.WriteLine("================================================================================================================================== ");
                TextBuffer.WriteLine("PROCESS JOB MANUAL UPLOAD DATA GL START! ");
                TextBuffer.WriteLine(ConnectionHelper.GetStringConnectionLog());
                TextBuffer.WriteLine("================================================================================================================================== ");
                TextBuffer.WriteLine("");

                // BackgroundJob.Enqueue((string jobId) => SendEmail(name, jobId));
                // var jobId = BackgroundJob.Enqueue(() => TaskToRun);
                // BackgroundJob.ContinueWith(jobId, () => SendEmail(name, jobId));
                // var jobId = BackgroundJob.Enqueue(() => JobProcessUpload.RunProcess(JobProcessUpload.GetFileName()));
                // BackgroundJob.ContinueWith(jobId, () => JobProcessUpload.ClearUplaodFile(jobId));
                BackgroundJob.Enqueue(() => MainProcess.RunProcess(MainProcess.GetFileName()));
                //BackgroundJob.Enqueue(() => MainProcess.RunProcess(MainProcess.GetFileName()));
                //BackgroundJob.Enqueue(() => Func.Execute(1, "Test", "OK"));
            }
            else
            {
                TextBuffer.WriteLine("");
                TextBuffer.WriteLine("SENDING NEW BATCH CANNOT CONTINUED ... PREVIOUS JOBS STILL RUNNING!");
                TextBuffer.WriteLine("");
            }

            return RedirectToAction("Index");
        }


        public ActionResult DownloadLog()
        {
            var byteArray = ReadAllBytes(System.Web.Hosting.HostingEnvironment.MapPath("~/Log/LogConsole.txt"));
            var stream = new MemoryStream(byteArray);
            // sr.Close();
            // ((FileAppender)LogManager.GetCurrentLoggers()[0].Logger.Repository.GetAppenders()[0]).DoAppend();
            return File(stream, "text/plain", "LogConsoleUploadGL.txt");
        }

        public static byte[] ReadAllBytes(String path)
        {
            byte[] bytes;
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                int index = 0;
                long fileLength = fs.Length;
                if (fileLength > Int32.MaxValue)
                    throw new InvalidOperationException("File too long");
                int count = (int)fileLength;
                bytes = new byte[count];
                while (count > 0)
                {
                    int n = fs.Read(bytes, index, count);
                    if (n == 0)
                        throw new InvalidOperationException("End of file reached before expected");
                    index += n;
                    count -= n;
                }
            }
            return bytes;
        }

        public ActionResult ClearFileUpload()
        {

            string fileUpload = System.Web.Hosting.HostingEnvironment.MapPath("~/FileUpload/");
            string FileTemp = System.Web.Hosting.HostingEnvironment.MapPath("~/FileTemp/");

            try
            {
                System.IO.DirectoryInfo di = new DirectoryInfo(fileUpload);

                foreach (FileInfo file in di.GetFiles())
                {
                    TextBuffer.WriteLine(string.Format("Delete Upload File ({0})", file.FullName));
                    file.Delete();
                }

                System.IO.DirectoryInfo dt = new DirectoryInfo(FileTemp);

                foreach (FileInfo file in dt.GetFiles())
                {
                    //TextBuffer.WriteLine(string.Format("Delete Upload File ({0})", file.FullName));
                    file.Delete();
                }

                //  TextBuffer.WriteLine(string.Format("Delete all Upload File ({0})", di.GetFiles().Count()));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.ToString());
            }

            return RedirectToAction("Index");
        }

        protected virtual bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }

        public static class Func
        {
            [DisplayName("Nama File: {0}")]
            public static void Execute(long requestID, string stepName, string stepLocation)
            {

                string fileIntraday = ConfigurationManager.ConnectionStrings["FileIntraday"].ConnectionString + "\\";
                string fileEOD = ConfigurationManager.ConnectionStrings["FileEOD"].ConnectionString + "\\";

                string[] filesIntraday = Directory.GetFiles(fileIntraday);
                string[] filesEOD = Directory.GetFiles(fileEOD);

                List<string> listDirectory = new List<string>();

                if (filesIntraday.Length > 0)
                {
                    var intr = filesIntraday.ToList<string>();
                    listDirectory.AddRange(intr);
                }
                if (filesEOD.Length > 0)
                {
                    var eod = filesIntraday.ToList<string>();
                    listDirectory.AddRange(eod);
                }


                for (int i = 0; i < listDirectory.Count; i++)
                {
                    string fileName = listDirectory[i];

                    // JobProcessUpload.RunProcess(fileName);
                }
            }
        }

        [HttpPost]
        public ActionResult FutureSchedules(string expression)
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
                ViewBag.CronScheduleParseError = ex.Message;
                return PartialView(Enumerable.Empty<DateTime>());
            }
        }

    }
}