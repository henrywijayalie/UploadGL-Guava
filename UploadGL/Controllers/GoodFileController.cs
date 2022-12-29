using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Mime;
using UploadGL.Helpers;
using UploadGL.Models;

namespace UploadGL.Controllers
{
    public class GoodFileController : Controller
    {
        //
        // GET: /Explorer/

        public ActionResult Index ()
        {
            return View();
        }

        public ActionResult List ()
        {
            string realPath;
            //  realPath = Server.MapPath("~/FileFailed/" + path);
            realPath = CommonHelper.MapPath("~/FileSuccess/");
            // or realPath = "FullPath of the folder on server" 

            if (Directory.Exists(realPath)) {
                List<DirModel> dirListModel = new List<DirModel>();
                IEnumerable<string> dirList = Directory.EnumerateDirectories(realPath);
                List<FileModel> fileListModel = new List<FileModel>();

                IEnumerable<string> fileList = Directory.EnumerateFiles(realPath);
                foreach (string file in fileList) {
                    FileInfo f = new FileInfo(file);

                    FileModel fileModel = new FileModel();

                    if (f.Extension.ToLower() != "php" && f.Extension.ToLower() != "aspx"
                        && f.Extension.ToLower() != "asp") {
                        fileModel.FileName = Path.GetFileName(file);
                        fileModel.FileAccessed = f.LastAccessTime;
                        fileModel.FileSizeText = (f.Length < 1024) ? f.Length.ToString() + " B" : f.Length / 1024 + " KB";

                        fileListModel.Add(fileModel);
                    }
                }

                ExplorerModel explorerModel = new ExplorerModel(dirListModel, fileListModel);
                return View(explorerModel);
            }
            else {
                return Content(" is not a valid file or directory.");
            }
        }


        [HttpGet]
        public ActionResult DownloadReportFileLink (string name)
        {

            string message = null;
            var path = CommonHelper.MapPath("~/FileSuccess/");


            if (!System.IO.File.Exists(path)) {
                path = Path.Combine(path, name);
            }

            if (System.IO.File.Exists(path)) {
                try {
                    var stream = new FileStream(path, FileMode.Open);

                    var result = new FileStreamResult(stream, MimeTypes.MapNameToMimeType(path)) {
                        FileDownloadName = Path.GetFileName(path)
                    };

                    return result;
                }
                catch (IOException) {
                    message = "FileInUse";
                }
            }

            return File(Encoding.UTF8.GetBytes(message), MediaTypeNames.Text.Plain, "DownloadImportFile.txt");
        }


    }
}
