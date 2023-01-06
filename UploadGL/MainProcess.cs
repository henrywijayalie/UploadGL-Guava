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
using UploadGL.Models;

namespace UploadGL
{
    public static class MainProcess
    {
        public static bool isRun;
        public static bool IsUpload;
        public static bool available;
        public static string DateCore;
        public static int noRec;
        static string dateGL;
        public static string NoBatch;
        public static string SeqBatch;
        static int lineCount;
        static System.Timers.Timer waitval = new System.Timers.Timer();
        static System.Timers.Timer waitpost = new System.Timers.Timer();

        public static bool InvalidFileCSV = false;

        public static Logger logger = new Logger();
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
        public static string NameBankCore()
        {
            string conString = ConnectionHelper.GetStringConnection();// ConfigurationManager.ConnectionStrings["DefaultConnectionIBM"].ConnectionString;
            string value = string.Empty;
            try
            {
                using (iDB2Connection iConn = new iDB2Connection(conString))
                {

                    iDB2Command cmd2 = iConn.CreateCommand();
                    cmd2.CommandText = "select winname from winpf";
                    cmd2.CommandTimeout = 0;
                    iConn.Open();
                    iDB2DataReader reader = cmd2.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            value = (string)reader["WINNAME"];
                        }
                    }

                    reader.Close();
                    iConn.Close();
                }

