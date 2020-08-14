using System;
using System.IO;

namespace Status.Services
{
    /// <summary>
    /// Class to log to and control the size of the process log file
    /// </summary>
    class LoggingToFile
    {
        public static string LogFileName;
        private static readonly Object fileLock = new Object();

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
            bool tooBig = false;
            int MaxFileSize = StaticData.logFileSizeLimit * 1024 * 1024;

            lock (fileLock)
            {
                using (var stream = new FileStream(LogFileName, FileMode.Append))
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        // Check file size before writing
                        if (stream.Position < MaxFileSize)
                        {
                            writer.WriteLine(text);
                        }
                        else
                        {
                            tooBig = true;
                        }
                    }
                }
            }

            if (tooBig)
            {
                Console.WriteLine("Process log file too big. Reducing 10%");
                lock (fileLock)
                {
                    // Remove old data from log file
                    using (MemoryStream memoryStream = new MemoryStream(StaticData.logFileSizeLimit))
                    {
                        using (FileStream stream = new FileStream(LogFileName, FileMode.Open, FileAccess.ReadWrite))
                        {
                            // Reduce size of log file by 10%
                            int sizeLimit = (int)(MaxFileSize * 0.9);
                            stream.Seek(-sizeLimit, SeekOrigin.End);
                            byte[] bytes = new byte[sizeLimit];
                            stream.Read(bytes, 0, sizeLimit);
                            memoryStream.Write(bytes, 0, sizeLimit);
                            memoryStream.Position = 0;
                            stream.SetLength(sizeLimit);
                            stream.Position = 0;
                            memoryStream.CopyTo(stream);

                            using (var writer = new StreamWriter(stream))
                            {
                                writer.WriteLine(text);
                            }
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
