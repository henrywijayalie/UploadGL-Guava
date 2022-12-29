using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace UploadGL.Helpers
{
    public class SettingJsonHelper
    {
        public string Read()
        {
            string path = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/Setting.json");
            string jsonResult;

            using (StreamReader streamReader = new StreamReader(path))
            {
                jsonResult = streamReader.ReadToEnd();
            }
            return jsonResult;
        }

        public void Write(string JsonString)
        {
            string path = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/Setting.json");

            using (var streamWriter = File.CreateText(path))
            {
                streamWriter.Write(JsonString);
                streamWriter.Close();
            }
        }
    }
}