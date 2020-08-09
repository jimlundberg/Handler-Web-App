using System;
using System.IO;

namespace Status.Services
{
    class LoggingToFile
    {
        string LogFileName;

        /// <summary>
        /// Logging to File Constructor
        /// </summary>
        /// <param name="logFileName"></param>
        public LoggingToFile(string logFileName)
        {
            LogFileName = logFileName;
        }

        /// <summary>
        /// Write to Log File
        /// </summary>
        /// <param name="text"></param>
        public void WriteToLogFile(string text)
        {
            using (StreamWriter writer = File.AppendText(LogFileName))
            {
                Log(text, writer);
            }
        }

        /// <summary>
        /// Read From Log File
        /// </summary>
        /// <returns></returns>
        public string ReadFromLogFile()
        {
            using (StreamReader reader = File.OpenText(LogFileName))
            {
                return(reader.ToString());
            }
        }

        /// <summary>
        /// Log string
        /// </summary>
        /// <param name="logMessage"></param>
        /// <param name="writer"></param>
        public static void Log(string logMessage, TextWriter writer)
        {
            writer.WriteLine(logMessage);
        }

        /// <summary>
        /// Dump log to console
        /// </summary>
        /// <param name="reader"></param>
        public static void DumpLog(StreamReader reader)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                Console.WriteLine(line);
            }
        }
    }
}
