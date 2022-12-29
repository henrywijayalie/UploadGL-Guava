using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using System.IO;
using UploadGL.Models;
using UploadGL.Helpers;
using IBM.Data.DB2.iSeries;

namespace UploadGL.Controllers
{
    public class SettingController : Controller
    {
        public ActionResult GetData ()
        {
            //  Serialize2();
            StreamReader sr = new StreamReader(System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/Setting.json"));
            var sets = JsonConvert.DeserializeObject<List<SettingModel>>(sr.ReadToEnd());
            sr.Close();

            return Json(new { data = sets }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Edit (int Id = 0)
        {
            List<SettingModel> sett = new List<SettingModel>();
            SettingJsonHelper readWrite = new SettingJsonHelper();
            sett = JsonConvert.DeserializeObject<List<SettingModel>>(readWrite.Read());

            if (Id == 0)
                return View(new SettingModel());
            else {
                return View(sett.Where(x => x.Id == Id).FirstOrDefault<SettingModel>());
            }
        }


        public ActionResult List ()
        {
            List<SettingModel> sett = new List<SettingModel>();
            SettingJsonHelper readWrite = new SettingJsonHelper();
            sett = JsonConvert.DeserializeObject<List<SettingModel>>(readWrite.Read());

            var datum = sett.Where(x => x.Id == 1).SingleOrDefault();

            var data = new SettingModel() {
                UserID = datum.UserID,
                Password = StringEncryptor.Decrypt(datum.Password),
                DefaultCollection = datum.DefaultCollection,
                DataSource = datum.DataSource,
                Id = datum.Id
            };

            return View(data);
        }


        [HttpPost]
        public ActionResult Saving (SettingModel model)
        {
            bool connect = false;
            List<SettingModel> sett = new List<SettingModel>();
            SettingJsonHelper readWrite = new SettingJsonHelper();
            sett = JsonConvert.DeserializeObject<List<SettingModel>>(readWrite.Read());

            SettingModel setts = sett.FirstOrDefault(x => x.Id == model.Id);

            if (setts != null) {

                int index = sett.FindIndex(x => x.Id == model.Id);
                sett[index].UserID = model.UserID;
                sett[index].Password = StringEncryptor.Encrypt(model.Password);
                sett[index].DataSource = model.DataSource;
                sett[index].DefaultCollection = model.DefaultCollection;

                connect = TesKoneksi(string.Format("Data Source = {0}; User ID = {1}; Password = {2}; Default Collection = {3}; DataCompression=true;  Pooling=false; ConnectionTimeout=1;", model.DataSource, model.UserID, model.Password, model.DefaultCollection));
            }

            if (connect == true) {
                string jSONString = JsonConvert.SerializeObject(sett);
                readWrite.Write(jSONString);

                return Json(new { success = true, message = "Updated and Connectivity Successfully" }, JsonRequestBehavior.AllowGet);
            }
            else {
                return Json(new { success = true, message = "Saving Failed, connectivity Unsuccessfully" }, JsonRequestBehavior.AllowGet);
            }
        }

        public static bool TesKoneksi(string conString)
        {
            //string conString = GetStringConnection(); //ConfigurationManager.ConnectionStrings["DefaultConnectionIBM"].ConnectionString;

            using (iDB2Connection iConn = new iDB2Connection(conString))
            {
                Logger logger = new Logger();
                
                try
                {
                    iConn.Open();
                    iConn.Close();
                    return true;
                }
                catch (iDB2Exception ex)
                {
                    iConn.Close();
                    logger.Error(ex.Message);
                    return false;
                }
            }
        }
    }
}