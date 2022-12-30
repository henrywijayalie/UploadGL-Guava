using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web;
using Hangfire;
using Hangfire.Storage;
using IBM.Data.DB2.iSeries;
using Newtonsoft.Json;
using UploadGL.Helpers;

namespace UploadGL
{
    public static class MainProcess
    {
        public static bool isRun;
        public static string DateCore;
        public static int noRec;
        public static bool isInvalidProcess()
        {
            return isRun;
        }
        public static bool isWINCoreEOD()
        {
            string conString = ConnectionHelper.GetStringConnection();// ConfigurationManager.ConnectionStrings["DefaultConnectionIBM"].ConnectionString;
            string value = string.Empty;
            bool eod = false;
            try
            {
                using (iDB2Connection iConn = new iDB2Connection(conString))
                {
                    iDB2Command cmd2 = iConn.CreateCommand();
                    cmd2.CommandText = "select WINPROC from winpf";
                    cmd2.CommandTimeout = 0;
                    iConn.Open();
                    iDB2DataReader reader = cmd2.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            value = (string)reader["WINPROC"];

                            if (value == "Y")
                            {
                                eod = true;
                            }
                        }
                    }

                    reader.Close();
                    iConn.Close();
                }

                return eod;
            }
            catch (Exception ex)
            {
                isRun = false;
                TextBuffer.WriteError("Error: " + ex.Message);

                throw new InvalidOperationException(ex.ToString());
            }
        }
        public static DateTime CurrentDateCore()
        {
            string conString = ConnectionHelper.GetStringConnection(); //ConfigurationManager.ConnectionStrings["DefaultConnectionIBM"].ConnectionString;
            decimal value;
            DateTime nValue;
            nValue = new DateTime();
            try
            {
                using (iDB2Connection iConn = new iDB2Connection(conString))
                {
                    iDB2Command cmd2 = iConn.CreateCommand();
                    cmd2.CommandText = "select wincdat from winpf";
                    cmd2.CommandTimeout = 0;
                    iConn.Open();
                    iDB2DataReader reader = cmd2.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            value = (decimal)reader["WINCDAT"];

                            DateTime dt;
                            dt = new DateTime();
                            dt = DateTime.ParseExact(value.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture);
                            nValue = dt;//.ToString("yyyy-MM-dd");
                        }
                    }

                    reader.Close();
                    iConn.Close();
                }

                return nValue;
            }
            catch (Exception ex)
            {
                isRun = false;
                TextBuffer.WriteError("Error: " + ex.Message);

                throw new InvalidOperationException(ex.ToString());
            }
        }
        [DisplayName("{0}")]
        [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public static void ProceedBySchedule()
        {
            TextBuffer.WriteLine("Processed by Automatic Scheduler");

            if (isInvalidProcess() == false)
            {
                if (isWINCoreEOD() == false)
                {
                    //RunProcess(GetFileName());
                    RunProcess();
                }
                else
                {
                    TextBuffer.WriteError("ERROR : PROCESS CANNOT CONTINUE DURING EOD PROCESS");
                }
            }
            else
            {
                TextBuffer.WriteLine("");
                TextBuffer.WriteLine("SENDING NEW BATCH CANNOT CONTINUED ... PREVIOUS JOBS STILL RUNNING!");
                TextBuffer.WriteLine("");
            }
        }

        public static void RunProcess()
        {
            DateCore = CurrentDateCore().ToString("yyyyMMdd");
            noRec = 0;

            string fileIntraday = System.Web.Hosting.HostingEnvironment.MapPath("~/FileIntraday/");
            string fileEOD = System.Web.Hosting.HostingEnvironment.MapPath("~/FileEOD/");

            if (isWINCoreEOD() == true)
            {
                TextBuffer.WriteError("ERROR : PROCESS CANNOT CONTINUE DURING EOD PROCESS");
            }
            else
            {
                string[] filesIntraday = Directory.GetFiles(fileIntraday);
                string[] filesEOD = Directory.GetFiles(fileEOD);

                List<string> listDirectory = new List<string>();

                if (filesIntraday.Length > 0 )
                {
                    var intr = filesIntraday.ToList<string>();
                    listDirectory.AddRange(intr);
                }
                if (filesEOD.Length > 0)
                {
                    var eod = filesEOD.ToList<string>();
                    listDirectory.AddRange(eod);
                }

                foreach (var i in listDirectory)
                {
                    TextBuffer.WriteLine(i);
                }
            }
        }

        public static string GetVersion()
        {
            Assembly assem = Assembly.GetExecutingAssembly();
            AssemblyName aName = assem.GetName();
            return aName.Version.ToString();
        }
    }
}