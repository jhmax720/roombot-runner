
using System;

using System.IO;

namespace RoombotRunner.Helpers
{

    public enum LogLevel
    {
        Information = 0,
        Warning = 1,
        Error = 2
    }

    public class Logger 
    {
        private Logger()
        {
            _fileName = $"{AppDomain.CurrentDomain.BaseDirectory}{DateTime.Now.ToString("LOG_dd_MMM_yyyy_HH_mm_sss")}.txt";
        }

        private string _fileName = string.Empty;
        private static Logger _instance = new Logger();
        public static Logger Instance
        {
            get
            {
                return _instance;
            }
        }

        public void Log(LogLevel level, string message)
        {
            try
            {
                Console.WriteLine(message);
                using (var sw = File.AppendText(_fileName))
                {
                    sw.WriteLine($"{DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")}\t{level}\t{message}");
                }
            }
            catch { }
        }
    }
}
