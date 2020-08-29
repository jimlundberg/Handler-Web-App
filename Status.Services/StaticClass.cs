using Microsoft.Extensions.Logging;
using Status.Models;
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
        public static int MaxJobTimeLimitSeconds = 0;
        public static int ScanWaitTime = 0;
		public static int NumberOfJobsExecuting = 0;
		public static int RunningJobsIndex = 0;
		public static int LogFileSizeLimit = 0;

        public static Thread CurrentInputJobsScanThreadHandle;
        public static Thread ProcessingFileWatcherThreadHandle;
        public static Thread CurrentProcessingJobsScanThreadHandle;
        public static Thread DirectoryWatcherThreadHandle;
        public static Thread InputFileWatcherThreadHandle;
        public static Thread JobRunThreadHandle;
        public static Thread TcpIpListenThreadHandle;

        public static volatile bool ShutdownFlag = false;
        public static volatile bool PauseFlag = false;
        public static volatile bool CurrentProcessingJobsScanComplete = false;

		public static List<string> NewInputJobsToRun = new List<String>();
		public static List<string> NewProcessingJobsToRun = new List<String>();

		public static Dictionary<string, bool> InputFileScanComplete = new Dictionary<string, bool>();
        public static Dictionary<string, bool> InputJobScanComplete = new Dictionary<string, bool>();
        public static Dictionary<string, bool> ProcessingFileScanComplete = new Dictionary<string, bool>();
        public static Dictionary<string, bool> ProcessingJobScanComplete = new Dictionary<string, bool>();
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
			LoggingToFile log = new LoggingToFile(logFile);
			log.WriteToLogFile(msg);
		}

        /// <summary>
        /// Status Data Entry Method
        /// </summary>
        /// <param name="statusList"></param>
        /// <param name="job"></param>
        /// <param name="iniData"></param>
        /// <param name="status"></param>
        /// <param name="timeSlot"></param>
        /// <param name="logger"></param>
        public static void StatusDataEntry(List<StatusData> statusList, string job, IniFileData iniData,
            JobStatus status, JobType timeSlot, ILogger<StatusRepository> logger)
        {
            string statusLogFile = iniData.StatusLogFile;
            StatusEntry statusData = new StatusEntry(logger);
            statusData.ListStatus(iniData, statusList, job, status, timeSlot);
            statusData.WriteToCsvFile(job, status, timeSlot, statusLogFile);
        }

        /// <summary>
        /// Returns when file is ready to access
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>Returns if file is ready to access</returns>
        public static async Task IsFileReady(string fileName, ILogger<StatusRepository> logger)
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
                            Thread.Yield();
                        }
                        else
                        {
                            // Rethrow the exception as it's not an exclusively-opened-exception.
                            logger.LogError(String.Format("IsFileReady exception {0} rethrown for file {1} at {2:HH:mm:ss.fff}",
                                e.ToString(), fileName, DateTime.Now));
                            throw;
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Check the Input Buffer for directories that are older than the time limit
        /// </summary>
        /// <param name="iniData"></param>
        public static void CheckForInputBufferTimeLimits(IniFileData iniData)
        {
            string logFile = iniData.ProcessLogFile;
            string[] directories = Directory.GetDirectories(iniData.InputDir);
            foreach (string dir in directories)
            {
                // Get the current directory list and delete the ones beyond the time limit
                DirectoryInfo dirInfo = new DirectoryInfo(dir);
                if (dirInfo.LastWriteTime < DateTime.Now.AddDays(-iniData.InputBufferTimeLimit))
                {
                    FileHandling.DeleteDirectory(dir, logFile);
                }
            }
        }
    }
}