using System;
using System.IO;

namespace Status.Services
{
    /// <summary>
    /// Class to log to and control the size of the process log file
    /// </summary>
    class LoggingToFile
    {
        private string LogFileName;
        private static readonly Object FileLock = new Object();

        /// <summary>
        /// Default logging To file constructor
        /// </summary>
        public LoggingToFile() { }

        /// <summary>
        /// Logging to file constructor with file name 
        /// </summary>
        /// <param name="logFileName"></param>
        public LoggingToFile(string logFileName)
        {
            LogFileName = logFileName;
        }

        /// <summary>
        /// Set the log file name
        /// </summary>
        /// <param name="logFileName"></param>
        public void SetLogFileName(string logFileName)
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
            int MaxFileSize = StaticClass.LogFileSizeLimit * 1024 * 1024;

            lock (FileLock)
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
                StaticClass.Log("\nProcess log file too big. Reducing 10%...\n");
                lock (FileLock)
                {
                    // Remove old data from log file
                    using (MemoryStream memoryStream = new MemoryStream(StaticClass.LogFileSizeLimit))
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
    }
}
