using StatusModels;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Status.Services
{
    /// <summary>
    /// Class to run the whole monitoring process as a thread
    /// </summary>
    public class NewJobsScanThread
    {
        private static Thread thread;
        public static IniFileData IniData;
        public static List<StatusData> StatusData;
        public static ILogger<StatusRepository> Logger;

        /// <summary>
        /// Processing complete callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void oldJob_ProcessCompleted(object sender, EventArgs e)
        {
            // Set Flag for ending directory scan loop
            Console.WriteLine("\nOld Job Scan Completed!");
            StaticData.OldJobScanComplete = true;
            ScanForCurrentNewJobs(IniData, StatusData, Logger);
        }

        /// <summary>
        /// Processing complete callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void newJob_DirectoryFound(object sender, EventArgs e)
        {
            StaticData.Log(IniData.ProcessLogFile, 
                String.Format("\nnewJob_DirectoryFound Received new directory at {0:HH:mm:ss.fff}", DateTime.Now));

            // Set Flag for ending directory scan loop
            StaticData.FoundNewJobReadyToRun = true;
        }

        /// <summary>
        /// New jobs Scan thread
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public NewJobsScanThread(IniFileData iniData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            IniData = iniData;
            StatusData = statusData;
            Logger = logger;
        }

        /// <summary>
        /// A Thread procedure that scans for new jobs
        /// </summary>
        public void ThreadProc()
        {
            thread = new Thread(() => ScanForCurrentNewJobs(IniData, StatusData, Logger));
            if (thread == null)
            {
                Logger.LogError("NewJobsScanThread ScanForNewJobs thread failed to instantiate");
            }
            thread.Start();
        }

        /// <summary>
        /// Method to scan for new jobs in the Input Buffer
        /// </summary>
        /// <param name="iniFileData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public static void ScanForCurrentNewJobs(IniFileData iniFileData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            string logFile = iniFileData.ProcessLogFile;

            StaticData.Log(logFile, "\nChecking for unfinished Processing Jobs...");

            // Register with the Old Jobs Processing class event and start its thread
            OldJobsScanThread oldJobs = new OldJobsScanThread(IniData, StatusData, Logger);
            if (oldJobs == null)
            {
                Logger.LogError("NewJobsScanThread oldJobs failed to instantiate");
            }
            oldJobs.ProcessCompleted += oldJob_ProcessCompleted;
            oldJobs.ThreadProc();

            // Wait while scanning for unfinished Processing jobs
            do
            {
                Thread.Sleep(250);
            }
            while ((StaticData.OldJobScanComplete == false) && (StaticData.ShutdownFlag == false));

            StaticData.Log(logFile, "\nChecking for unfinished Input Jobs...");

            StatusModels.JobXmlData jobXmlData = new StatusModels.JobXmlData();
            if (jobXmlData == null)
            {
                Logger.LogError("NewJobsScanThread jobXmlDatafailed failed to instantiate");
            }

            List<DirectoryInfo> runDirectoryInfoList = new List<DirectoryInfo>();
            if (runDirectoryInfoList == null)
            {
                Logger.LogError("NewJobsScanThread runDirectoryInfoList failed to instantiate");
            }

            List<String> runDirectoryList = new List<String>();
            if (runDirectoryList == null)
            {
                Logger.LogError("NewJobsScanThread runDirectoryList failed to instantiate");
            }

            DirectoryInfo runDirectoryInfo = new DirectoryInfo(iniFileData.InputDir);
            if (runDirectoryInfo == null)
            {
                Logger.LogError("NewJobsScanThread runDirectoryInfo failed to instantiate");
            }

            // Get the list of directories from the Input Buffer
            bool oldProcessingJobsFound = false;
            runDirectoryInfoList = runDirectoryInfo.EnumerateDirectories().ToList();
            if (runDirectoryInfoList.Count > 0)
            {
                oldProcessingJobsFound = true;
                StaticData.Log(logFile, "\nProcesssing unfinished Input jobs...");
            }
            else
            {
                StaticData.Log(logFile, "\nNo unfinished Input Jobs Found...");
            }

            // Start the jobs in the directory list found on initial scan of the Input Buffer
            foreach (var dir in runDirectoryInfoList)
            {
                StartJob(dir.ToString(), oldProcessingJobsFound, IniData, StatusData, Logger);
                Thread.Sleep(iniFileData.ScanTime);
            }

            if (oldProcessingJobsFound)
            {
                StaticData.Log(logFile, "\nNo more unfinished Input Jobs...");
            }

            StaticData.Log(logFile, "\nChecking for new Input Jobs...\n");

            // Start the Directory Watcher class to scan for new jobs
            DirectoryWatcherThread dirWatch = new DirectoryWatcherThread(IniData, StatusData, Logger);
            if (dirWatch == null)
            {
                Logger.LogError("NewJobsScanThread dirWatch failed to instantiate");
            }

            dirWatch.ProcessCompleted += newJob_DirectoryFound;
            dirWatch.ThreadProc();

            // Wait while scanning for new jobs
            do
            {
                Thread.Sleep(250);
            }
            while ((StaticData.FoundNewJobReadyToRun == false) && (StaticData.ShutdownFlag == false));

            // Exit thread
            StaticData.Log(logFile, String.Format("Exiting Input Job Scan at {0:HH:mm:ss.fff}", DateTime.Now));
        }

        /// <summary>
        /// Method to start new jobs from the Input Buffer 
        /// </summary>
        /// <param name="jobDirectory"></param>
        /// <param name="newJobsFound"></param>
        /// <param name="iniFileData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public static void StartJob(string jobDirectory, bool newJobsFound, IniFileData iniFileData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            // Get job name from directory name
            string job = jobDirectory.Replace(iniFileData.InputDir, "").Remove(0, 1);

            // Start scan for new directory in the Input Buffer
            ScanDirectory scanDir = new ScanDirectory();
            if (scanDir == null)
            {
                Logger.LogError("NewJobsScanThread scanDir failed to instantiate");
            }
            StatusModels.JobXmlData jobXmlData = scanDir.GetJobXmlData(job, iniFileData.InputDir + @"\" + job, logger);

            // Get data found in Job xml file
            JobXmlData xmlData = new JobXmlData();
            if (xmlData == null)
            {
                Logger.LogError("NewJobsScanThread data failed to instantiate");
            }
            xmlData.Job = job;
            xmlData.JobDirectory = jobXmlData.JobDirectory;
            xmlData.JobSerialNumber = jobXmlData.JobSerialNumber;
            xmlData.TimeStamp = jobXmlData.TimeStamp;
            xmlData.XmlFileName = jobXmlData.XmlFileName;

            // Display xmlData found
            string logFile = iniFileData.ProcessLogFile;
            StaticData.Log(logFile, "");
            StaticData.Log(logFile, "Found new Job         = " + xmlData.Job);
            StaticData.Log(logFile, "New Job Directory     = " + xmlData.JobDirectory);
            StaticData.Log(logFile, "New Serial Number     = " + xmlData.JobSerialNumber);
            StaticData.Log(logFile, "New Time Stamp        = " + xmlData.TimeStamp);
            StaticData.Log(logFile, "New Job Xml File      = " + xmlData.XmlFileName);

            StaticData.Log(logFile, String.Format("Job {0} started executing slot {1} at {2:HH:mm:ss.fff}",
                xmlData.Job, StaticData.NumberOfJobsExecuting, DateTime.Now));

            // Create a thread to execute the job, and start it
            JobRunThread jobThread = new JobRunThread(iniFileData.InputDir, newJobsFound, iniFileData, xmlData, statusData, logger);
            if (jobThread == null)
            {
                Logger.LogError("NewJobsScanThread jobThread failed to instantiate");
            }
            jobThread.ThreadProc();

            // Cieck if the shutdown flag is set, exit method
            if (StaticData.ShutdownFlag == true)
            {
                logger.LogInformation("Shutdown NewJobsScanThread job {0}", job);
                return;
            }
        }
    }
}