                return value;
            }
            catch (Exception ex)
            {
                isRun = false;
                TextBuffer.WriteError("Error: " + ex.Message);

                throw new InvalidOperationException(ex.ToString());
            }
        }
        public static string QuoteStr(string value)
        {
            //string valuex = "\"" + value + "\"";
            string valuex = value;
            return valuex;
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
            TextBuffer.WriteLine("");
            TextBuffer.WriteLine("Processed by Automatic Scheduler");
            TextBuffer.WriteLine("");

            if (isInvalidProcess() == false)
            {
                if (isWINCoreEOD() == false)
                {
                    RunProcess(GetFileName());
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

        public static string CheckHeaderNoBatch()
        {
            string conString = ConnectionHelper.GetStringConnection(); // ConfigurationManager.ConnectionStrings["DefaultConnectionIBM"].ConnectionString;
            string value = string.Empty;

            try
            {
                using (iDB2Connection iConn = new iDB2Connection(conString))
                {
                    iDB2Command cmd2 = iConn.CreateCommand();
                    cmd2.CommandText = "select TBRSBCH from TBRPF where tbrtype = 'H' and TBRFLAG = 'P'";
                    iConn.Open();
                    iDB2DataReader reader = cmd2.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            value = (string)reader["TBRSBCH"];
                        }
                    }
                    reader.Close();
                    iConn.Close();
                }
            }
            catch (Exception ex)
            {
                isRun = false;
                TextBuffer.WriteError("Error: " + ex.Message);

                throw new InvalidOperationException(ex.ToString());
            }
            return value;
        }

        public static bool CheckValidDateGL(string filename)
        {
            StreamReader sr = new StreamReader(filename);
            try
            {
                string headerLine = sr.ReadLine();
                string line = sr.ReadLine();
                string[] strRow = line.Split('|');

                dateGL = strRow[4];

                if (dateGL == DateCore)
                {
                    isRun = false;
                    sr.Close();
                    return true;

                }
                else
                {
                    isRun = false;
                    sr.Close();
                    return false;
                }
            }
            catch (Exception ex)
            {
                isRun = false;
                sr.Close();
                return false;
            }
        }
        public static Int32 CheckOnProcessBroken()
        {
            string conString = ConnectionHelper.GetStringConnection(); //ConfigurationManager.ConnectionStrings["DefaultConnectionIBM"].ConnectionString;
            Int32 value = 0;

            try
            {
                using (iDB2Connection iConn = new iDB2Connection(conString))
                {
                    iDB2Command cmd2 = iConn.CreateCommand();
                    cmd2.CommandText = "select count(*) as COUNT from TBRPF where tbrtype <> 'H' ";
                    iConn.Open();
                    iDB2DataReader reader = cmd2.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            value = (Int32)reader["COUNT"];
                        }
                    }

                    reader.Close();
                    iConn.Close();
                }
                available = true;
                return value;

            }
            catch (Exception ex)
            {
                TextBuffer.WriteError("Error: " + ex.Message);
                available = false;
                isRun = false;
                throw new InvalidOperationException(ex.ToString());
            }
        }
        public static bool ExistValHeader()
        {
            string conString = ConnectionHelper.GetStringConnection(); //ConfigurationManager.ConnectionStrings["DefaultConnectionIBM"].ConnectionString;
            Int32 value = 0;
            bool state = false;

            try
            {
                using (iDB2Connection iConn = new iDB2Connection(conString))
                {
                    iDB2Command cmd2 = iConn.CreateCommand();
                    cmd2.CommandText = "select count(*) as COUNT from TBRPF where tbrtype = 'H' and TBRFLAG = 'V' and TBRPROC  in ('03','04','')";
                    iConn.Open();
                    iDB2DataReader reader = cmd2.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            value = (Int32)reader["COUNT"];

                            if (value > 0)
                            {
                                state = true;
                            }
                            else
                            {
                                state = false;
                            }
                        }
                    }

                    reader.Close();
                    iConn.Close();

                    return state;
                }
            }
            catch (Exception ex)
            {
                isRun = false;
                TextBuffer.WriteError("Error: " + ex.Message);

                throw new InvalidOperationException(ex.ToString());
            }
        }
        public static bool ExistPostHeader()
        {
            string conString = ConnectionHelper.GetStringConnection(); //ConfigurationManager.ConnectionStrings["DefaultConnectionIBM"].ConnectionString;
            Int32 value = 0;
            bool state = false;

            try
            {
                using (iDB2Connection iConn = new iDB2Connection(conString))
                {
                    iDB2Command cmd2 = iConn.CreateCommand();
                    cmd2.CommandText = "select count(*) as COUNT from TBRPF where tbrtype = 'H' and TBRFLAG = 'P' and TBRPROC  in ('03')";
                    iConn.Open();
                    iDB2DataReader reader = cmd2.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            value = (Int32)reader["COUNT"];

                            if (value > 0)
                            {
                                state = true;
                            }
                            else
                            {
                                state = false;
                            }
                        }
                    }

                    reader.Close();
                    iConn.Close();

                    return state;
                }
            }
            catch (Exception ex)
            {
                isRun = false;
                TextBuffer.WriteError("Error: " + ex.Message);

                throw new InvalidOperationException(ex.ToString());
            }
        }
        public static string FlagHeaderVal()
        {
            string conString = ConnectionHelper.GetStringConnection(); //ConfigurationManager.ConnectionStrings["DefaultConnectionIBM"].ConnectionString;
            string value = string.Empty;

            try
            {
                using (iDB2Connection iConn = new iDB2Connection(conString))
                {

                    iDB2Command cmd2 = iConn.CreateCommand();
                    cmd2.CommandText = "select TBRPOST from tbrpf where tbrtype = 'H'";
                    cmd2.CommandTimeout = 0;
                    iConn.Open();
                    iDB2DataReader reader = cmd2.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            value = (string)reader["TBRPOST"];
                        }
                    }

                    reader.Close();
                    iConn.Close();
                }
                return value;
            }
            catch (Exception ex)
            {
                isRun = false;
                TextBuffer.WriteError("Error: " + ex.Message);

                throw new InvalidOperationException(ex.ToString());
            }
        }
        public static bool DataDetailGLUpload(string filename, bool isEODFile)
        {
            using (var progress = new ProgressBar())
            {
                List<TbrpfModel> listDataTBRPF = new List<TbrpfModel>();
                var lines = File.ReadLines(filename);
                // decimal lineCount = File.ReadLines(filename).Count() - 1;
                decimal lineCount = lines.Count();

                TextBuffer.WriteLine("Proses Batch No : " + NoBatch);
                // TextBuffer.WriteLine("Proses Upload Data GL... ");
                TextBuffer.WriteLine("Clear File TBRPF");
                DBExec("DELETE FROM TBRPF");

                int batchSize;
                // bool isValid = int.TryParse(batchSizeSetting, out batchSize);
                // if (!isValid)
                batchSize = 10000;
                var pageNumbers = Math.Ceiling(lineCount / batchSize);

                for (int j = 0; j < pageNumbers; j++)
                {
                    int skipNumber = j * batchSize + 1; // add 1 to skip because there's header line
                    var linesPerPage = lines.Skip(skipNumber).Take(batchSize).ToList();

                    InsertDataToTBRPF(linesPerPage, DateCore, isEODFile);
                }
                IsUpload = true;
            }
            return true;
        }

        public static void InsertDataToTBRPF(List<string> contents, string dateCore, bool isEODFile)
        {
            string connectionString = ConnectionHelper.GetStringConnection();
            int msg = 0;
            int recno = 0;

            using (iDB2Connection conn = new iDB2Connection(connectionString))
            {
                try
                {
                    for (int i = 0; i < contents.Count; i++)
                    {
                        var item = contents[i].Split('|');
                        var cnt = contents.Count;


                        if (msg < 1)
                        {
                            if (cnt < 1000)
                            {
                                TextBuffer.WriteLine("Uploading Data Record ..........." + cnt);
                            }
                            else
                            {
                                TextBuffer.WriteLine("Uploading Data Record ..........." + item[2]);
                            }
                        }

                        var sqlText = "INSERT INTO TBRPF (TBRBRCP, TBRSBCH, TBRSEQN, TBRBRCO, TBRDATE, TBRTIME, TBRPODT, TBRREFN, TBRCHRT, TBRDSC1, TBRACB1, TBRACN1, TBRTRC1, TBRDBC1, TBRCYCO, TBRAMNT, TBREXRT, TBRBASE, TBRTYPE, TBRUSIN, TBRDSIN, TBRDTIN, TBRTMIN) VALUES " +
                            "(\'0000\',\'" + NoBatch.ToString() + "\', " + (string.IsNullOrEmpty(item[2].ToString()) ? "0" : item[2].ToString()) + ", \'" + (string.IsNullOrEmpty(item[3].ToString()) ? "" : item[3].ToString()) + "\', \'" + dateCore + "\', " + item[5] + ", " + (item[6].ToString() == "" ? 0 : Convert.ToDecimal(item[6])) + ", \'" + item[7] + "\', \'" + item[8] + "\', \'" + item[9] + "\', \'" + item[10] + "\', \'" + item[11] + "\', \'" + item[12] + "\', \'" + item[13] + "\', \'" + item[14] + "\', " + item[15] + "," + item[16] + "," + item[17] + ", \'" + item[18] + "\', \'" + item[19] + "\', \'" + item[20] + "\', " + item[21] + "," + item[22] + ") ";

                        iDB2Command cmd = new iDB2Command(sqlText.ToString())
                        {
                            CommandType = CommandType.Text,
                            Connection = conn,
                            CommandTimeout = 0
                        };

                        conn.Open();

                        msg++;
                        noRec++;
                        recno++;



                        //cmd.Parameters.AddWithValue("@TBRBRCP", DbType.String);
                        //cmd.Parameters.AddWithValue("@TBRSBCH", DbType.String);
                        //cmd.Parameters.AddWithValue("@TBRSEQN", DbType.Decimal);
                        //cmd.Parameters.AddWithValue("@TBRBRCO", DbType.String);
                        //cmd.Parameters.AddWithValue("@TBRDATE", DbType.Decimal);
                        //cmd.Parameters.AddWithValue("@TBRTIME", DbType.Decimal);
                        //cmd.Parameters.AddWithValue("@TBRPODT", DbType.Decimal);
                        //cmd.Parameters.AddWithValue("@TBRREFN", DbType.String);
                        //cmd.Parameters.AddWithValue("@TBRCHRT", DbType.String);
                        //cmd.Parameters.AddWithValue("@TBRDSC1", DbType.String);
                        //cmd.Parameters.AddWithValue("@TBRACB1", DbType.String);
                        //cmd.Parameters.AddWithValue("@TBRACN1", DbType.String);
                        //cmd.Parameters.AddWithValue("@TBRTRC1", DbType.String);
                        //cmd.Parameters.AddWithValue("@TBRDBC1", DbType.String);
                        //cmd.Parameters.AddWithValue("@TBRCYCO", DbType.String);
                        //cmd.Parameters.AddWithValue("@TBRAMNT", DbType.Decimal);
                        //cmd.Parameters.AddWithValue("@TBREXRT", DbType.Decimal);
                        //cmd.Parameters.AddWithValue("@TBRBASE", DbType.Decimal);
                        //cmd.Parameters.AddWithValue("@TBRTYPE", DbType.String);
                        //cmd.Parameters.AddWithValue("@TBRUSIN", DbType.String);
                        //cmd.Parameters.AddWithValue("@TBRDSIN", DbType.String);
                        //cmd.Parameters.AddWithValue("@TBRDTIN", DbType.Decimal);
                        //cmd.Parameters.AddWithValue("@TBRTMIN", DbType.Decimal);

                        //cmd.Parameters[0].Value = "0000"; //TBRBRCP
                        //cmd.Parameters[1].Value = NoBatch.ToString(); //item[1]; //TBRSBCH //INTRADAY BLANK
                        //cmd.Parameters[2].Value = item[2].ToString() ?? ""; //TBRSEQN //INTRADAY BLANK
                        //cmd.Parameters[3].Value = item[3].ToString() ?? ""; //TBRBRCO //INTRADAY BLANK
                        //cmd.Parameters[4].Value = dateCore; //TBRDATE
                        //cmd.Parameters[5].Value = item[5]; //TBRTIME
                        //cmd.Parameters[6].Value = item[6] == "" ? 0 : Convert.ToDecimal(item[6]); //TBRPODT
                        //cmd.Parameters[7].Value = item[7] == "" ? "0" : item[7]; //TBRREFN
                        //cmd.Parameters[8].Value = item[8]; //TBRCHRT //AN
                        //cmd.Parameters[9].Value = item[9]; //TBRDSC1
                        //cmd.Parameters[10].Value = item[10].ToString() ?? ""; //TBRACB1 // BLANK
                        //cmd.Parameters[11].Value = item[11]; //TBRACN1
                        //cmd.Parameters[12].Value = item[12]; //TBRTRC1
                        //cmd.Parameters[13].Value = item[13]; //TBRDBC1
                        //cmd.Parameters[14].Value = item[14]; //TBRCYCO
                        //cmd.Parameters[15].Value = Convert.ToDecimal(item[15]); //ConversionExtensions.ToInt(item[15]); //TBRAMNT
                        //cmd.Parameters[16].Value = Convert.ToDecimal(item[16]); //TBREXRT
                        //cmd.Parameters[17].Value = Convert.ToDecimal(item[17]); //TBRBASE
                        //cmd.Parameters[18].Value = item[18]; //TBRTYPE
                        //cmd.Parameters[19].Value = item[19]; //TBRUSIN
                        //cmd.Parameters[20].Value = item[20]; //CommonHelper.RandomDSPBatch() + item[3].PadLeft(6, '0'); //TBRDSIN
                        //cmd.Parameters[21].Value = item[21]; //TBRDTIN
                        //cmd.Parameters[22].Value = item[22]; //TBRTMIN

                        cmd.ExecuteNonQuery();
                        cmd.Parameters.Clear();

                        conn.Close();
                    }

                }
                catch (Exception ex)
                {
                    IsUpload = false;
                    conn.Close();
                    isRun = false;
                    TextBuffer.WriteError("Error Line: " + recno + " - " + ex.Message);


                    throw new InvalidOperationException(ex.ToString());
                }
            }
        }

        public static void HeaderGLValidate(bool isEODFile)
        {
            string connectionString = ConnectionHelper.GetStringConnection();//ConfigurationManager.ConnectionStrings["DefaultConnectionIBM"].ConnectionString;
            int msg = 0;

            using (iDB2Connection conn = new iDB2Connection(connectionString))
            {
                try
                {
                    string sqlText2 = "INSERT INTO TBRPF (TBRBRCP,TBRSBCH,TBRDATE,TBRTYPE,TBRFLAG,TBRPROC,TBRPOST,TBRAMNT) VALUES";
                    sqlText2 += "(@TBRBRCP,@TBRSBCH,@TBRDATE,@TBRTYPE,@TBRFLAG,@TBRPROC,@TBRPOST,@TBRAMNT)";

                    iDB2Command cmd = new iDB2Command(sqlText2)
                    {
                        CommandType = CommandType.Text,
                        Connection = conn,
                        CommandTimeout = 0
                    };

                    conn.Open();
                    cmd.Parameters.AddWithValue("@TBRBRCP", DbType.String);
                    cmd.Parameters.AddWithValue("@TBRSBCH", DbType.String);
                    cmd.Parameters.AddWithValue("@TBRDATE", DbType.String);
                    cmd.Parameters.AddWithValue("@TBRTYPE", DbType.String);
                    cmd.Parameters.AddWithValue("@TBRFLAG", DbType.String);
                    cmd.Parameters.AddWithValue("@TBRPROC", DbType.String);
                    cmd.Parameters.AddWithValue("@TBRPOST", DbType.String);
                    cmd.Parameters.AddWithValue("@TBRAMNT", DbType.String);

                    cmd.Parameters[0].Value = "0000";
                    cmd.Parameters[1].Value = NoBatch;
                    cmd.Parameters[2].Value = DateCore;
                    cmd.Parameters[3].Value = "H";
                    cmd.Parameters[4].Value = isEODFile ? "V" : "E"; // V = validate (EOD), E = intraday
                    cmd.Parameters[5].Value = "";
                    cmd.Parameters[6].Value = "";
                    cmd.Parameters[7].Value = lineCount - 1;

                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
                catch (Exception ex)
                {
                    IsUpload = false;
                    conn.Close();
                    isRun = false;
                    TextBuffer.WriteError("Error: " + ex.Message);
                    throw new InvalidOperationException(ex.ToString());
                }
            }
        }
        public static void HeaderPostingGL(bool isEODFile)
        {
            string conString = ConnectionHelper.GetStringConnection(); //ConfigurationManager.ConnectionStrings["DefaultConnectionIBM"].ConnectionString;
            try
            {
                using (iDB2Connection iConn = new iDB2Connection(conString))
                {

                    string sqlText2 = "INSERT INTO TBRPF(TBRBRCP, TBRSBCH, TBRDATE, TBRTYPE, TBRFLAG, TBRPROC, TBRPOST, TBRAMNT) VALUES";
                    sqlText2 += "(@TBRBRCP,@TBRSBCH,@TBRDATE,@TBRTYPE,@TBRFLAG,@TBRPROC,@TBRPOST,@TBRAMNT)";
                    List<iDB2Parameter> paraList = new List<iDB2Parameter>()
                    {
                         new iDB2Parameter("@TBRBRCP","0000"), 
                         new iDB2Parameter("@TBRSBCH",NoBatch),
                         new iDB2Parameter("@TBRDATE",DateCore), 
                         new iDB2Parameter("@TBRTYPE", "H"),
                         new iDB2Parameter("@TBRFLAG", "P"),
                         new iDB2Parameter("@TBRPROC", ""), 
                         new iDB2Parameter("@TBRPOST", ""), 
                         new iDB2Parameter("@TBRAMNT", lineCount - 1)
                    };

                    iDB2Command sqlCommand2 = iConn.CreateCommand();
                    sqlCommand2.CommandText = sqlText2;
                    sqlCommand2.Parameters.AddRange(paraList.ToArray());

                    iConn.Open();
                    iDB2Transaction trans = iConn.BeginTransaction();
                    sqlCommand2.Transaction = trans;
                    try
                    {
                        sqlCommand2.ExecuteNonQuery();
                        trans.Commit();
                        iConn.Close();
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        iConn.Close();
                        isRun = false;
                        TextBuffer.WriteLine("Error: " + ex.Message);
                        throw new InvalidOperationException(ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                TextBuffer.WriteError("Error: " + ex.Message);
                isRun = false;
                throw new InvalidOperationException(ex.ToString());
            }
        }

        public static void DBExec(string sqlText)
        {
            string conString = ConnectionHelper.GetStringConnection();

            try
            {
                using (iDB2Connection iConn = new iDB2Connection(conString))
                {
                    iDB2Command cmd2 = iConn.CreateCommand();
                    cmd2.CommandText = sqlText;
                    cmd2.CommandTimeout = 0;
                    iConn.Open();
                    iDB2Transaction trans = iConn.BeginTransaction();
                    cmd2.Transaction = trans;

                    try
                    {
                        cmd2.ExecuteNonQuery();
                        trans.Commit();
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        throw new InvalidOperationException(ex.ToString());
                    }

                    iConn.Close();
                }
            }
            catch (Exception ex)
            {
                isRun = false;
                TextBuffer.WriteError("Error: " + ex.Message);

                throw new InvalidOperationException(ex.ToString());
            }
        }

        public static void RunProcess(string filename)
        {
            try
            {
                string fileTerKirim = System.Web.Hosting.HostingEnvironment.MapPath("~/FileSukses/");
                DateCore = CurrentDateCore().ToString("yyyyMMdd");
                noRec = 0;
                string filex = Path.GetFileNameWithoutExtension(filename);

                if (isWINCoreEOD() == true)
                {
                    TextBuffer.WriteError("ERROR : PROCESS CANNOT CONTINUE DURING EOD PROCESS");
                }
                else
                {
                    isRun = false;
                    IsUpload = false;
                    bool isEODFile = filex.Contains("EOD");
                    NoBatch = CommonHelper.RandomBatch(isEODFile); // set nomor batch
                    if (!filename.IsEmpty())
                    {
                        DateTime startTime = DateTime.Now;
                        if (CheckValidFile(filename) == true)
                        {
                            if (CheckHeaderNoBatch() != NoBatch)
                            {
                                if (CheckValidDateGL(filename.Trim()) == true)
                                {
                                    isRun = true;

                                    TextBuffer.WriteLine("Nama File : " + filex.Trim() + ".csv");
                                    lineCount = File.ReadLines(filename.Trim()).Count();

                                    TextBuffer.WriteLine("Jumlah Data : " + (lineCount - 1));
                                    TextBuffer.WriteLine("Tanggal & Jam Mulai Proses : " + startTime + "\n");

                                    DataDetailGLUpload(filename.Trim(), isEODFile);

                                    if (IsUpload == true)
                                    {
                                        // TextBuffer.WriteLine("Create data Header Validation... ");
                                        //HeaderDataGL();
                                        HeaderGLValidate(isEODFile);
                                        TextBuffer.WriteLine("Create data Header Validation... Done.");
                                        // LogSuccess("Create data Header... Done", log);
                                    }

                                    TextBuffer.WriteLine("Proses Upload Data GL selesai.");
                                    // LogSuccess("Proses Upload Data GL selesai.", log);

                                    int msg = 0;
                                    int msg1 = 0;
                                    int msg2 = 0;
                                    int msg3 = 0;

                                    waitval.Interval = GetTimeOutVal() * 60000;//one minute
                                    waitval.Elapsed += new System.Timers.ElapsedEventHandler(waitval_Tick);

                                    waitpost.Interval = GetTimeOutPost() * 60000;//one minute
                                    waitpost.Elapsed += new System.Timers.ElapsedEventHandler(waitpost_Tick);

                                    while (IsUpload == true)
                                    {
                                        switch (FlagHeaderProc())
                                        {
                                            case "01":
                                                if (msg1 < 1)
                                                {
                                                    waitval.Stop();
                                                    TextBuffer.WriteLine("Proses validasi sedang berjalan");
                                                    waitval.Start();
                                                    //       LogSuccess("Proses validasi sedang berjalan.", log);
                                                }
                                                if (FlagHeaderVal() == "Y")
                                                {
                                                    waitval.Stop();
                                                }
                                                msg1++;
                                                break;
                                            case "02":
                                                waitval.Stop();
                                                if (FlagHeaderVal() == "Y")
                                                {
                                                    TextBuffer.WriteLine("Proses Validasi Data Selesai");
                                                    //      LogSuccess("Proses Validasi Data Selesai.", log);
                                                    //HeaderPostingGL();
                                                    HeaderPostingGL(isEODFile);

                                                    while (IsUpload == true)
                                                    {
                                                        switch (FlagHeaderPosting())
                                                        {
                                                            case "01":
                                                                if (msg2 < 1)
                                                                {
                                                                    waitpost.Stop();
                                                                    TextBuffer.WriteLine("Proses Validasi Posting Sedang Berjalan");
                                                                    waitpost.Start();
                                                                    //            LogSuccess("Proses Validasi Posting sedang berjalan.", log);
                                                                }
                                                                if (FlagHeaderPost() == "Y")
                                                                {
                                                                    waitpost.Close();
                                                                }

                                                                msg2++;

                                                                break;
                                                            case "02":

                                                                waitpost.Close();
                                                                if (FlagHeaderPost() == "Y")
                                                                {
                                                                    ReportDetailPostSukses(filex, startTime, DateTime.Now, FlagHeaderNoBatch(), FlagHeaderProc());
                                                                    ReportDetailPostPenampunganSukses(filex, startTime, DateTime.Now, FlagHeaderNoBatch(), FlagHeaderProc());
                                                                    ReportDetailPostError(filex, startTime, DateTime.Now, FlagHeaderNoBatch(), FlagHeaderProc());
                                                                    SummaryReportSuksesUpload(filex, startTime, DateTime.Now, FlagHeaderNoBatch(), FlagHeaderProc(), isEODFile);
                                                                    //        LogSuccess("Proses Posting Selesai.", log);
                                                                    TextBuffer.WriteLine("Proses Posting Selesai.");
                                                                    IsUpload = false;
                                                                }

                                                                break;
                                                            case "03":

                                                                waitpost.Close();
                                                                TextBuffer.WriteError("ERROR: Posting Validasi Data Error");
                                                                //  Console.ForegroundColor = ConsoleColor.White;
                                                                ReportHeaderPostingError(filex, startTime, DateTime.Now, FlagHeaderNoBatch(), FlagHeaderProc(), isEODFile);
                                                                //  SummaryReportErrorUpload(filex, startTime, DateTime.Now, FlagHeaderNoBatch(), FlagHeaderProc());

                                                                IsUpload = false;

                                                                break;

                                                            default:
                                                                if (msg3 < 1)
                                                                {
                                                                    TextBuffer.WriteLine("");
                                                                    TextBuffer.WriteLine("Menunggu Proses Posting di WINCORE... ");
                                                                    waitpost.Start();
                                                                    //        LogSuccess("Waiting Proses Posting di WINCORE....", log);
                                                                }
                                                                msg3++;
                                                                break;
                                                        }
                                                    }
                                                }
                                                break;
                                            case "03":

                                                waitval.Stop();
                                                TextBuffer.WriteError("ERROR: Validasi Total Mutasi Tidak Balance  ");
                                                ReportHeaderValError(filex, startTime, DateTime.Now, FlagHeaderNoBatch(), FlagHeaderProc(), isEODFile);
                                                //      LogSuccess("Validasi total mutasi tidak balance  ", log);

                                                IsUpload = false;
                                                break;
                                            case "04":
                                                waitval.Stop();
                                                TextBuffer.WriteError("ERROR: Validasi Detail Data");
                                                ReportDetailValError(filex, startTime, DateTime.Now, FlagHeaderNoBatch(), FlagHeaderProc(), isEODFile);
                                                //   LogSuccess("ERROR-Validasi Data Detail", log);

                                                IsUpload = false;
                                                break;
                                            default:
                                                if (msg < 1)
                                                {
                                                    TextBuffer.WriteLine("Menunggu proses pada WINCORE... ");
                                                    waitval.Start();
                                                    //        LogSuccess("Waiting proses di WINCORE....", log);
                                                }
                                                msg++;
                                                break;
                                        }
                                    }


                                    TextBuffer.WriteLine("Proses Upload Data GL Complete");
                                    TextBuffer.WriteLine("Tanggal & Jam Akhir Proses : " + DateTime.Now);
                                    TextBuffer.WriteLine("Lama Proses : " + (DateTime.Now - startTime).ToString(@"hh\:mm\:ss") + "\n");

                                    TextBuffer.WriteLine("================================================================================================================================== ");

                                    File.Delete(filename.Trim());
                                    //File.Delete(filename.Trim() + ".done");

                                    isRun = false;
                                    NoBatch = "";
                                }
                                else
                                {
                                    isRun = false;
                                    TextBuffer.WriteError(string.Format("Error: Upload File ({0}.csv) tanggal file ({1}) berbeda dengan tanggal sistem Wincore ({2})", filex, dateGL, CurrentDateCore().ToString("yyyyMMdd")));
                                    throw new InvalidOperationException(string.Format("Upload File ({0}.csv) tanggal file ({1}) berbeda dengan tanggal sistem Wincore ({2})", filex, dateGL, CurrentDateCore().ToString("yyyyMMdd")));

                                }
                            }
                            else
                            {
                                isRun = false;
                                TextBuffer.WriteError("Error: File: " + filename + ".csv No batch (" + NoBatch + ") harus berbeda dengan nomor batch sebelumnya");
                                throw new InvalidOperationException("File: " + filename + ".csv No batch (" + NoBatch + ") harus berbeda dengan nomor batch sebelumnya");
                            }
                        }
                        else
                        {
                            isRun = false;
                            throw new InvalidOperationException("File: " + filename + ".csv Tidak Ada");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                TextBuffer.WriteError("Something when wrong : \n" + ex.Message);
            }
        }
        public static bool CheckValidFile(string filename)
        {
            if (File.Exists(filename))
            {
                return true;
            }
            else
            {
                TextBuffer.WriteLine("Terjadi kesalahan pada sistem, file berikut ini tidak ditemukan saat pemrosesan : \n" + filename);
                return false;
            }
        }

        public static bool BlankValHeader()
        {
            string conString = ConnectionHelper.GetStringConnection();
            Int32 value = 0;
            bool state = false;

            try
            {
                using (iDB2Connection iConn = new iDB2Connection(conString))
                {
                    iDB2Command cmd2 = iConn.CreateCommand();
                    cmd2.CommandText = "select count(*) as COUNT from TBRPF where tbrtype = 'H' and TBRFLAG = 'V' and TBRPROC  in ('')";
                    iConn.Open();
                    iDB2DataReader reader = cmd2.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            value = (Int32)reader["COUNT"];

                            if (value > 0)
                            {
                                state = true;
                            }
                            else
                            {
                                state = false;
                            }
                        }
                    }

                    reader.Close();
                    iConn.Close();

                    return state;
                }
            }
            catch (Exception ex)
            {
                isRun = false;
                TextBuffer.WriteError("Error: " + ex.Message);

                throw new InvalidOperationException(ex.ToString());
            }
        }

        public static string GetFileName()
        {
            bool validDt = false;
            try
            {
                int recAwal = CheckOnProcessBroken();
                Thread.Sleep(2000);
                int recAkhir = CheckOnProcessBroken();

                if (recAwal == recAkhir)
                {
                    if (ExistValHeader() == true && ExistPostHeader() == false)
                    {
                        validDt = true;
                        //   ContinueProses(GetFile());
                        //   IsUpload = false;
                    }
                    else if (ExistValHeader() == true && ExistPostHeader() == true)
                    {
                        IsUpload = true;
                        ContinueProsesPost(GetFile());
                        IsUpload = false;
                        validDt = false;
                    }
                    else if (ExistValHeader() == false && ExistPostHeader() == false)
                    {
                        validDt = true;
                    }
                }
                else
                {
                    DBExec("DELETE FROM TBRPF");
                    validDt = true;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                // TextBuffer.WriteLine(ex.Message);
            }

            if (validDt)
            {
                return GetFile();
            }
            else
            {
                if (available == true && BlankValHeader() == true)
                {
                    TextBuffer.WriteLine("");
                    TextBuffer.WriteLine("PROCESS BY SCHEDULER CANNOT CONTINUE, ANOTHER JOB IS BEING PROCESS!");
                    TextBuffer.WriteLine("");
                }

                return "";
            }
        }
        public static string GetFile()
        {
            string fileIntraday = System.Web.Hosting.HostingEnvironment.MapPath("~/FileIntraday/");
            string fileEOD = System.Web.Hosting.HostingEnvironment.MapPath("~/FileEOD/");
            string[] filesIntraday = Directory.GetFiles(fileIntraday);
            string[] filesEOD = Directory.GetFiles(fileEOD);

            List<string> listDirectory = new List<string>();

            string filename = string.Empty;

            if (filesIntraday.Length > 0)
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
                var file = i;
                var filenamex = Path.GetFileName(file);
                //TextBuffer.WriteLine(filename);
                FileInfo fi = new FileInfo(filenamex);
                if (fi.Extension.ToLower() == ".csv")
                {
                    int lineCount = File.ReadLines(i).Count() - 1;

                    if (lineCount >= 99999)
                    {
                        TextBuffer.WriteLine("Error - File : " + filenamex + " record data harus lebih kecil dari 99,999 record");
                        InvalidFileCSV = true;
                        break;
                    }
                    else
                    {
                        filename = i;
                        break;
                    }
                }
            }
            return filename;
        }
        static private void waitpost_Tick(object sender, System.Timers.ElapsedEventArgs e)
        {
            TextBuffer.WriteLine("");
            TextBuffer.WriteError("PROSES POSTING TIDAK BERJALAN, CEK SP500 PASTIKAN A7 @@ATSBCHA7 *ACTIVE");
            TextBuffer.WriteLine("");
        }
        static private void waitval_Tick(object sender, System.Timers.ElapsedEventArgs e)
        {

            TextBuffer.WriteLine("");
            TextBuffer.WriteError("PROSES VALIDASI TIDAK BERJALAN, CEK SP500 PASTIKAN A7 @@ATSBCHA7 *ACTIVE");
            TextBuffer.WriteError("");

        }

        public static void ContinueProsesPost(string filename)
        {
            //string fileUpload = System.Web.Hosting.HostingEnvironment.MapPath("~/FileUpload/");
            string fileTerKirim = System.Web.Hosting.HostingEnvironment.MapPath("~/FileSukses/");
            if (filename.HasValue())
            {
                DateTime startTime = DateTime.Now;
                string filex = Path.GetFileNameWithoutExtension(filename);
                string[] fname = filex.Split('_');

                bool isEODFile = filex.Contains("EOD");
                int msg = 0;
                int msg2 = 0;

                waitpost.Interval = GetTimeOutPost() * 60000;//one minute
                waitpost.Elapsed += new System.Timers.ElapsedEventHandler(waitpost_Tick);


                while (IsUpload == true)
                {
                    switch (FlagHeaderPosting())
                    {
                        case "01":
                            if (msg < 1)
                            {
                                waitpost.Stop();
                                TextBuffer.WriteLine("Proses Validasi Posting Sedang Berjalan");
                                waitpost.Start();
                            }

                            if (FlagHeaderPost() == "Y")
                            {
                                waitpost.Stop();
                            }
                            msg++;

                            break;
                        case "02":
                            waitpost.Stop();
                            if (FlagHeaderPost() == "Y")
                            {
                                ReportDetailPostSukses(filex, startTime, DateTime.Now, FlagHeaderNoBatch(), FlagHeaderProc());
                                ReportDetailPostPenampunganSukses(filex, startTime, DateTime.Now, FlagHeaderNoBatch(), FlagHeaderProc());
                                ReportDetailPostError(filex, startTime, DateTime.Now, FlagHeaderNoBatch(), FlagHeaderProc());
                                SummaryReportSuksesUpload(filex, startTime, DateTime.Now, FlagHeaderNoBatch(), FlagHeaderProc(), isEODFile);
                                //        LogSuccess("Proses Posting Selesai.", log);
                                TextBuffer.WriteLine("Proses Posting Selesai.");
                                IsUpload = false;
                            }

                            break;
                        case "03":
                            waitpost.Stop();
                            TextBuffer.WriteError("ERROR: Posting Validasi Data Error");
                            ReportHeaderPostingError(filex, startTime, DateTime.Now, FlagHeaderNoBatch(), FlagHeaderProc(), isEODFile);
                            //  SummaryReportErrorUpload(filex, startTime, DateTime.Now, FlagHeaderNoBatch(), FlagHeaderProc());

                            IsUpload = false;
                            break;

                        default:
                            if (msg2 < 1)
                            {
                                TextBuffer.WriteLine("Waiting Proses Posting di WINCORE... ");
                                waitpost.Start();
                            }
                            msg2++;
                            break;
                    }
                }

                TextBuffer.WriteLine("Proses Upload Data GL Complete");
                TextBuffer.WriteLine("Tanggal & Jam Akhir Proses : " + DateTime.Now);
                TextBuffer.WriteLine("Lama Proses : " + (DateTime.Now - startTime).ToString(@"hh\:mm\:ss") + "\n");
                TextBuffer.WriteLine("================================================================================================================================== ");

                File.Delete(filename);
            }
            isRun = false;
            NoBatch = "";
        }

        public static string GetVersion()
        {
            Assembly assem = Assembly.GetExecutingAssembly();
            AssemblyName aName = assem.GetName();
            return aName.Version.ToString();
        }

        public static int GetTimeOutVal()
        {
            string setting = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/");

            List<ParameterModel> sett = new List<ParameterModel>();
            ParameterJsonHelper readWrite = new ParameterJsonHelper();
            sett = JsonConvert.DeserializeObject<List<ParameterModel>>(readWrite.Read());
            var datum = sett.Where(x => x.Id == 1).SingleOrDefault();

            if (datum.TimeOutValidasi.ToInt() <= 0)
            {
                return 3;
            }
            else
            {
                return datum.TimeOutValidasi.ToInt();
            }
        }
        public static int GetTimeOutPost()
        {
            string setting = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/");

            List<ParameterModel> sett = new List<ParameterModel>();
            ParameterJsonHelper readWrite = new ParameterJsonHelper();
            sett = JsonConvert.DeserializeObject<List<ParameterModel>>(readWrite.Read());
            var datum = sett.Where(x => x.Id == 1).SingleOrDefault();

            if (datum.TimeOutValidasi.ToInt() <= 0)
            {
                return 3;
            }
            else
            {
                return datum.TimeOutValidasi.ToInt();
            }
        }
        public static string FlagHeaderNoBatch()
        {
            string conString = ConnectionHelper.GetStringConnection(); //ConfigurationManager.ConnectionStrings["DefaultConnectionIBM"].ConnectionString;
            string value = string.Empty;
            try
            {
                using (iDB2Connection iConn = new iDB2Connection(conString))
                {
                    iDB2Command cmd2 = iConn.CreateCommand();
                    cmd2.CommandText = "select TBRSBCH from TBRPF where tbrtype = 'H' and TBRFLAG = 'V'";
                    cmd2.CommandTimeout = 0;
                    iConn.Open();
                    iDB2DataReader reader = cmd2.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            value = (string)reader["TBRSBCH"];
                        }
                    }

                    reader.Close();
                    iConn.Close();
                }
                return value;
            }
            catch (Exception ex)
            {
                isRun = false;
                TextBuffer.WriteError("Error: " + ex.Message);

                throw new InvalidOperationException(ex.ToString());
            }
        }

        public static string FlagHeaderProc()
        {
            string conString = ConnectionHelper.GetStringConnection();// ConfigurationManager.ConnectionStrings["DefaultConnectionIBM"].ConnectionString;
            string value = string.Empty;

            try
            {
                using (iDB2Connection iConn = new iDB2Connection(conString))
                {
                    iDB2Command cmd2 = iConn.CreateCommand();
                    cmd2.CommandText = "select TBRPROC from TBRPF where tbrtype = 'H' and TBRPOST = 'Y'";
                    iConn.Open();
                    iDB2DataReader reader = cmd2.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            value = (string)reader["TBRPROC"];
                        }
                    }

                    reader.Close();
                    iConn.Close();
                }
                return value;
            }
            catch (Exception ex)
            {
                isRun = false;
                TextBuffer.WriteError("Error: " + ex.Message);

                throw new InvalidOperationException(ex.ToString());
            }
        }
        public static string FlagHeaderPost()
        {
            string conString = ConnectionHelper.GetStringConnection(); //ConfigurationManager.ConnectionStrings["DefaultConnectionIBM"].ConnectionString;
            string value = string.Empty;

            try
            {
                using (iDB2Connection iConn = new iDB2Connection(conString))
                {

                    iDB2Command cmd2 = iConn.CreateCommand();
                    cmd2.CommandText = "select TBRPOST from TBRPF where tbrtype = 'H' and TBRFLAG = 'P'";
                    cmd2.CommandTimeout = 0;
                    iConn.Open();
                    iDB2DataReader reader = cmd2.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            value = (string)reader["TBRPOST"];
                        }
                    }

                    reader.Close();
                    iConn.Close();
                }

                return value;
            }
            catch (Exception ex)
            {
                isRun = false;
                TextBuffer.WriteError("Error: " + ex.Message);

                throw new InvalidOperationException(ex.ToString());
            }
        }
        public static string FlagHeaderPosting()
        {
            string conString = ConnectionHelper.GetStringConnection(); //ConfigurationManager.ConnectionStrings["DefaultConnectionIBM"].ConnectionString;
            string value = string.Empty;
            try
            {
                using (iDB2Connection iConn = new iDB2Connection(conString))
                {
                    iDB2Command cmd2 = iConn.CreateCommand();
                    cmd2.CommandText = "select TBRPROC from TBRPF where tbrtype = 'H' AND TBRFLAG = 'P'";
                    iConn.Open();
                    iDB2DataReader reader = cmd2.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            value = (string)reader["TBRPROC"];
                        }
                    }

                    reader.Close();
                    iConn.Close();
                }
                return value;
            }
            catch (Exception ex)
            {
                isRun = false;
                TextBuffer.WriteError("Error: " + ex.Message);

                throw new InvalidOperationException(ex.ToString());
            }
        }

        public static void ReportHeaderValError(string namafile, DateTime jammulai, DateTime jamselesai, string noBatch, string prosesFlag, bool isEODFile)
        {
            string conString = ConnectionHelper.GetStringConnection(); //ConfigurationManager.ConnectionStrings["DefaultConnectionIBM"].ConnectionString;
                                                                       //string DirGagal = ConfigurationManager.ConnectionStrings["FileGagal"].ConnectionString;
                                                                       //string fileUpload = ConfigurationManager.ConnectionStrings["FileIntraday"].ConnectionString;
            namafile = Path.GetFileNameWithoutExtension(namafile);
            string DirGagal = System.Web.Hosting.HostingEnvironment.MapPath("~/FileGagal/");
            string fileUpload = isEODFile ? System.Web.Hosting.HostingEnvironment.MapPath("~/FileEOD/") : System.Web.Hosting.HostingEnvironment.MapPath("~/FileIntraday/");
            string TBRFLAG = "V";

            string TBRDSIN = string.Empty;
            string TBRSBCH = string.Empty;
            string TBRACN1 = string.Empty;
            string TBRERCO = string.Empty;
            string TBRERDS = string.Empty;
            string TBRBRCO = string.Empty;
            string TBRAMNT1 = string.Empty;
            string TBRAMNT2 = string.Empty;
            string TBRAMNT3 = string.Empty;
            string TBRDBC1 = string.Empty;
            string TBRCYCO = string.Empty;

            try
            {
                int i = 0;

                var sb = new StringBuilder();
                using (var writer = new StringWriter(sb))
                {
                    writer.WriteLine("PROSES ERROR VALIDASI DETAIL TOTAL MUTASI TIDAK BALANCE");
                    writer.WriteLine("============================================================================================== \n");

                    writer.WriteLine("NAMA BANK                    : " + NameBankCore());
                    writer.WriteLine("NAMA FILE                    : " + namafile + ".csv");
                    writer.WriteLine("NO BATCH                     : " + noBatch);
                    writer.WriteLine("TANGGAL & JAM PROSES MULAI   : " + jammulai);
                    writer.WriteLine("TANGGAL & JAM PROSES SELESAI : " + jamselesai);
                    writer.WriteLine("PROCESS TYPE / PROCESS FLAG  : " + prosesFlag);
                    writer.WriteLine("---------------------------------------------------------------------------------------------- \n");

                    writer.WriteLine("PESAN ERROR : ");
                    writer.WriteLine("=============\n");

                    using (iDB2Connection iConn = new iDB2Connection(conString))
                    {

                        iDB2Command cmd2 = iConn.CreateCommand();
                        cmd2.CommandText = "select TBRDSIN, TBRSBCH,TBRACN1,TBRERCO,TBRERDS from TBRPF where  TBRpost = 'Y' and TBRtype = 'H' and tbrproc ='03' and TBRFLAG ='"+ TBRFLAG + "' ";
                        cmd2.CommandTimeout = 0;
                        iDB2Command cmd3 = iConn.CreateCommand();
                        cmd3.CommandText = "SELECT  TBRBRCO,TBRCYCO, SUM(CASE WHEN tbrdbc1 = 'D' THEN TBRAMNT END) AS DEBET,SUM(CASE WHEN tbrdbc1 = 'K' THEN TBRAMNT END) AS KREDIT,SUM(CASE WHEN tbrdbc1 = 'D' THEN TBRAMNT END) - SUM(CASE WHEN tbrdbc1 = 'K' THEN TBRAMNT END) as SELISIH FROM TBRPF WHERE  TBRTYPE <> 'H' GROUP BY TBRBRCO, TBRCYCO";
                        cmd3.CommandTimeout = 0;
                        iDB2Command cmd4 = iConn.CreateCommand();
                        cmd4.CommandText = "SELECT SUM(CASE WHEN tbrdbc1 = 'D' THEN TBRAMNT END) AS DEBET, SUM(CASE WHEN tbrdbc1 = 'K' THEN TBRAMNT END) AS KREDIT, SUM(CASE WHEN tbrdbc1 = 'D' THEN TBRAMNT END) - SUM(CASE WHEN tbrdbc1 = 'K' THEN TBRAMNT END) as SELISIH FROM TBRPF WHERE TBRTYPE<> 'H'";
                        cmd4.CommandTimeout = 0;

                        iConn.Open();

                        iDB2DataReader reader1 = cmd2.ExecuteReader();
                        iDB2DataReader reader2 = cmd3.ExecuteReader();
                        iDB2DataReader reader3 = cmd4.ExecuteReader();

                        if (reader1.HasRows)
                        {
                            while (reader1.Read())
                            {
                                string TBRSEQNx = (string)reader1["TBRDSIN"];
                                TBRDSIN = TBRSEQNx.ToString();
                                TBRSBCH = (string)reader1["TBRSBCH"];
                                TBRACN1 = (string)reader1["TBRACN1"];
                                TBRERCO = (string)reader1["TBRERCO"];
                                TBRERDS = (string)reader1["TBRERDS"];

                                string msg = string.Empty;
                                var prefix = new List<string>
                                {
                                    "RECORD : " + TBRDSIN,
                                    "NO BATCH : " + TBRSBCH,
                                    "ERROR CODE : " + TBRERCO,
                                    "ERROR DESC : " + TBRERDS
                                };

                                if (prefix.Any())
                                {
                                    msg = string.Format("[{0}] ", string.Join(", ", prefix));
                                }
                                writer.WriteLine(string.Format("{0} {1}", "ERROR", msg));
                            }
                        }


                        writer.WriteLine("\n");
                        writer.WriteLine("DETAIL ERROR : ");
                        writer.WriteLine("==============\n");

                        writer.WriteLine("+-------------------------------------------------------------------------------------------------------------+");
                        writer.WriteLine("|NO     |CCY              |CABANG              |TOTAL DEBET         |TOTAL KREDIT        |SELISIH             |");
                        writer.WriteLine("+-------+-----------------+--------------------+--------------------+--------------------+--------------------+");
                        //writer.WriteLine("|       |" + value1.PadLeft(20, ' ') + "|" + value2.PadLeft(20, ' ') + "|" + value3.PadLeft(20, ' ') + "|" + value4.PadLeft(20, ' ') + "|");
                        //writer.WriteLine("+--------------------+-----------------------------------------------------------------------+");

                        if (reader2.HasRows)
                        {
                            while (reader2.Read())
                            {
                                i++;
                                string TBRBRCOx = (string)reader2["TBRBRCO"];
                                TBRBRCO = TBRBRCOx.ToString();
                                string TBRCYCOx = (string)reader2["TBRCYCO"];
                                TBRCYCO = TBRCYCOx.ToString();
                                decimal DEBET = (decimal)reader2["DEBET"];
                                TBRAMNT1 = DEBET.ToString();
                                decimal KREDIT = (decimal)reader2["KREDIT"];
                                TBRAMNT2 = KREDIT.ToString();
                                decimal SELISIH = (decimal)reader2["SELISIH"];
                                TBRAMNT3 = SELISIH.ToString();

                                writer.WriteLine("|" + i.ToString().PadLeft(7, ' ') + "|" + TBRCYCO.PadLeft(20, ' ') + "|" + TBRBRCO.PadLeft(20, ' ') + "|" + TBRAMNT1.PadLeft(20, ' ') + "|" + TBRAMNT2.PadLeft(20, ' ') + "|" + TBRAMNT3.PadLeft(20, ' ') + "|");
                            }

                        }

                        if (reader3.HasRows)
                        {
                            while (reader3.Read())
                            {
                                decimal DEBET = (decimal)reader3["DEBET"];
                                TBRAMNT1 = DEBET.ToString();
                                decimal KREDIT = (decimal)reader3["KREDIT"];
                                TBRAMNT2 = KREDIT.ToString();
                                decimal SELISIH = (decimal)reader3["SELISIH"];
                                TBRAMNT3 = SELISIH.ToString();
                            }
                        }
                        reader1.Close();
                        reader2.Close();
                        reader3.Close();
                        iConn.Close();
                    }
                    writer.WriteLine("+-------+--------------------+--------------------+--------------------+--------------------+--------------------+");
                    writer.WriteLine("Summary :" + i.ToString().PadLeft(20) + "|" + TBRAMNT1.PadLeft(20, ' ') + "|" + TBRAMNT2.PadLeft(20, ' ') + "|" + TBRAMNT3.PadLeft(20, ' '));
                }

                bool exists = Directory.Exists(DirGagal);

                if (!exists)
                    Directory.CreateDirectory(DirGagal);

                File.WriteAllText(DirGagal + "\\" + namafile + "_validasi_nobalance.error.txt", sb.ToString());

                var destinationcsv = Path.Combine(DirGagal + "\\", namafile.Trim() + ".csv");
                if (File.Exists(destinationcsv))
                    File.Delete(destinationcsv);

                //var destinationdone = Path.Combine(DirGagal + "\\", namafile.Trim() + ".done");
                //if (File.Exists(destinationdone))
                //    File.Delete(destinationdone);

                //  File.Copy(fileUpload + "\\" + namafile.Trim() + ".csv", destinationcsv, true);
                //  File.Copy(fileUpload + "\\" + namafile.Trim() + ".done", destinationdone, true);

                File.Move(fileUpload + "\\" + namafile.Trim() + ".csv", destinationcsv);
                //File.Move(fileUpload + "\\" + namafile.Trim() + ".done", destinationdone);
            }
            catch (Exception ex)
            {
                isRun = false;
                TextBuffer.WriteError("Error: " + ex.Message);

                throw new InvalidOperationException(ex.ToString());
            }
        }
        public static void ReportHeaderPostingError(string namafile, DateTime jammulai, DateTime jamselesai, string noBatch, string prosesFlag, bool isEODFile)
        {
            string conString = ConnectionHelper.GetStringConnection(); //ConfigurationManager.ConnectionStrings["DefaultConnectionIBM"].ConnectionString;
                                                                       //string DirGagal = ConfigurationManager.ConnectionStrings["FileGagal"].ConnectionString;
                                                                       // string fileUpload = ConfigurationManager.ConnectionStrings["FileIntraday"].ConnectionString;

            string DirGagal = System.Web.Hosting.HostingEnvironment.MapPath("~/FileGagal/");
            string fileUpload = isEODFile ? System.Web.Hosting.HostingEnvironment.MapPath("~/FileEOD/") : System.Web.Hosting.HostingEnvironment.MapPath("~/FileIntraday/");

            namafile = Path.GetFileNameWithoutExtension(namafile);

            string TBRDSIN = string.Empty;
            string TBRSBCH = string.Empty;
            string TBRACN1 = string.Empty;
            string TBRERCO = string.Empty;
            string TBRERDS = string.Empty;
            string TBRBRCO = string.Empty;
            string TBRAMNT1 = string.Empty;
            string TBRAMNT2 = string.Empty;
            string TBRAMNT3 = string.Empty;
            string TBRDBC1 = string.Empty;

            try
            {
                int i = 0;
                var sb = new StringBuilder();

                using (var writer = new StringWriter(sb))
                {
                    writer.WriteLine("PROSES ERROR VALIDASI POSTING TOTAL MUTASI TIDAK BALANCE");
                    writer.WriteLine("============================================================================================== \n");
                    writer.WriteLine("NAMA BANK                    : " + NameBankCore());
                    writer.WriteLine("NAMA FILE                    : " + namafile + ".csv");
                    writer.WriteLine("NO BATCH                     : " + noBatch);
                    writer.WriteLine("TANGGAL & JAM PROSES MULAI   : " + jammulai);
                    writer.WriteLine("TANGGAL & JAM PROSES SELESAI : " + jamselesai);
                    writer.WriteLine("PROCESS TYPE / PROCESS FLAG  : " + prosesFlag);
                    writer.WriteLine("---------------------------------------------------------------------------------------------- \n");

                    writer.WriteLine("PESAN ERROR : ");
                    writer.WriteLine("=============\n");

                    using (iDB2Connection iConn = new iDB2Connection(conString))
                    {

                        iDB2Command cmd2 = iConn.CreateCommand();
                        cmd2.CommandText = "select TBRDSIN, TBRSBCH,TBRACN1,TBRERCO,TBRERDS from TBRPF where  tbrpost = 'Y' and tbrtype = 'H' and tbrproc ='03' and TBRFLAG ='P' ";
                        cmd2.CommandTimeout = 0;
                        iDB2Command cmd3 = iConn.CreateCommand();
                        cmd3.CommandText = "SELECT  TBRBRCO,SUM(CASE WHEN tbrdbc1 = 'D' THEN TBRAMNT END) AS DEBET,SUM(CASE WHEN tbrdbc1 = 'K' THEN TBRAMNT END) AS KREDIT,SUM(CASE WHEN tbrdbc1 = 'D' THEN TBRAMNT END) - SUM(CASE WHEN tbrdbc1 = 'K' THEN TBRAMNT END) as SELISIH FROM TBRPF WHERE  TBRTYPE <> 'H'  and TBRFLAG ='V' GROUP BY TBRBRCO";
                        cmd3.CommandTimeout = 0;
                        iDB2Command cmd4 = iConn.CreateCommand();   
                        cmd4.CommandText = "SELECT SUM(CASE WHEN tbrdbc1 = 'D' THEN TBRAMNT END) AS DEBET, SUM(CASE WHEN tbrdbc1 = 'K' THEN TBRAMNT END) AS KREDIT, SUM(CASE WHEN tbrdbc1 = 'D' THEN TBRAMNT END) -SUM(CASE WHEN tbrdbc1 = 'K' THEN TBRAMNT END) as SELISIH FROM TBRPF WHERE TBRTYPE<> 'H' and TBRFLAG ='V'";
                        cmd4.CommandTimeout = 0;

                        iConn.Open();

                        iDB2DataReader reader1 = cmd2.ExecuteReader();
                        iDB2DataReader reader2 = cmd3.ExecuteReader();
                        iDB2DataReader reader3 = cmd4.ExecuteReader();

                        if (reader1.HasRows)
                        {
                            while (reader1.Read())
                            {
                                string TBRSEQNx = (string)reader1["TBRDSIN"];
                                TBRDSIN = TBRSEQNx.ToString();
                                TBRSBCH = (string)reader1["TBRSBCH"];
                                TBRACN1 = (string)reader1["TBRACN1"];

                                TBRERCO = (string)reader1["TBRERCO"];
                                TBRERDS = (string)reader1["TBRERDS"];

                                string msg = string.Empty;
                                var prefix = new List<string>();

                                prefix.Add("RECORD : " + TBRDSIN);
                                prefix.Add("NO BATCH : " + TBRSBCH);
                                prefix.Add("ERROR CODE : " + TBRERCO);
                                prefix.Add("ERROR DESC : " + TBRERDS);

                                if (prefix.Any())
                                {
                                    msg = string.Format("[{0}] ", string.Join(", ", prefix));
                                }
                                writer.WriteLine(string.Format("{0} {1}", "ERROR", msg));
                            }
                        }

                        writer.WriteLine("\n");
                        writer.WriteLine("DETAIL ERROR : ");
                        writer.WriteLine("==============\n");

                        writer.WriteLine("+-------------------------------------------------------------------------------------------+");
                        writer.WriteLine("|NO     |CABANG              |TOTAL DEBET         |TOTAL KREDIT        |SELISIH             |");
                        writer.WriteLine("+-------+--------------------+--------------------+--------------------+--------------------+");
                        //writer.WriteLine("|       |" + value1.PadLeft(20, ' ') + "|" + value2.PadLeft(20, ' ') + "|" + value3.PadLeft(20, ' ') + "|" + value4.PadLeft(20, ' ') + "|");
                        //writer.WriteLine("+--------------------+-----------------------------------------------------------------------+");

                        if (reader2.HasRows)
                        {
                            while (reader2.Read())
                            {
                                i++;
                                string TBRBRCOx = (string)reader2["TBRBRCO"];
                                TBRBRCO = TBRBRCOx.ToString();
                                var DEBET = string.IsNullOrWhiteSpace(reader2["DEBET"].ToString()) ? 0 : (decimal)reader2["DEBET"];
                                TBRAMNT1 = DEBET.ToString();
                                var KREDIT = string.IsNullOrWhiteSpace(reader2["KREDIT"].ToString()) ? 0 : (decimal)reader2["KREDIT"];
                                TBRAMNT2 = KREDIT.ToString();
                                decimal SELISIH = string.IsNullOrWhiteSpace(reader2["SELISIH"].ToString()) ? 0 : (decimal)reader2["SELISIH"];
                                TBRAMNT3 = SELISIH.ToString();

                                writer.WriteLine("|" + i.ToString().PadLeft(7, ' ') + "|" + TBRBRCO.PadLeft(20, ' ') + "|" + TBRAMNT1.PadLeft(20, ' ') + "|" + TBRAMNT2.PadLeft(20, ' ') + "|" + TBRAMNT3.PadLeft(20, ' ') + "|");
                            }
                        }

                        if (reader3.HasRows)
                        {
                            while (reader3.Read())
                            {
                                decimal DEBET = string.IsNullOrWhiteSpace(reader3["DEBET"].ToString()) ? 0 : (decimal)reader3["DEBET"];
                                TBRAMNT1 = DEBET.ToString();
                                decimal KREDIT = string.IsNullOrWhiteSpace(reader3["KREDIT"].ToString()) ? 0 : (decimal)reader3["KREDIT"];
                                TBRAMNT2 = KREDIT.ToString();
                                decimal SELISIH = string.IsNullOrWhiteSpace(reader3["SELISIH"].ToString()) ? 0 : (decimal)reader3["SELISIH"];
                                TBRAMNT3 = SELISIH.ToString();
                            }
                        }

                        reader1.Close();
                        reader2.Close();
                        reader3.Close();
                        iConn.Close();
                    }
                    writer.WriteLine("+-------+--------------------+--------------------+--------------------+--------------------+");
                    writer.WriteLine("Summary :" + i.ToString().PadLeft(20) + "|" + TBRAMNT1.PadLeft(20, ' ') + "|" + TBRAMNT2.PadLeft(20, ' ') + "|" + TBRAMNT3.PadLeft(20, ' '));
                }

                bool exists = Directory.Exists(DirGagal);

                if (!exists)
                    Directory.CreateDirectory(DirGagal);

                File.WriteAllText(DirGagal + "\\" + namafile + "_posting_notbalance.error.txt", sb.ToString());

                var destinationcsv = Path.Combine(DirGagal + "\\", namafile.Trim() + ".csv");
                if (File.Exists(destinationcsv))
                    File.Delete(destinationcsv);

                //var destinationdone = Path.Combine(DirGagal + "\\", namafile.Trim() + ".done");
                //if (File.Exists(destinationdone))
                //    File.Delete(destinationdone);

                // File.Copy(fileUpload + "\\" + namafile.Trim() + ".csv", destinationcsv, true);
                //  File.Copy(fileUpload + "\\" + namafile.Trim() + ".done", destinationdone, true);

                File.Move(fileUpload + "\\" + namafile.Trim() + ".csv", destinationcsv);
                //File.Move(fileUpload + "\\" + namafile.Trim() + ".done", destinationdone);
            }
            catch (Exception ex)
            {
                isRun = false;
                TextBuffer.WriteError("Error: " + ex.Message);
                throw new InvalidOperationException(ex.ToString());
            }
        }
        public static void ReportDetailValError(string namafile, DateTime jammulai, DateTime jamselesai, string noBatch, string prosesFlag, bool isEODFile)
        {
            string DirGagal = System.Web.Hosting.HostingEnvironment.MapPath("~/FileGagal/");
            string fileUpload = isEODFile ? System.Web.Hosting.HostingEnvironment.MapPath("~/FileEOD/") : System.Web.Hosting.HostingEnvironment.MapPath("~/FileIntraday/");
            string conString = ConnectionHelper.GetStringConnection();

            //namafile = Path.GetFileNameWithoutExtension(namafile);
            var TBRFLAG = isEODFile ? "V" : "E";
            string TBRSEQN = string.Empty;
            string TBRSBCH = string.Empty;
            string TBRACN1 = string.Empty;
            string TBRERCO = string.Empty;
            string TBRERDS = string.Empty;
            string TBRBRCO = string.Empty;
            string TBRAMNT = string.Empty;
            string TBRDBC1 = string.Empty;
            string TBRDATE = string.Empty;
            string TBRPODT = string.Empty;
            string TBRREFN = string.Empty;
            string TBRDSC1 = string.Empty;
            string JERERCO = string.Empty;
            string JERDESC = string.Empty;
            string JERLEVL = string.Empty;
            string JERUSLV = string.Empty;

            int i = 0;

            var sb = new StringBuilder();

            try
            {
                /*
                Console.WriteLine("-----------------------------------------------------------");
                Console.WriteLine("                    Error Upload GL                        ");
                Console.WriteLine("-----------------------------------------------------------\n");
                Console.WriteLine("Nama File : " + namafile);
                Console.WriteLine("Tanggal & Jam Proses   : " + jamproses);
                Console.WriteLine("Error List");
                Console.WriteLine("------------------------------------------------------------\n");
                */
                using (var writer = new StringWriter(sb))
                {
                    writer.WriteLine("PROSES ERROR VALIDASI DATA DETAIL GL");
                    writer.WriteLine(" \n");
                    writer.WriteLine("NAMA BANK                      : " + NameBankCore());
                    writer.WriteLine("NAMA FILE                      : " + namafile);
                    writer.WriteLine("NO BATCH                       : " + noBatch);
                    writer.WriteLine("TANGGAL & JAM PROSES MULAI     : " + jammulai);
                    writer.WriteLine("TANGGAL & JAM PROSES SELESAI   : " + jamselesai);
                    writer.WriteLine("PROCESS TYPE / PROCESS FLAG    : " + prosesFlag + " / " + TBRFLAG);
                    writer.WriteLine("");

                    writer.WriteLine("PESAN ERROR : \r");

                    using (iDB2Connection iConn = new iDB2Connection(conString))
                    {
                        iDB2Command cmd2 = iConn.CreateCommand();
                        cmd2.CommandText = "select TBRSEQN, TBRSBCH,TBRACN1,TBRERCO,TBRERDS from TBRPF where tbrpost = 'Y' and tbrtype = 'H' and tbrproc ='04' and TBRFLAG ='"+ TBRFLAG + "' ";

                        iDB2Command cmd3 = iConn.CreateCommand();

                        string sqlText = "select t1.TBRERCO as TBRERCO ,t1.TBRERDS as TBRERDS , t1.TBRSEQN as TBRSEQN , t1.TBRBRCO as TBRBRCO, t1.TBRDATE as TBRDATE, t1.TBRPODT as TBRPODT , t1.TBRREFN as TBRREFN , t1.TBRACN1 as TBRACN1 , t1.TBRAMNT as TBRAMNT , t1.TBRDSC1  as TBRDSC1 , t1.TBRDBC1 as TBRDBC1 , t2.JERERCO as JERERCO , t2.JERDESC as JERDESC , t2.JERLEVL as  JERLEVL, t2.JERUSLV as JERUSLV  from TBRPF t1 ";
                        sqlText += "LEFT join WINATSTMP.JERRPF t2 on t1.TBRUSIN = t2.JERUSER and t1.TBRDSIN = t2.JERDISP and t1.TBRDTIN = t2.JERDATE and t1.TBRTMIN = t2.JERTIME  and t1.TBRBRCO = t2.JERBRCO and t1.TBRDOCM = t2.JERMENU ";
                        sqlText += "where t1.tbrtype <> 'H' and t1.tbrflag = '3' and JERLEVL = '0'";

                        cmd3.CommandText = sqlText;
                        iConn.Open();

                        iDB2DataReader reader1 = cmd2.ExecuteReader();
                        iDB2DataReader reader2 = cmd3.ExecuteReader();

                        if (reader1.HasRows)
                        {
                            while (reader1.Read())
                            {
                                decimal TBRSEQNx = (decimal)reader1["TBRSEQN"];
                                TBRSEQN = TBRSEQNx.ToString();
                                TBRSBCH = (string)reader1["TBRSBCH"];
                                TBRACN1 = (string)reader1["TBRACN1"];

                                TBRERCO = (string)reader1["TBRERCO"];
                                TBRERDS = (string)reader1["TBRERDS"];

                                string msg = string.Empty;
                                var prefix = new List<string> {
                                    "RECORD : " + TBRSEQN,
                                    "NO BATCH : " + TBRSBCH,
                                    "ERROR CODE : " + TBRERCO,
                                    "ERROR DESC : " + TBRERDS
                                };

                                if (prefix.Any())
                                {
                                    msg = string.Format("[{0}] ", string.Join(" ", prefix));
                                }

                                writer.WriteLine(string.Format("{0} {1}", "ERROR", msg));
                            }
                        }

                        writer.WriteLine("");
                        writer.WriteLine("DETAIL ERROR : \r");
                        writer.WriteLine("NO,BARIS RECORD,CABANG,TGL TRANSAKSI,TGL POSTING,NO REFERENSI,NO REKENING,AMOUNT,KETERANGAN,D/K,ERROR CODE,KETERANGAN ERROR,LEVEL MESSAGE,USER LEVEL");

                        if (reader2.HasRows)
                        {
                            while (reader2.Read())
                            {
                                i++;
                                decimal TBRSEQNx = (decimal)reader2["TBRSEQN"];
                                TBRSEQN = TBRSEQNx.ToString() == null ? "" : TBRSEQNx.ToString();
                                string TBRBRCOx = (string)reader2["TBRBRCO"];
                                TBRBRCO = TBRBRCOx.ToString() == null ? "" : TBRBRCOx.ToString();
                                decimal TBRDATEx = (decimal)reader2["TBRDATE"];
                                TBRDATE = TBRDATEx.ToString() == null ? "" : TBRDATEx.ToString();
                                decimal TBRPODTx = (decimal)reader2["TBRPODT"];
                                TBRPODT = TBRPODTx.ToString() == null ? "" : TBRPODTx.ToString();
                                string TBRREFNx = (string)reader2["TBRREFN"];
                                TBRREFN = TBRREFNx.ToString() == null ? "" : TBRREFNx.ToString();
                                string TBRACN1x = (string)reader2["TBRACN1"];
                                TBRACN1 = TBRACN1x.ToString() == null ? "" : TBRACN1x.ToString();
                                decimal TBRAMNTx = (decimal)reader2["TBRAMNT"];
                                TBRAMNT = TBRAMNTx.ToString() == null ? "" : TBRAMNTx.ToString();
                                string TBRDSC1x = (string)reader2["TBRDSC1"];
                                TBRDSC1 = TBRDSC1x.ToString() == null ? "" : TBRDSC1x.ToString();
                                string TBRDBC1x = (string)reader2["TBRDBC1"];
                                TBRDBC1 = TBRDBC1x.ToString() == null ? "" : TBRDBC1x.ToString();

                                var TBRERCOx = reader2["TBRERCO"];
                                var TBRERDSx = reader2["TBRERDS"];

                                var JERERCOx = reader2["JERERCO"];

                                JERERCO = JERERCOx.ToString().Replace(",", "") == "" ? TBRERCOx.ToString() : JERERCOx.ToString().Replace(",", "");
                                var JERDESCx = reader2["JERDESC"];
                                JERDESC = JERDESCx.ToString().Replace(",", "") == "" ? TBRERDSx.ToString() : JERDESCx.ToString().Replace(",", "");
                                var JERLEVLx = reader2["JERLEVL"];
                                JERLEVL = JERLEVLx.ToString().Replace(",", "") ?? string.Empty;

                                var JERUSLVx = reader2["JERUSLV"];
                                JERUSLV = JERUSLVx.ToString() ?? string.Empty;

                                writer.WriteLine(i + "," + TBRSEQN + "," + TBRBRCO + "," + TBRDATE + "," + TBRPODT + "," + TBRREFN + "," + TBRACN1 + "," + TBRAMNT + "," + TBRDSC1 + "," + TBRDBC1 + "," + JERERCO + "," + JERDESC + "," + JERLEVL + "," + JERUSLV);
                            }
                        }
                        reader1.Close();
                        reader2.Close();

                        iConn.Close();
                    }
                    writer.WriteLine("");
                    writer.WriteLine("SUMMARY TOTAL RECORD: " + i);
                }

                bool exists = Directory.Exists(DirGagal);

                if (!exists)
                    Directory.CreateDirectory(DirGagal);

                File.WriteAllText(DirGagal + "\\" + namafile + "_validasi_error.csv", sb.ToString());

                var destinationcsv = Path.Combine(DirGagal + "\\", namafile.Trim() + ".csv");
                if (File.Exists(destinationcsv))
                    File.Delete(destinationcsv);


                //var destinationdone = Path.Combine(DirGagal + "\\", namafile.Trim() + ".done");
                //if (File.Exists(destinationdone))
                //    File.Delete(destinationdone);

                File.Move(fileUpload + "\\" + namafile.Trim() + ".csv", destinationcsv);
                //File.Move(fileUpload + "\\" + namafile.Trim() + ".done", destinationdone);
            }
            catch (Exception ex)
            {
                isRun = false;
                TextBuffer.WriteError("Error: " + ex.Message);

                throw new InvalidOperationException(ex.ToString());

            }
            // File.Move(fileUpload + "\\" + namafile.Trim() + ".csv", destinationcsv);
            // File.Move(fileUpload + "\\" + namafile.Trim() + ".done", destinationdone);
        }
        public static void ReportDetailPostError(string namafile, DateTime jammulai, DateTime jamselesai, string noBatch, string prosesFlag)
        {
            string conString = ConnectionHelper.GetStringConnection();
            string DirGagal = System.Web.Hosting.HostingEnvironment.MapPath("~/FileGagal/");
            //string fileUpload = System.Web.Hosting.HostingEnvironment.MapPath("~/FileIntraday/");

            namafile = Path.GetFileNameWithoutExtension(namafile);

            string TBRDSIN = string.Empty;
            string TBRSBCH = string.Empty;
            string TBRACN1 = string.Empty;
            string TBRERCO = string.Empty;
            string TBRERDS = string.Empty;
            string TBRBRCO = string.Empty;
            string TBRAMNT = string.Empty;
            string TBRDBC1 = string.Empty;
            string TBRDATE = string.Empty;
            string TBRPODT = string.Empty;
            string TBRREFN = string.Empty;
            string TBRDSC1 = string.Empty;
            string JERERCO = string.Empty;
            string JERDESC = string.Empty;
            string JERLEVL = string.Empty;
            string JERUSLV = string.Empty;

            int i = 0;

            var sb = new StringBuilder();
            try
            {
                /*
                Console.WriteLine("-----------------------------------------------------------");
                Console.WriteLine("                    Error Upload GL                        ");
                Console.WriteLine("-----------------------------------------------------------\n");
                Console.WriteLine("Nama File : " + namafile);
                Console.WriteLine("Tanggal & Jam Proses   : " + jamproses);
                Console.WriteLine("Error List");
                Console.WriteLine("------------------------------------------------------------\n");
                */
                using (var writer = new StringWriter(sb))
                {
                    writer.WriteLine("PROSES ERROR VALIDASI POSTING");
                    writer.WriteLine("");
                    writer.WriteLine("NAMA BANK  : " + NameBankCore());
                    writer.WriteLine("NAMA FILE  : " + namafile);
                    writer.WriteLine("NO BATCH    : " + noBatch);
                    writer.WriteLine("TANGGAL & JAM PROSES MULAI     : " + jammulai);
                    writer.WriteLine("TANGGAL & JAM PROSES SELESAI   : " + jamselesai);
                    writer.WriteLine("PROCESS TYPE / PROCESS FLAG    : " + prosesFlag);
                    writer.WriteLine("");

                    writer.WriteLine("PESAN ERROR : \r");
                    writer.WriteLine("");

                    using (iDB2Connection iConn = new iDB2Connection(conString))
                    {
                        iDB2Command cmd2 = iConn.CreateCommand();
                        cmd2.CommandText = "select TBRDSIN, TBRSBCH,TBRACN1,TBRERCO,TBRERDS from TBRPF where  tbrpost = 'Y' and tbrtype = 'H' and TBRFLAG ='P' "; //
                        cmd2.CommandTimeout = 0;
                        iDB2Command cmd3 = iConn.CreateCommand();

                        string sqlText = "select  t1.TBRERCO as TBRERCO ,t1.TBRERDS as TBRERDS,t1.TBRDSIN as TBRDSIN , t1.TBRBRCO as TBRBRCO, t1.TBRDATE as TBRDATE, t1.TBRPODT as TBRPODT , t1.TBRREFN as TBRREFN , t1.TBRACN1 as TBRACN1 , t1.TBRAMNT as TBRAMNT , t1.TBRDSC1  as TBRDSC1 , t1.TBRDBC1 as TBRDBC1 , t2.JERERCO as JERERCO , t2.JERDESC as JERDESC , t2.JERLEVL as  JERLEVL, t2.JERUSLV as JERUSLV  from TBRPF t1 ";
                        sqlText += "inner join winatstmp.JERRPF t2 on t1.TBRUSIN = t2.JERUSER and t1.TBRDSIN = t2.JERDISP and t1.TBRDTIN = t2.JERDATE and t1.TBRTMIN = t2.JERTIME  and t1.TBRBRCO = t2.JERBRCO ";
                        sqlText += "where t1.tbrtype <> 'H' and tbrpost = 'Y' and tbrflag ='3' and JERLEVL = '0'";

                        cmd3.CommandText = sqlText;
                        cmd3.CommandTimeout = 0;
                        iConn.Open();

                        iDB2DataReader reader1 = cmd2.ExecuteReader();
                        iDB2DataReader reader2 = cmd3.ExecuteReader();

                        if (reader1.HasRows)
                        {
                            while (reader1.Read())
                            {
                                string TBRSEQNx = (string)reader1["TBRDSIN"];
                                TBRDSIN = TBRSEQNx.ToString();
                                TBRSBCH = (string)reader1["TBRSBCH"];
                                TBRACN1 = (string)reader1["TBRACN1"];

                                TBRERCO = (string)reader1["TBRERCO"];
                                TBRERDS = (string)reader1["TBRERDS"];

                                string msg = string.Empty;
                                var prefix = new List<string>();

                                prefix.Add("RECORD : " + TBRDSIN);
                                prefix.Add("NO BATCH : " + TBRSBCH);
                                prefix.Add("ERROR CODE : " + TBRERCO);
                                prefix.Add("ERROR DESC : " + TBRERDS);

                                if (prefix.Any())
                                {
                                    msg = string.Format("[{0}] ", string.Join(", ", prefix));
                                }

                                writer.WriteLine(string.Format("{0} {1}", "ERROR", msg));
                            }
                        }

                        writer.WriteLine("DETAIL ERROR :\r");
                        writer.WriteLine("");
                        writer.WriteLine("No,Baris Record,Cabang,Tgl Transaksi,Tgl Posting,No Ref,No rek,Amount,Keterangan,D/K,Error Code,Ket. Error,Level Message,User Level");

                        if (reader2.HasRows)
                        {
                            while (reader2.Read())
                            {
                                i++;
                                string TBRSEQNx = (string)reader2["TBRDSIN"];
                                TBRDSIN = TBRSEQNx.ToString() == null ? "" : TBRSEQNx.ToString();
                                string TBRBRCOx = (string)reader2["TBRBRCO"];
                                TBRBRCO = TBRBRCOx.ToString() == null ? "" : TBRBRCOx.ToString();
                                decimal TBRDATEx = (decimal)reader2["TBRDATE"];
                                TBRDATE = TBRDATEx.ToString() == null ? "" : TBRDATEx.ToString();
                                decimal TBRPODTx = (decimal)reader2["TBRPODT"];
                                TBRPODT = TBRPODTx.ToString() == null ? "" : TBRPODTx.ToString();
                                string TBRREFNx = (string)reader2["TBRREFN"];
                                TBRREFN = TBRREFNx.ToString() == null ? "" : TBRREFNx.ToString();
                                string TBRACN1x = (string)reader2["TBRACN1"];
                                TBRACN1 = TBRACN1x.ToString() == null ? "" : TBRACN1x.ToString();
                                decimal TBRAMNTx = (decimal)reader2["TBRAMNT"];
                                TBRAMNT = TBRAMNTx.ToString() == null ? "" : TBRAMNTx.ToString();
                                string TBRDSC1x = (string)reader2["TBRDSC1"];
                                TBRDSC1 = TBRDSC1x.ToString() == null ? "" : TBRDSC1x.ToString();
                                string TBRDBC1x = (string)reader2["TBRDBC1"];
                                TBRDBC1 = TBRDBC1x.ToString() == null ? "" : TBRDBC1x.ToString();

                                var TBRERCOx = reader2["TBRERCO"];
                                var TBRERDSx = reader2["TBRERDS"];

                                var JERERCOx = reader2["JERERCO"];

                                JERERCO = JERERCOx.ToString().Replace(",", "") == "" ? TBRERCOx.ToString() : JERERCOx.ToString().Replace(",", "");
                                var JERDESCx = reader2["JERDESC"];
                                JERDESC = JERDESCx.ToString().Replace(",", "") == "" ? TBRERDSx.ToString() : JERDESCx.ToString().Replace(",", "");
                                var JERLEVLx = reader2["JERLEVL"];
                                JERLEVL = JERLEVLx.ToString().Replace(",", "") ?? string.Empty;

                                var JERUSLVx = reader2["JERUSLV"];
                                JERUSLV = JERUSLVx.ToString() ?? string.Empty;

                                writer.WriteLine(i + "," + TBRDSIN + "," + TBRBRCO + "," + TBRDATE + "," + TBRPODT + "," + TBRREFN + "," + TBRACN1 + "," + TBRDSC1 + "," + TBRDBC1 + "," + JERERCO + "," + JERDESC + "," + JERLEVL + "," + JERUSLV);
                            }
                        }

                        reader1.Close();
                        reader2.Close();

                        iConn.Close();
                    }
                    writer.WriteLine("");
                    writer.WriteLine("Summary total record: " + i);
                }

                bool exists = Directory.Exists(DirGagal);

                if (!exists)
                    Directory.CreateDirectory(DirGagal);

                File.WriteAllText(DirGagal + "\\" + namafile + "_posting_error.csv", sb.ToString());

                var destinationcsv = Path.Combine(DirGagal + "\\", namafile.Trim() + ".csv");
                if (File.Exists(destinationcsv))
                    File.Delete(destinationcsv);

                //var destinationdone = Path.Combine(DirGagal + "\\", namafile.Trim() + ".done");
                //if (File.Exists(destinationdone))
                //    File.Delete(destinationdone);

                // File.Copy(fileUpload + "\\" + namafile.Trim() + ".csv", destinationcsv, true);
                // File.Copy(fileUpload + "\\" + namafile.Trim() + ".done", destinationdone, true);

                // File.Move(fileUpload + "\\" + namafile.Trim() + ".csv", destinationcsv);
                // File.Move(fileUpload + "\\" + namafile.Trim() + ".done", destinationdone
            }
            catch (Exception ex)
            {
                isRun = false;
                TextBuffer.WriteError("Error: " + ex.Message);
                throw new InvalidOperationException(ex.ToString());
            }
        }
        public static void ReportDetailPostSukses(string namafile, DateTime jammulai, DateTime jamselesai, string noBatch, string prosesFlag)
        {
            string conString = ConnectionHelper.GetOledbStringConnection(); //ConfigurationManager.ConnectionStrings["DefaultConnectionIBM"].ConnectionString;
            string[] connx = conString.Split(';');
            string lib = connx[4].Replace("Catalog Library List =", "").Trim();
            //string DirSukses = ConfigurationManager.ConnectionStrings["FileSukses"].ConnectionString;
            // string fileUpload = ConfigurationManager.ConnectionStrings["FileIntraday"].ConnectionString;
            string DirSukses = System.Web.Hosting.HostingEnvironment.MapPath("~/FileSukses/");
            //string fileUpload = System.Web.Hosting.HostingEnvironment.MapPath("~/FileIntraday/");
            namafile = Path.GetFileNameWithoutExtension(namafile);
            string TBRDSIN = string.Empty;
            string TBRSBCH = string.Empty;
            string TBRACN1 = string.Empty;
            string TBRERCO = string.Empty;
            string TBRERDS = string.Empty;
            string TBRBRCO = string.Empty;
            string TBRAMNT = string.Empty;
            string TBRDBC1 = string.Empty;
            string TBRDATE = string.Empty;
            string TBRPODT = string.Empty;
            string TBRREFN = string.Empty;
            string TBRDSC1 = string.Empty;
            string TBRACX1 = string.Empty;
            string TBRTRC1 = string.Empty;
            string JERLEVL = string.Empty;
            string JERUSLV = string.Empty;

            int i = 0;
            int j = 0;
            int batch = 500;

            var sb = new StringBuilder();
            try
            {

                using (var writer = new StringWriter(sb))
                {
                    writer.WriteLine("PROSES SUKSES VALIDASI DATA POSTING \n");
                    //writer.WriteLine("__");
                    writer.WriteLine("NAMA BANK                      : " + NameBankCore());
                    writer.WriteLine("NAMA FILE                      : " + namafile + ".csv");
                    writer.WriteLine("NO BATCH                       : " + noBatch);
                    writer.WriteLine("TANGGAL & JAM PROSES MULAI     : " + jammulai);
                    writer.WriteLine("TANGGAL & JAM PROSES SELESAI   : " + jamselesai);
                    writer.WriteLine("PROCESS TYPE / PROCESS FLAG    : " + prosesFlag + " / Posting Berhasil \n");
                    // writer.WriteLine("----------------------------------------------------------------------------------- \n");

                    writer.WriteLine("DETAIL DATA YANG BERHASIL DI POSTING \n");
                    // writer.WriteLine("______________________________________________________________________________________________________________________________________________________________");


                    using (OleDbConnection iConn = new OleDbConnection(conString))
                    {
                        OleDbCommand cmd3 = iConn.CreateCommand();

                        string sqlText = "select   TBRDSIN ,  TBRBRCO,  TBRDATE,  TBRPODT ,  TBRREFN ,  TBRACN1 ,  TBRACX1, TBRAMNT ,  TBRDSC1 , TBRTRC1  ,TBRDBC1   from " + lib + ".TBRPF ";
                        sqlText += "where tbrtype <> 'H' and tbrpost = 'Y' and tbrflag ='1'";

                        cmd3.CommandText = sqlText;
                        cmd3.CommandTimeout = 0;

                        iConn.Open();
                        OleDbDataReader reader2 = cmd3.ExecuteReader();

                        writer.WriteLine("NO,BARIS RECORD,CABANG,TGL TRANSAKSI,TGL POSTING,NO REFERENSI,NO REKENING,AMOUNT,KETERANGAN,KODE TRANSAKSI,D/K,FLAG PROSES");

                        if (reader2.HasRows)
                        {
                            while (reader2.Read())
                            {
                                string TBRSEQNx = (string)reader2["TBRDSIN"];
                                TBRDSIN = TBRSEQNx.ToString();
                                string TBRBRCOx = (string)reader2["TBRBRCO"];
                                TBRBRCO = TBRBRCOx.ToString();
                                decimal TBRDATEx = (decimal)reader2["TBRDATE"];
                                TBRDATE = TBRDATEx.ToString();
                                decimal TBRPODTx = (decimal)reader2["TBRPODT"];
                                TBRPODT = TBRPODTx.ToString();
                                string TBRREFNx = (string)reader2["TBRREFN"];
                                TBRREFN = TBRREFNx.ToString();
                                string TBRACN1x = (string)reader2["TBRACN1"];
                                TBRACN1 = TBRACN1x.ToString();
                                string TBRACX1x = (string)reader2["TBRACX1"];
                                TBRACX1 = TBRACX1x.ToString();
                                decimal TBRAMNTx = (decimal)reader2["TBRAMNT"];
                                TBRAMNT = TBRAMNTx.ToString();
                                string TBRDSC1x = (string)reader2["TBRDSC1"];
                                TBRDSC1 = TBRDSC1x.ToString();
                                string TBRDBC1x = (string)reader2["TBRDBC1"];
                                TBRDBC1 = TBRDBC1x.ToString();
                                string TBRTRC1x = (string)reader2["TBRTRC1"];
                                TBRTRC1 = TBRTRC1x.ToString();

                                writer.WriteLine(QuoteStr(i.ToString()) + "," + QuoteStr(TBRDSIN) + "," + QuoteStr(TBRBRCO) + "," + QuoteStr(TBRDATE) + "," + QuoteStr(TBRPODT) + "," + QuoteStr(TBRREFN) + "," + QuoteStr(TBRACN1) + "," + QuoteStr(TBRAMNT) + "," + QuoteStr(TBRDSC1) + "," + QuoteStr(TBRTRC1) + "," + QuoteStr(TBRDBC1) + "," + QuoteStr(prosesFlag));
                                i++;

                                if (j == batch)
                                {
                                    TextBuffer.WriteLine("Line Record: " + i);
                                    j = 0;
                                }
                                else
                                {
                                    j++;
                                }
                            }
                        }
                        reader2.Close();
                        iConn.Close();
                    }
                    writer.WriteLine("");
                    writer.WriteLine("Summary total record: " + i);
                }

                bool exists = Directory.Exists(DirSukses);

                if (!exists)
                    Directory.CreateDirectory(DirSukses);

                File.WriteAllText(DirSukses + "\\" + namafile + "_posting_success.csv", sb.ToString());

                var destinationcsv = Path.Combine(DirSukses + "\\", namafile.Trim() + ".csv");
                if (File.Exists(destinationcsv))
                    File.Delete(destinationcsv);

                //var destinationdone = Path.Combine(DirSukses + "\\", namafile.Trim() + ".done");
                //if (File.Exists(destinationdone))
                //    File.Delete(destinationdone);

                //File.Copy(fileUpload + "\\" + namafile.Trim() + ".csv", destinationcsv, true);
                //File.Copy(fileUpload + "\\" + namafile.Trim() + ".done", destinationdone, true);

                //    File.Move(fileUpload + "\\" + namafile.Trim() + ".csv", destinationcsv);
                //    File.Move(fileUpload + "\\" + namafile.Trim() + ".done", destinationdone);

            }
            catch (Exception ex)
            {
                isRun = false;
                TextBuffer.WriteError("Error: " + ex.Message);

                throw new InvalidOperationException(ex.ToString());

            }
        }
        public static void ReportDetailPostPenampunganSukses(string namafile, DateTime jammulai, DateTime jamselesai, string noBatch, string prosesFlag)
        {
            string conString = ConnectionHelper.GetStringConnection();

            string DirSukses = System.Web.Hosting.HostingEnvironment.MapPath("~/FileSukses/");
            //string fileUpload = System.Web.Hosting.HostingEnvironment.MapPath("~/FileIntraday/");

            namafile = Path.GetFileNameWithoutExtension(namafile);

            string TBRDSIN = string.Empty;
            string TBRSBCH = string.Empty;
            string TBRACN1 = string.Empty;
            string TBRERCO = string.Empty;
            string TBRERDS = string.Empty;
            string TBRBRCO = string.Empty;
            string TBRAMNT = string.Empty;
            string TBRDBC1 = string.Empty;
            string TBRDATE = string.Empty;
            string TBRPODT = string.Empty;
            string TBRREFN = string.Empty;
            string TBRDSC1 = string.Empty;
            string TBRACX1 = string.Empty;
            string TBRTRC1 = string.Empty;
            string JERLEVL = string.Empty;
            string JERUSLV = string.Empty;
            try
            {
                int i = 0;
                var sb = new StringBuilder();

                using (var writer = new StringWriter(sb))
                {
                    writer.WriteLine("PROSES SUKSES VALIDASI DATA POSTING KE REKENING PENAMPUNG \n");
                    //writer.WriteLine("----------------------------------------------------------------------------------");
                    writer.WriteLine("NAMA BANK                      : " + NameBankCore());
                    writer.WriteLine("NAMA FILE                      : " + namafile + ".csv");
                    writer.WriteLine("NO BATCH                       : " + noBatch);
                    writer.WriteLine("TANGGAL & JAM PROSES MULAI     : " + jammulai);
                    writer.WriteLine("TANGGAL & JAM PROSES SELESAI   : " + jamselesai);
                    writer.WriteLine("PROCESS TYPE / PROCESS FLAG    : " + prosesFlag + " / Posting Berhasil ke Rek. Penampungan \n");
                    // writer.WriteLine("----------------------------------------------------------------------------------- \n");

                    writer.WriteLine("DETAIL DATA YANG BERHASIL POSTING \n");
                    //   writer.WriteLine("==================================================================================");

                    using (iDB2Connection iConn = new iDB2Connection(conString))
                    {
                        iDB2Command cmd3 = iConn.CreateCommand();

                        string sqlText = "select t1.TBRDSIN as TBRDSIN , t1.TBRBRCO as TBRBRCO, t1.TBRDATE as TBRDATE, t1.TBRPODT as TBRPODT , t1.TBRREFN as TBRREFN , t1.TBRACN1 as TBRACN1 , t1.TBRACX1 as TBRACX1,t1.TBRAMNT as TBRAMNT , t1.TBRDSC1  as TBRDSC1 , t1.TBRTRC1 as TBRTRC1  ,t1.TBRDBC1 as TBRDBC1   from TBRPF t1 ";
                        sqlText += "where t1.tbrtype <> 'H' and t1.tbrpost = 'Y' and t1.tbrflag ='2'";

                        cmd3.CommandText = sqlText;
                        cmd3.CommandTimeout = 0;
                        iConn.Open();

                        iDB2DataReader reader2 = cmd3.ExecuteReader();
                        writer.WriteLine("NO,BARIS RECORD, CABANG, TGL TRANSAKSI,TGL POSTING, NO REFERENSI,NO REKENING,NO REK PENAMPUNG, AMOUNT, KETERANGAN, KODE TRANSAKSI,D / K,FLAG PROSES");

                        if (reader2.HasRows)
                        {
                            while (reader2.Read())
                            {
                                string TBRSEQNx = (string)reader2["TBRDSIN"];
                                TBRDSIN = TBRSEQNx.ToString();
                                string TBRBRCOx = (string)reader2["TBRBRCO"];
                                TBRBRCO = TBRBRCOx.ToString();
                                decimal TBRDATEx = (decimal)reader2["TBRDATE"];
                                TBRDATE = TBRDATEx.ToString();
                                decimal TBRPODTx = (decimal)reader2["TBRPODT"];
                                TBRPODT = TBRPODTx.ToString();
                                string TBRREFNx = (string)reader2["TBRREFN"];
                                TBRREFN = TBRREFNx.ToString();
                                string TBRACN1x = (string)reader2["TBRACN1"];
                                TBRACN1 = TBRACN1x.ToString();
                                string TBRACX1x = (string)reader2["TBRACX1"];
                                TBRACX1 = TBRACX1x.ToString();
                                decimal TBRAMNTx = (decimal)reader2["TBRAMNT"];
                                TBRAMNT = TBRAMNTx.ToString();
                                string TBRDSC1x = (string)reader2["TBRDSC1"];
                                TBRDSC1 = TBRDSC1x.ToString();
                                string TBRDBC1x = (string)reader2["TBRDBC1"];
                                TBRDBC1 = TBRDBC1x.ToString();
                                string TBRTRC1x = (string)reader2["TBRTRC1"];
                                TBRTRC1 = TBRTRC1x.ToString();

                                writer.WriteLine(QuoteStr(i.ToString()) + "," + QuoteStr(TBRDSIN) + "," + QuoteStr(TBRBRCO) + "," + QuoteStr(TBRDATE) + "," + QuoteStr(TBRPODT) + "," + QuoteStr(TBRREFN) + "," + QuoteStr(TBRACN1) + "," + QuoteStr(TBRACX1) + "," + QuoteStr(TBRAMNT) + "," + QuoteStr(TBRDSC1) + "," + QuoteStr(TBRTRC1) + "," + QuoteStr(TBRDBC1) + "," + QuoteStr(prosesFlag));
                                i++;
                            }
                        }
                        reader2.Close();
                        iConn.Close();
                    }
                    writer.WriteLine("");
                    writer.WriteLine("Summary total record: " + i);
                }

                bool exists = Directory.Exists(DirSukses);

                if (!exists)
                    Directory.CreateDirectory(DirSukses);

                File.WriteAllText(DirSukses + "\\" + namafile + "_posting_success_penampungan.csv", sb.ToString());

                var destinationcsv = Path.Combine(DirSukses + "\\", namafile.Trim() + ".csv");
                if (File.Exists(destinationcsv))
                    File.Delete(destinationcsv);

                //var destinationdone = Path.Combine(DirSukses + "\\", namafile.Trim() + ".done");
                //if (File.Exists(destinationdone))
                //    File.Delete(destinationdone);

                // File.Copy(fileUpload + "\\" + namafile.Trim() + ".csv", destinationcsv, true);
                //  File.Copy(fileUpload + "\\" + namafile.Trim() + ".done", destinationdone, true);

                //    File.Move(fileUpload + "\\" + namafile.Trim() + ".csv", destinationcsv);
                //     File.Move(fileUpload + "\\" + namafile.Trim() + ".done", destinationdone);
            }
            catch (Exception ex)
            {
                isRun = false;
                TextBuffer.WriteError("Error: " + ex.Message);

                throw new InvalidOperationException(ex.ToString());
            }
        }

        public static void SummaryReportSuksesUpload(string namafile, DateTime jammulai, DateTime jamselesai, string noBatch, string prosesFlag, bool isEODFile)
        {
            string conString = ConnectionHelper.GetStringConnection();
            string DirSukses = System.Web.Hosting.HostingEnvironment.MapPath("~/FileSukses/");
            string DirGagal = System.Web.Hosting.HostingEnvironment.MapPath("~/FileGagal/");
            string fileUpload = isEODFile ? System.Web.Hosting.HostingEnvironment.MapPath("~/FileEOD/") : System.Web.Hosting.HostingEnvironment.MapPath("~/FileIntraday/");

            namafile = Path.GetFileNameWithoutExtension(namafile);

            try
            {
                var sb = new StringBuilder();

                string value1 = string.Empty;
                string value2 = string.Empty;
                string value3 = string.Empty;
                string value4 = string.Empty;
                string value5 = string.Empty;
                string value6 = string.Empty;
                string value7 = string.Empty;
                string value8 = string.Empty;
                string value9 = string.Empty;
                string value10 = string.Empty;
                string value11 = string.Empty;
                string value12 = string.Empty;

                using (var writer = new StringWriter(sb))
                {
                    writer.WriteLine("SUMMARY PROSES SUKSES UPLOAD DATA GL");
                    writer.WriteLine("=========================================================================================================");
                    writer.WriteLine("NAMA BANK                      : " + NameBankCore());
                    writer.WriteLine("NAMA FILE                      : " + namafile + ".csv");
                    writer.WriteLine("NOMOR BATCH                    : " + noBatch);
                    writer.WriteLine("TANGGAL & JAM PROSES MULAI     : " + jammulai);
                    writer.WriteLine("TANGGAL & JAM PROSES SELESAI   : " + jamselesai);
                    writer.WriteLine("---------------------------------------------------------------------------------------------------------");

                    writer.WriteLine("");
                    writer.WriteLine("");

                    using (iDB2Connection iConn = new iDB2Connection(conString))
                    {

                        iDB2Command cmd2 = iConn.CreateCommand();
                        cmd2.CommandText = "select count(*) as TBRCNT, sum(TBRAMNT) as TBRAMNT from TBRPF where TBRDBC1 = 'D'  and tbrpost = 'Y' and tbrtype = '' and TBRFLAG in ('1') group by TBRDBC1,tbrpost ";
                        cmd2.CommandTimeout = 0;
                        iDB2Command cmd3 = iConn.CreateCommand();
                        cmd3.CommandText = "select count(*) as TBRCNT, sum(TBRAMNT) as TBRAMNT from TBRPF where TBRDBC1 = 'K'  and tbrpost = 'Y' and tbrtype = '' and TBRFLAG in ('1') group by TBRDBC1,tbrpost ";

                        iDB2Command cmd4 = iConn.CreateCommand();
                        cmd4.CommandTimeout = 0;
                        cmd4.CommandText = "select count(*) as TBRCNT, sum(TBRAMNT) as TBRAMNT from TBRPF where TBRDBC1 = 'D'  and tbrpost = 'Y' and tbrtype = '' and TBRFLAG in ('2') group by TBRDBC1,tbrpost ";

                        iDB2Command cmd5 = iConn.CreateCommand();
                        cmd5.CommandTimeout = 0;
                        cmd5.CommandText = "select count(*) as TBRCNT, sum(TBRAMNT) as TBRAMNT from TBRPF where TBRDBC1 = 'K'  and tbrpost = 'Y' and tbrtype = '' and TBRFLAG in ('2') group by TBRDBC1,tbrpost ";

                        iDB2Command cmd6 = iConn.CreateCommand();
                        cmd6.CommandText = "select count(*) as TBRCNT, sum(TBRAMNT) as TBRAMNT from TBRPF where TBRDBC1 = 'D'  and tbrpost = 'Y' and tbrtype = '' and TBRFLAG in ('3') group by TBRDBC1,tbrpost ";
                        cmd6.CommandTimeout = 0;
                        iDB2Command cmd7 = iConn.CreateCommand();
                        cmd7.CommandText = "select count(*) as TBRCNT, sum(TBRAMNT) as TBRAMNT from TBRPF where TBRDBC1 = 'K'  and tbrpost = 'Y' and tbrtype = '' and TBRFLAG in ('3') group by TBRDBC1,tbrpost ";
                        cmd7.CommandTimeout = 0;

                        iConn.Open();

                        iDB2DataReader reader1 = cmd2.ExecuteReader();
                        iDB2DataReader reader2 = cmd3.ExecuteReader();
                        iDB2DataReader reader3 = cmd4.ExecuteReader();
                        iDB2DataReader reader4 = cmd5.ExecuteReader();
                        iDB2DataReader reader5 = cmd6.ExecuteReader();
                        iDB2DataReader reader6 = cmd7.ExecuteReader();

                        if (reader1.HasRows)
                        {
                            while (reader1.Read())
                            {
                                Int32 value1x = (Int32)reader1["TBRCNT"];
                                value1 = value1x.ToString() != "" ? value1x.ToString() : "-";

                                decimal value2x = (decimal)reader1["TBRAMNT"];
                                value2 = value2x.ToString() != "" ? value2x.ToString() : "-";
                            }
                        }
                        else
                        {
                            value1 = "-";
                            value2 = "-";
                        }

                        if (reader2.HasRows)
                        {
                            while (reader2.Read())
                            {
                                Int32 value3x = (Int32)reader2["TBRCNT"];
                                value3 = value3x.ToString() != "" ? value3x.ToString() : "-";

                                decimal value4x = (decimal)reader2["TBRAMNT"];
                                value4 = value4x.ToString() != "" ? value4x.ToString() : "-";
                            }
                        }
                        else
                        {
                            value3 = "-";
                            value4 = "-";
                        }

                        if (reader3.HasRows)
                        {
                            while (reader3.Read())
                            {
                                Int32 value5x = (Int32)reader2["TBRCNT"];
                                value5 = value5x.ToString() != "" ? value5x.ToString() : "-";

                                decimal value6x = (decimal)reader2["TBRAMNT"];
                                value6 = value6x.ToString() != "" ? value6x.ToString() : "-";
                            }
                        }
                        else
                        {
                            value5 = "-";
                            value6 = "-";
                        }

                        if (reader4.HasRows)
                        {
                            while (reader4.Read())
                            {
                                Int32 value7x = (Int32)reader2["TBRCNT"];
                                value7 = value7x.ToString() != "" ? value7x.ToString() : "-";

                                decimal value8x = (decimal)reader2["TBRAMNT"];
                                value8 = value8x.ToString() != "" ? value7x.ToString() : "-";
                            }
                        }
                        else
                        {
                            value7 = "-";
                            value8 = "-";
                        }

                        if (reader5.HasRows)
                        {
                            while (reader5.Read())
                            {
                                Int32 value9x = (Int32)reader2["TBRCNT"];
                                value9 = value9x.ToString() != "" ? value9x.ToString() : "-";

                                decimal value10x = (decimal)reader2["TBRAMNT"];
                                value10 = value10x.ToString() != "" ? value10x.ToString() : "-";
                            }
                        }
                        else
                        {
                            value9 = "-";
                            value10 = "-";
                        }

                        if (reader6.HasRows)
                        {
                            while (reader6.Read())
                            {
                                Int32 value11x = (Int32)reader2["TBRCNT"];
                                value11 = value11x.ToString() != "" ? value11x.ToString() : "-";

                                decimal value12x = (decimal)reader2["TBRAMNT"];
                                value12 = value12x.ToString() != "" ? value12x.ToString() : "-";
                            }
                        }
                        else
                        {
                            value11 = "-";
                            value12 = "-";
                        }
                        reader1.Close();
                        reader2.Close();
                        reader3.Close();
                        reader4.Close();
                        reader5.Close();
                        reader6.Close();
                        iConn.Close();
                    }

                    writer.WriteLine("+--------------------------------------------------------------------------------------------------------+");
                    writer.WriteLine("|                    |                  Debit                  |                  Kredit                 |");
                    writer.WriteLine("| Jenis Posting      +--------------------+--------------------+--------------------+--------------------+");
                    writer.WriteLine("|                    |Total Item          |Amont               |Total Item         |Amount              |");
                    writer.WriteLine("+--------------------+--------------------+--------------------+--------------------+--------------------+");
                    writer.WriteLine("|1. Berhasil         |" + value1.PadLeft(20, ' ') + "|" + value2.PadLeft(20, ' ') + "|" + value3.PadLeft(20, ' ') + "|" + value4.PadLeft(20, ' ') + "|");
                    writer.WriteLine("|2. Penampungan      |" + value5.PadLeft(20, ' ') + "|" + value6.PadLeft(20, ' ') + "|" + value7.PadLeft(20, ' ') + "|" + value8.PadLeft(20, ' ') + "|");
                    writer.WriteLine("|3. Gagal            |" + value9.PadLeft(20, ' ') + "|" + value10.PadLeft(20, ' ') + "|" + value11.PadLeft(20, ' ') + "|" + value12.PadLeft(20, ' ') + "|");
                    writer.WriteLine("+--------------------+-----------------------------------------------------------------------------------+");
                }
                bool exists = Directory.Exists(DirSukses);

                if (!exists)
                    Directory.CreateDirectory(DirSukses);

                File.WriteAllText(DirSukses + "\\" + namafile + "_posting_summary.txt", sb.ToString());

                var destinationcsv = Path.Combine(DirSukses + "\\", namafile.Trim() + ".csv");
                if (File.Exists(destinationcsv))
                    File.Delete(destinationcsv);

                //var destinationdone = Path.Combine(DirSukses + "\\", namafile.Trim() + ".done");
                //if (File.Exists(destinationdone))
                //    File.Delete(destinationdone);

                //  File.Copy(fileUpload + "\\" + namafile.Trim() + ".csv", destinationcsv, true);
                //  File.Copy(fileUpload + "\\" + namafile.Trim() + ".done", destinationdone, true);
                if (File.Exists(fileUpload + "\\" + namafile.Trim() + ".csv"))
                    File.Move(fileUpload + "\\" + namafile.Trim() + ".csv", destinationcsv);

                //if (File.Exists(fileUpload + "\\" + namafile.Trim() + ".done"))
                //    File.Move(fileUpload + "\\" + namafile.Trim() + ".done", destinationdone);
            }
            catch (Exception ex)
            {
                isRun = false;
                TextBuffer.WriteError("Error: " + ex.Message);
                throw new InvalidOperationException(ex.ToString());
            }
        }
    }
}