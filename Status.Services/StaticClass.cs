using Microsoft.Extensions.Logging;
using Status.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Status.Services
{
    /// <summary>
    /// Static Data with global access
    /// </summary>
    public static class StaticClass
	{
        public const int TCP_IP_STARTUP_WAIT = 60000;
        public const int STARTING_TCP_IP_WAIT = 15000;
        public const int KILL_PROCESS_WAIT = 5000;
        public const int DISPLAY_PROCESS_DATA_WAIT = 45000;
        public const int DISPLAY_PROCESS_TITLE_WAIT = 1000;
        public const int SHUTDOWN_PROCESS_WAIT = 5000;
        public const int READ_AVAILABLE_RETRY_DELAY = 250;
        public const int FILE_WAIT_DELAY = 10;
        public const int NUM_TCP_IP_RETRIES = 480;

        public static double MaxJobTimeLimitSeconds = 0.0;
        public static int ScanWaitTime = 0;
		public static int NumberOfJobsExecuting = 0;
		public static int JobPortIndex = 0;
		public static int LogFileSizeLimit = 0;

        public static Thread InputJobsScanThreadHandle;
        public static Thread ProcessingFileWatcherThreadHandle;
        public static Thread ProcessingJobsScanThreadHandle;
        public static Thread DirectoryWatcherThreadHandle;
        public static Thread InputFileWatcherThreadHandle;
        public static Thread JobRunThreadHandle;
        public static Thread TcpListenerThreadHandle;

        public static volatile bool ShutdownFlag = false;
        public static volatile bool PauseFlag = false;
        public static volatile bool UnfinishedProcessingJobsScanComplete = false;
        
		public static List<string> ProcessingJobsToRun = new List<String>();

        public static Dictionary<string, DateTime> JobStartTime = new Dictionary<string, DateTime>();
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

        internal static LoggingToFile FileLoggerObject;
        internal static ILogger<StatusRepository> Logger;
        internal static SynchronizedCache InputJobsToRun = new SynchronizedCache();

        /// <summary>
        /// Global log to file method
        /// </summary>
        /// <param name="msg"></param>
        public static void Log(string msg)
		{
            FileLoggerObject.WriteToLogFile(msg);
            Console.WriteLine(msg);
        }

        /// <summary>
        /// Status Data Entry Method
        /// </summary>
        /// <param name="statusList"></param>
        /// <param name="job"></param>
        /// <param name="iniData"></param>
        /// <param name="status"></param>
        /// <param name="timeSlot"></param>
        public static void StatusDataEntry(List<StatusData> statusList, string job,
            IniFileData iniData, JobStatus status, JobType timeSlot)
        {
            string statusLogFile = iniData.StatusLogFile;

            // Write to the Status accumulator
            StatusEntry statusData = new StatusEntry();
            if (statusData == null)
            {
                StaticClass.Logger.LogError("StaticClass statusData failed to instantiate");
            }
            statusData.ListStatus(statusList, job, status, timeSlot);

            // Write new status to the log file
            CsvFileHandler csvFileHandler = new CsvFileHandler();
            {
                StaticClass.Logger.LogError("StaticClass csvFileHandler failed to instantiate");
            }
            csvFileHandler.WriteToCsvFile(job, status, timeSlot, statusLogFile);
        }

        /// <summary>
        /// Get the Job XML data 
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="iniData"></param>
        /// <param name="scanType"></param>
        /// <returns>Job xml data</returns>
        public static JobXmlData GetJobXmlFileInfo(string directory, IniFileData iniData, DirectoryScanType scanType)
        {
            JobXmlData jobScanXmlData = new JobXmlData();
            string baseDirectory = (scanType == DirectoryScanType.INPUT_BUFFER) ? iniData.InputDir : iniData.ProcessingDir;
            string job = directory.Replace(baseDirectory, "").Remove(0, 1);
            jobScanXmlData.Job = job;
            jobScanXmlData.JobDirectory = directory;
            jobScanXmlData.JobSerialNumber = job.Substring(0, job.IndexOf("_"));
            int start = job.IndexOf("_") + 1;
            jobScanXmlData.TimeStamp = job.Substring(start, job.Length - start);

            // Wait until the Xml file shows up
            bool xmlFileFound = false;
            do
            {
                string[] files = Directory.GetFiles(directory, "*.xml");
                if (files.Length > 0)
                {
                    jobScanXmlData.XmlFileName = Path.GetFileName(files[0]);
                    xmlFileFound = true;
                }

                Thread.Yield();
            }
            while (xmlFileFound == false);

            return jobScanXmlData;
        }

        /// <summary>
        /// Returns when a file is ready to access
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>Returns if file is ready to access</returns>
        public static bool IsFileReady(string fileName)
        {
            try
            {
                using (File.OpenWrite(fileName))
                {
                    return true;
                }
            }
            catch (IOException e)
            {
                var errorCode = Marshal.GetHRForException(e) & ((1 << 16) - 1);
                return errorCode == 32 || errorCode == 33;
            }
        }

        /// <summary>
        /// Check the Input Buffer for directories that are older than the time limit
        /// </summary>
        /// <param name="iniData"></param>
        public static void CheckForInputBufferTimeLimits(IniFileData iniData)
        {
            string[] directories = Directory.GetDirectories(iniData.InputDir);
            foreach (string dir in directories)
            {
                // Get the current directory list and delete the ones beyond the time limit
                DirectoryInfo dirInfo = new DirectoryInfo(dir);
                if (dirInfo.LastWriteTime < DateTime.Now.AddDays(-iniData.InputBufferTimeLimit))
                {
                    FileHandling.DeleteDirectory(dir);
                }
            }
        }
    }
}