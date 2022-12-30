using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using UploadGL.Models;

namespace UploadGL.Helpers
{
    public class ConnectionHelper
    {
        public static string GetStringConnectionLog()
        {
            string conString = string.Empty;
            StreamReader sr = new StreamReader(System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/Setting.json"));
            var sets = JsonConvert.DeserializeObject<List<SettingModel>>(sr.ReadToEnd());
            sr.Close();

            var decoder = sets.Where(x => x.Id == 1).SingleOrDefault();

            string UserID = decoder.UserID;
            string Password = StringEncryptor.Decrypt(decoder.Password);
            string DataSource = decoder.DataSource;
            string Library = decoder.DefaultCollection;
            string conx = string.Format("Host IP Machine = {0}; User ID = {1}; Unit = {2};", DataSource, UserID, Library);
            return conx;
        }

        public static string GetStringConnection()
        {
            string conString = string.Empty;
            StreamReader sr = new StreamReader(System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/Setting.json"));
            var sets = JsonConvert.DeserializeObject<List<SettingModel>>(sr.ReadToEnd());
            sr.Close();
            var decoder = sets.Where(x => x.Id == 1).SingleOrDefault();

            //conString = ConfigurationManager.ConnectionStrings["DefaultConnectionIBM"].ConnectionString;
            // iDB2ConnectionStringBuilder decoder = new iDB2ConnectionStringBuilder(conString);

            string UserID = decoder.UserID;
            string Password = StringEncryptor.Decrypt(decoder.Password);
            string DataSource = decoder.DataSource;
            string Library = decoder.DefaultCollection;

            string conx = string.Format("Data Source = {0}; User ID = {1}; Password = {2}; Default Collection = {3}; DataCompression=true;  Pooling=false;", DataSource, UserID, Password, Library);
            return conx;
        }


        public static string GetOledbStringConnection()
        {
            string conString = string.Empty;
            StreamReader sr = new StreamReader(System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/Setting.json"));
            var sets = JsonConvert.DeserializeObject<List<SettingModel>>(sr.ReadToEnd());

            sr.Close();
            var decoder = sets.Where(x => x.Id == 1).SingleOrDefault();

            //conString = ConfigurationManager.ConnectionStrings["DefaultConnectionIBM"].ConnectionString;
            // iDB2ConnectionStringBuilder decoder = new iDB2ConnectionStringBuilder(conString);

            string UserID = decoder.UserID;
            string Password = StringEncryptor.Decrypt(decoder.Password);
            string DataSource = decoder.DataSource;
            string Library = decoder.DefaultCollection;
            string conx = string.Format("Provider= IBMDA400.DataSource.1;Data Source = {0}; User ID = {1}; Password = {2};Catalog Library List = {3}; ", DataSource, UserID, Password, Library);

            return conx;
        }
    }
}