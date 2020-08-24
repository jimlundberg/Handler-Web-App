using Microsoft.Extensions.Logging;
using StatusModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Status.Services
{
    /// <summary>
    /// Static Data with global access
    /// </summary>
    public static class StaticClass
	{
		public static int NumberOfJobsExecuting = 0;
		public static int RunningJobsIndex = 0;
		public static int logFileSizeLimit = 0;

		public static volatile bool ShutdownFlag = false;
		public static volatile bool CurrentInputJobsScanComplete = false;
		public static volatile bool CurrentProcessingJobScanComplete = false;

		public static List<string> NewInputJobsToRun = new List<String>();
		public static List<string> NewProcessingJobsToRun = new List<String>();

		public static Dictionary<string, bool> InputFileScanComplete = new Dictionary<string, bool>();
		public static Dictionary<string, bool> ProcessingFileScanComplete = new Dictionary<string, bool>();
        public static Dictionary<string, bool> TcpIpScanComplete = new Dictionary<string, bool>();

		public static Dictionary<string, int> NumberOfInputFilesFound = new Dictionary<string, int>();
		public static Dictionary<string, int> NumberOfInputFilesNeeded = new Dictionary<string, int>();
		public static Dictionary<string, int> NumberOfProcessingFilesFound = new Dictionary<string, int>();
		public static Dictionary<string, int> NumberOfProcessingFilesNeeded = new Dictionary<string, int>();

		public static Dictionary<string, Process> ProcessHandles = new Dictionary<string, Process>();

		/// <summary>
		/// Global log to file method
		/// </summary>
		/// <param name="logFile"></param>
		/// <param name="msg"></param>
		public static void Log(string logFile, string msg)
		{
			Console.WriteLine(msg);
			Thread.Sleep(10);
			LoggingToFile log = new LoggingToFile(logFile);
			log.WriteToLogFile(msg);
			Thread.Sleep(10);
		}

        /// <summary>
        /// Status Data Entry Method
        /// </summary>
        /// <param name="statusList"></param>
        /// <param name="job"></param>
        /// <param name="iniData"></param>
        /// <param name="status"></param>
        /// <param name="timeSlot"></param>
        /// <param name="logFileName"></param>
        /// <param name="logger"></param>
        public static void StatusDataEntry(List<StatusData> statusList, string job, IniFileData iniData, JobStatus status,
            JobType timeSlot, string logFileName, ILogger<StatusRepository> logger)
        {
            StatusEntry statusData = new StatusEntry(statusList, job, status, timeSlot, logFileName, logger);
            statusData.ListStatus(iniData, statusList, job, status, timeSlot);
            statusData.WriteToCsvFile(job, iniData, status, timeSlot, logFileName, logger);
        }

        /// <summary>
        /// Is file ready to access
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>Returns if file is ready to access</returns>
        public static async Task IsFileReady(string fileName)
        {
            await Task.Run(() =>
            {
                // Check if file even exists
                if (!File.Exists(fileName))
                {
                    return;
                }

                bool isReady = false;
                while (!isReady)
                {
                    // If file can be opened for exclusive access it means it is no longer locked by other process
                    try
                    {
                        using (FileStream inputStream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                            if (inputStream.Length > 0)
                            {
                                Console.WriteLine(String.Format("\nIsFileReady sees {0} as available at {1:HH:mm:ss.fff}",
                                    fileName, DateTime.Now));
                                isReady = true;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        // Check if the exception is related to an IO error.
                        if (e.GetType() == typeof(IOException))
                        {
                            isReady = false;
                            Thread.Sleep(500);
                        }
                        else
                        {
                            // Rethrow the exception as it's not an exclusively-opened-exception.
                            Console.WriteLine(String.Format("IsFileReady exception {0} rethrown for file {1} at {2:HH:mm:ss.fff}",
                                e.ToString(), fileName, DateTime.Now));
                            throw;
                        }
                    }
                }
            });
        }
    }
}