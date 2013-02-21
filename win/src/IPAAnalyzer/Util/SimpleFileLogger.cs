using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace IPAAnalyzer.Util
{
    public class SimpleFileLogger
    {
        private string _logFilename;
        public string LogFilename { get { return _logFilename; } }

        private string _identifier;

        public SimpleFileLogger(string filename)
        {
            _logFilename = filename;
            //_identifier = System.Guid.NewGuid().ToString();
        }

        private const string TYPE_INFO = "INFO";
        private const string TYPE_ERROR = "ERROR";

        public void LogInfo(string message)
        {
            Log("INFO", message);
        }

        public void LogError(string message)
        {
            Log("ERROR", message);
        }

        private void Log(string type, string message)
        {
            StreamWriter sw = null;
            try {
                sw = System.IO.File.AppendText(_logFilename);
                //string logMsg = string.Format("{0:o} [{1}] - {2}", DateTime.Now, _identifier, message);
                string logMsg = string.Format("{0:o} [{1}] - {2}", DateTime.Now, type, message);
                sw.WriteLine(logMsg);
            }
            finally {
                if (sw != null) {
                    sw.Close();
                }
            }
        }
    }
}
