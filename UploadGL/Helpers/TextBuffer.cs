using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace UploadGL.Helpers
{
    public static class TextBuffer
    {
        private static readonly StringBuilder Buffer = new StringBuilder();
        private static readonly ILog Log = LogManager.GetLogger(typeof(TextBuffer));

        public static void WriteLine(string value)
        {
            lock (Buffer)
            {
                Buffer.AppendLine(String.Format("{0}   {1}", DateTime.Now, value));
                Log.Info(String.Format("  {0}", value));
            }
        }

        public static void WriteError(string value)
        {
            lock (Buffer)
            {
                Buffer.AppendLine(String.Format("{0}   {1}", DateTime.Now, value));
                Log.Error(String.Format("  {0}", value));
            }
        }

        public static void Write(string value)
        {
            lock (Buffer)
            {
                Buffer.Append(value);
                Log.Info(String.Format("{0} {1}", DateTime.Now, value));
            }
        }

        public static void Clear()
        {
            lock (Buffer)
            {
                Buffer.Clear();
            }
        }

        public new static string ToString()
        {
            lock (Buffer)
            {
                return Buffer.ToString();
            }
        }
    }

}