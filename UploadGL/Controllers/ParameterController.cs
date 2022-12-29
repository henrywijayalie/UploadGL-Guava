using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Newtonsoft.Json;
using System.IO;
using UploadGL.Models;
using UploadGL.Helpers;

namespace UploadGL.Controllers
{
    public class ParameterController : Controller
    {
        //
        // GET: /Employee/
        // public ActionResult List()
        // {
        //     return View();
        //  }


        public ActionResult GetData ()
        {
            StreamReader sr = new StreamReader(System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/Parameter.json"));
            var sets = JsonConvert.DeserializeObject<List<ParameterModel>>(sr.ReadToEnd());
            sr.Close();

            return Json(new { data = sets }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Edit (int Id = 0)
        {
            List<ParameterModel> sett = new List<ParameterModel>();
            ParameterJsonHelper readWrite = new ParameterJsonHelper();
            sett = JsonConvert.DeserializeObject<List<ParameterModel>>(readWrite.Read());

            if (Id == 0)
                return View(new ParameterModel());
            else {
                return View(sett.Where(x => x.Id == Id).FirstOrDefault<ParameterModel>());
            }
        }


        public ActionResult List ()
        {
            List<ParameterModel> sett = new List<ParameterModel>();
            ParameterJsonHelper readWrite = new ParameterJsonHelper();
            sett = JsonConvert.DeserializeObject<List<ParameterModel>>(readWrite.Read());

            var datum = sett.Where(x => x.Id == 1).SingleOrDefault();
            var data = new ParameterModel() {
                TimeOutValidasi = datum.TimeOutValidasi,
                TimeOutPosting = datum.TimeOutPosting,
                Id = datum.Id
            };

            return View(data);
        }


        [HttpPost]
        public ActionResult Saving (ParameterModel model)
        {
            List<ParameterModel> sett = new List<ParameterModel>();
            ParameterJsonHelper readWrite = new ParameterJsonHelper();
            sett = JsonConvert.DeserializeObject<List<ParameterModel>>(readWrite.Read());

            ParameterModel setts = sett.FirstOrDefault(x => x.Id == model.Id);

            if (setts != null) {

                int index = sett.FindIndex(x => x.Id == model.Id);
                sett[index].TimeOutValidasi = model.TimeOutValidasi;
                sett[index].TimeOutPosting = model.TimeOutPosting;
            }

            string jSONString = JsonConvert.SerializeObject(sett);
            readWrite.Write(jSONString);

            return Json(new { success = true, message = "Updated Parameter Successfully" }, JsonRequestBehavior.AllowGet);
        }
    }
}