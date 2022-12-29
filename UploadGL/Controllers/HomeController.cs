using AngleSharp.Css.Values;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UploadGL.Helpers;
using UploadGL.Models;

namespace UploadGL.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
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
    }
}