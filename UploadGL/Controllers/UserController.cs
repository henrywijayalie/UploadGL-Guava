using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using UploadGL.Helpers;
using UploadGL.Models;

namespace UploadGL.Controllers
{
    public class UserController : Controller
    {
        // GET: User
        public ActionResult List ()
        {
            return View();
        }
        public void Serialize2 ()
        {
            List<UserModel> listOfAccounts = new List<UserModel>();
            StreamReader sr = new StreamReader(System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/User.json"));
            listOfAccounts = JsonConvert.DeserializeObject<List<UserModel>>(sr.ReadToEnd());
            sr.Close();

            listOfAccounts.Add(new UserModel { UserID = 1, Username = "Admin", Password = "Admin" });
            string outputJSON = "";
            foreach (var item in listOfAccounts) {
                outputJSON += JsonConvert.SerializeObject(item, Formatting.Indented) + ",";
            }

            string setting = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/");
            System.IO.File.WriteAllText(setting + "User.json", "[" + outputJSON + "]");
        }

        public ActionResult GetData ()
        {
            //  Serialize2();
            StreamReader sr = new StreamReader(System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/User.json"));
            var users = JsonConvert.DeserializeObject<List<UserModel>>(sr.ReadToEnd());
            sr.Close();

            return Json(new { data = users }, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public ActionResult AddOrEdit (int id = 0)
        {
            List<UserModel> user = new List<UserModel>();
            UserJsonHelper readWrite = new UserJsonHelper();
            user = JsonConvert.DeserializeObject<List<UserModel>>(readWrite.Read());

            if (id == 0)
                return View(new UserModel());
            else {
                var datum = user.Where(x => x.UserID == id).SingleOrDefault();
                var data = new UserModel() {
                    UserID = datum.UserID,
                    Password = StringEncryptor.Decrypt(datum.Password),
                    Username = datum.Username
                };
                return View(data);
            }
        }

        [HttpPost]
        public ActionResult AddOrEdit (UserModel usermodel)
        {
            List<UserModel> user = new List<UserModel>();
            UserJsonHelper readWrite = new UserJsonHelper();
            user = JsonConvert.DeserializeObject<List<UserModel>>(readWrite.Read());

            UserModel users = user.FirstOrDefault(x => x.UserID == usermodel.UserID);

            if (users == null) {
                UserModel usrnm = user.FirstOrDefault(x => x.Username == usermodel.Username);
                if (usrnm == null) {
                    user.Add(new UserModel { UserID = CommonHelper.RandomNumber(1, 99999), Username = usermodel.Username, Password = StringEncryptor.Encrypt(usermodel.Password) });
                }
                else {
                    return Json(new { success = true, message = "Username Already Exist." }, JsonRequestBehavior.AllowGet);
                }

                //user.Add(usermodel);
            }
            else {
                int index = user.FindIndex(x => x.UserID == usermodel.UserID);
                user[index].UserID = usermodel.UserID;
                user[index].Password = StringEncryptor.Encrypt(usermodel.Password);
            }

            string jSONString = JsonConvert.SerializeObject(user);
            readWrite.Write(jSONString);

            return Json(new { success = true, message = "Updated Successfully" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult Delete (int id)
        {
            List<UserModel> user = new List<UserModel>();
            UserJsonHelper readWrite = new UserJsonHelper();
            user = JsonConvert.DeserializeObject<List<UserModel>>(readWrite.Read());

            int index = user.FindIndex(x => x.UserID == id);
            user.RemoveAt(index);

            string jSONString = JsonConvert.SerializeObject(user);
            readWrite.Write(jSONString);


            return Json(new { success = true, message = "Deleted Successfully" }, JsonRequestBehavior.AllowGet);

        }
    }
}