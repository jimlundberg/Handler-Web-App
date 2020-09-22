using Microsoft.Extensions.Logging;
using Status.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Status.Services
{
    /// <summary>
    /// Static Data with global access
    /// </summary>
    public static class StaticClass
	{
        // Common definitions
        public const int TCP_IP_STARTUP_WAIT = 60000;
        public const int STARTING_TCP_IP_WAIT = 15000;
        public const int POST_PROCESS_WAIT = 10000;
        public const int DIRECTORY_RECEIVE_WAIT = 12000;
        public const int FILE_RECEIVE_WAIT = 1000;
        public const int WAIT_FOR_FILES_TO_COMPLETE = 2500;
        public const int DISPLAY_PROCESS_DATA_WAIT = 45000;
        public const int DISPLAY_PROCESS_TITLE_WAIT = 1000;
        public const int SHUTDOWN_PROCESS_WAIT = 5000;
        public const int READ_AVAILABLE_RETRY_DELAY = 250;
        public const int FILE_WAIT_DELAY = 2500;
        public const int FILE_READY_WAIT = 250;
        public const int ADD_TASK_DELAY = 300;
        public const int DELETE_TASK_DELAY = 400;
        public const int ADD_JOB_DELAY = 2000;
        public const int NUM_TCP_IP_RETRIES = 240;
        public const int NUM_XML_ACCESS_RETRIES = 100;
        public const int NUM_FILE_RECEIVE_RETRIES = 100;
        public const int NUM_RESULTS_ENTRY_RETRIES = 100;
        public const int NUM_REQUESTS_TILL_TCPIP_SLOWDOWN = 5;

        // Common counters
        public static double MaxJobTimeLimitSeconds = 0.0;
        public static int ScanWaitTime = 0;
		public static int NumberOfJobsExecuting = 0;
		public static int JobPortIndex = 0;
		public static int LogFileSizeLimit = 0;

        // Thread handles
        public static Thread InputJobsScanThreadHandle;
        public static Thread ProcessingFileWatcherThreadHandle;
        public static Thread ProcessingJobsScanThreadHandle;
        public static Thread DirectoryWatcherThreadHandle;
        public static Thread InputFileWatcherThreadHandle;
        public static Thread JobRunThreadHandle;
        public static Thread TcpListenerThreadHandle;

        // Global flags
        public static volatile bool ShutdownFlag = false;
        public static volatile bool PauseFlag = false;
        public static volatile bool UnfinishedProcessingJobsScanComplete = false;
        
        // Job state tracking
        public static Dictionary<string, DateTime> JobStartTime = new Dictionary<string, DateTime>();
        public static Dictionary<string, bool> InputFileScanComplete = new Dictionary<string, bool>();
        public static Dictionary<string, bool> InputJobScanComplete = new Dictionary<string, bool>();
        public static Dictionary<string, bool> ProcessingFileScanComplete = new Dictionary<string, bool>();
        public static Dictionary<string, bool> ProcessingJobScanComplete = new Dictionary<string, bool>();
        public static Dictionary<string, bool> JobShutdownFlag = new Dictionary<string, bool>();
        public static Dictionary<string, bool> TcpIpScanComplete = new Dictionary<string, bool>();

        // Job number of files tracking
		public static Dictionary<string, int> NumberOfInputFilesFound = new Dictionary<string, int>();
		public static Dictionary<string, int> NumberOfInputFilesNeeded = new Dictionary<string, int>();
		public static Dictionary<string, int> NumberOfProcessingFilesFound = new Dictionary<string, int>();
		public static Dictionary<string, int> NumberOfProcessingFilesNeeded = new Dictionary<string, int>();

        // Modeler process handle list
		public static Dictionary<string, Process> ProcessHandles = new Dictionary<string, Process>();

        // Common objects
        internal static LoggingToFile FileLoggerObject;
        internal static Logger<IStatusRepository> Logger;
        internal static SynchronizedCache InputJobsToRun = new SynchronizedCache();
        internal static CsvFileHandler CsvFileHandlerHandle = new CsvFileHandler();
        internal static StatusEntry StatusEntryHandle = new StatusEntry();
        internal static List<StatusData> StatusDataList = new List<StatusData>();
        internal static IniFileData IniData = new IniFileData();

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
        /// <param name="job"></param>
        /// <param name="status"></param>
        /// <param name="timeSlot"></param>
        public static void StatusDataEntry(string job, JobStatus status, JobType timeSlot)
        {
            // Write to the Status accumulator
            StatusEntryHandle.ListStatus(job, status, timeSlot);

            // Write new status to the log file
            CsvFileHandlerHandle.WriteToCsvFile(job, status, timeSlot);
        }

        /// <summary>
        /// Get the Job XML data 
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="scanType"></param>
        /// <returns>Job xml data</returns>
        public static JobXmlData GetJobXmlFileInfo(string directory, DirectoryScanType scanType)
        {
            JobXmlData jobScanXmlData = new JobXmlData();
            string baseDirectory = (scanType == DirectoryScanType.INPUT_BUFFER) ? IniData.InputDir : IniData.ProcessingDir;
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
        public static bool CheckFileReady(string fileName)
        {
            int numOfRetries = 0;
            do
            {
                try
                {
                    // Check that a file is both readable and writeable
                    using (FileStream fileService = File.Open(fileName, FileMode.Open))
                    {
                        if (fileService.CanRead && fileService.CanWrite)
                        {
                            //StaticClass.Log(string.Format("File {0} ready at {1:HH:mm:ss.fff}",
                            //    fileName, DateTime.Now));

                            return true;
                        }
                    }
                }
                catch (IOException)
                {
                    //StaticClass.Log(string.Format("File {0} ready retry {1} at {2:HH:mm:ss.fff}",
                    //    fileName, numOfRetries, DateTime.Now));

                    Thread.Sleep(FILE_READY_WAIT);
                }

                // Check for shutdown or pause
                if (ShutDownPauseCheck("IsFileReady") == true)
                {
                    return false;
                }

                Thread.Yield();
            }
            while (numOfRetries++ < NUM_XML_ACCESS_RETRIES);

            return false;
        }

        /// <summary>
        /// Returns when a file is ready to access
        /// </summary>
        /// <param name="directory"></param>
        /// <returns>Returns if file is ready to access</returns>
        public static bool CheckDirectoryReady(string directory)
        {
            // Get directory info
            DirectoryInfo dirInfo = new DirectoryInfo(directory);
            try
            {
                // If GetDirectories works then is accessible
                DirectoryInfo[] dirs = dirInfo.GetDirectories();
                if (dirs != null)
                {
                    return true;
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Check Job Directory is complete
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public static bool CheckJobDirectoryComplete(string directory)
        {
            Dictionary<string, bool> files = new Dictionary<string, bool>();
            bool filesAreReady = false;
            if (CheckDirectoryReady(directory))
            {
                string[] directoryList = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
                int numRetries = 0;
                do
                {
                    if (directoryList.Length == 0)
                    {
                        return true;
                    }
                    else
                    {
                        foreach (string file in directoryList)
                        {
                            files.Add(file, CheckFileReady(file));
                        }

                        // Check that all files are accessable
                        filesAreReady = files.ContainsValue(true);
                    }
                }
                while ((filesAreReady == false) && (numRetries < NUM_FILE_RECEIVE_RETRIES));
            }

            return filesAreReady;
        }

        /// <summary>
        /// Check the Input Buffer for directories that are older than the time limit
        /// </summary>
        public static void CheckForInputBufferTimeLimits()
        {
            string[] directories = Directory.GetDirectories(IniData.InputDir);
            foreach (string dir in directories)
            {
                // Get the current directory list and delete the ones beyond the time limit
                DirectoryInfo dirInfo = new DirectoryInfo(dir);
                if (dirInfo.LastWriteTime < DateTime.Now.AddDays(-IniData.InputBufferTimeLimit))
                {
                    FileHandling.DeleteDirectory(dir);
                }
            }
        }

        /// <summary>
        /// Shut Down and Pause Check
        /// </summary>
        /// <param name="location"></param>
        /// <returns>Shutdown or not</returns>
        public static bool ShutDownPauseCheck(string location)
        {
            // Output message of the shutdown flag is set
            if (ShutdownFlag == true)
            {
                Log(string.Format("\nShutdown {0} at {1:HH:mm:ss.fff}", location, DateTime.Now));

                // Shutdown confirmed
                return true;
            }

            // Check if the pause flag is set, then wait for reset
            if (PauseFlag == true)
            {
                Log(string.Format("Handler in Pause mode in {0} at {1:HH:mm:ss.fff}", location, DateTime.Now));
                do
                {
                    Thread.Yield();
                }
                while (PauseFlag == true);
            }

            // No shutdown
            return false;
        }
    }
}