using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UploadGL.Helpers;
using UploadGL.Models;

namespace UploadGL.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string Username, string password)
        {

            List<UserModel> listUser = new List<UserModel>();
            UserJsonHelper userData = new UserJsonHelper();
            listUser = JsonConvert.DeserializeObject<List<UserModel>>(userData.Read());

            UserModel users = listUser.FirstOrDefault(x => x.Username == Username);
            //UserModel pass = user.FirstOrDefault(x => x.Password == password);

            if (users != null)
            {
                var pass = StringEncryptor.Encrypt(password);
                if (Username.Equals(users.Username) && users.Password.Equals(pass))
                {
                    Session["Username"] = Username;
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ViewBag.error = "Salah Username atau Password";
                    return View("Index");
                }
            }
            else
            {
                ViewBag.error = "Salah Username atau Password";
                return View("Index");

            }
        }

        [HttpGet]
        public ActionResult Logout()
        {
            Session.Remove("Username");
            return RedirectToAction("Index");
        }

    }
}
