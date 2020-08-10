using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.IO;
using System.Linq;

namespace Status.Services
{
    class LoggingToFile
    {
        public static string LogFileName;
        private static Object fileLock = new Object();

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
            lock (fileLock)
            {
                using (var stream = new FileStream(LogFileName, FileMode.Append))
                using (var writer = new StreamWriter(stream))
                {
                    // Check file size before writing
                    if (stream.Position < StaticData.sizeLimitInBytes)
                    {
                        writer.WriteLine(text);
                    }
                    else
                    {
                        Console.WriteLine("Log file of {0} lines too big", stream.Position); 
                        using (var reduceStream = new FileStream(LogFileName, FileMode.Truncate))
                        {
                            long start = stream.Length * (long)0.1;
                            long count = stream.Length; 

                            //reduceStream.WriteAllLines(LogFileName, File.ReadAllLines(LogFileName)
                            //    .Where((line, index) => index < start - 1 ||
                            //    index >= start + count - 1));

                            writer.WriteLine(text);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Read From Log File
        /// </summary>
        /// <returns>string read from log file</returns>
        public string ReadFromLogFile()
        {
            lock (fileLock)
            {
                using (StreamReader reader = File.OpenText(LogFileName))
                {
                    return (reader.ToString());
                }
            }
        }

        /// <summary>
        /// Log string
        /// </summary>
        /// <param name="logMessage"></param>
        /// <param name="writer"></param>
        public static void Log(string logMessage, TextWriter writer)
        {
            lock (fileLock)
            {
                writer.WriteLine(logMessage);
            }
        }

        /// <summary>
        /// Dump log to console
        /// </summary>
        /// <param name="reader"></param>
        public static void DumpLog(StreamReader reader)
        {
            string line;

            lock (fileLock)
            {
                while ((line = reader.ReadLine()) != null)
                {
                    Console.WriteLine(line);
                }
            }
        }
    }
}
