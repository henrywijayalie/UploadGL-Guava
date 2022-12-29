using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace UploadGL.Helpers
{
    public class ParameterJsonHelper
    {
        public string Read()
        {
            string path = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/Parameter.json");
            string jsonResult;

            using (StreamReader streamReader = new StreamReader(path))
            {
                jsonResult = streamReader.ReadToEnd();

            }
            return jsonResult;
        }

        public void Write(string jsonString)
        {
            string path = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/Parameter.json");

            using (var streamWriter = File.CreateText(path))
            {
                streamWriter.Write(jsonString);
                streamWriter.Close();
            }
        }
    }
}