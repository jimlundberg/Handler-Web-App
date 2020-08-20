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
    public class CurrentInutJobsScanThread
    {
        private static Thread thread;
        public static IniFileData IniData;
        public static List<StatusData> StatusData;
        public static ILogger<StatusRepository> Logger;

        public CurrentInutJobsScanThread() { }

        /// <summary>
        /// Processing complete callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void currentInputJob_ProcessCompleted(object sender, EventArgs e)
        {
            // Set Flag for ending directory scan loop
            Console.WriteLine("\nCurrent Input Job Scan Completed...");
            StaticData.CurrentInputJobsScanComplete = false;
            StaticData.FoundNewJobReadyToRun = false;
            ScanForCurrentInputJobs(IniData, StatusData, Logger);
        }

        /// <summary>
        /// Processing complete callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void newJob_DirectoryFound(object sender, EventArgs e)
        {
            StaticData.Log(IniData.ProcessLogFile, 
                String.Format("\nnewJob_DirectoryFound Received new directory at {0:HH:mm:ss.fff}",
                DateTime.Now));

            // Set Flag for ending directory scan loop
            StaticData.FoundNewJobReadyToRun = true;
        }

        /// <summary>
        /// New jobs Scan thread
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public CurrentInutJobsScanThread(IniFileData iniData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            IniData = iniData;
            StatusData = statusData;
            Logger = logger;
            StaticData.CurrentInputJobsScanComplete = false;
        }

        /// <summary>
        /// A Thread procedure that scans for new jobs
        /// </summary>
        public void ThreadProc()
        {
            thread = new Thread(() => ScanForCurrentInputJobs(IniData, StatusData, Logger));
            if (thread == null)
            {
                Logger.LogError("CurrentInutJobsScanThread thread failed to instantiate");
            }
            thread.Start();
        }

        /// <summary>
        /// Method to scan for new jobs in the Input Buffer
        /// </summary>
        /// <param name="iniFileData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public static void ScanForCurrentInputJobs(IniFileData iniFileData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            string logFile = iniFileData.ProcessLogFile;

            // Register with the Old Jobs Processing class event and start its thread
            CurrentProcessingJobsScanThread currentProcessingJobs = new CurrentProcessingJobsScanThread(IniData, StatusData, Logger);
            if (currentProcessingJobs == null)
            {
                Logger.LogError("CurrentInutJobsScanThread oldJobs failed to instantiate");
            }
            currentProcessingJobs.ProcessCompleted += currentInputJob_ProcessCompleted;
            currentProcessingJobs.ThreadProc();

            // Wait while scanning for unfinished Processing jobs
            do
            {
                Thread.Sleep(250);
            }
            while ((StaticData.CurrentProcessingJobsScanComplete == false) && (StaticData.ShutdownFlag == false));

            StaticData.Log(logFile, "\nChecking for unfinished Input Jobs...");

            StatusModels.JobXmlData jobXmlData = new StatusModels.JobXmlData();
            if (jobXmlData == null)
            {
                Logger.LogError("CurrentInutJobsScanThread jobXmlDatafailed failed to instantiate");
            }

            List<DirectoryInfo> runDirectoryInfoList = new List<DirectoryInfo>();
            if (runDirectoryInfoList == null)
            {
                Logger.LogError("CurrentInutJobsScanThread runDirectoryInfoList failed to instantiate");
            }
            List<String> runDirectoryList = new List<String>();
            if (runDirectoryList == null)
            {
                Logger.LogError("CurrentInutJobsScanThread runDirectoryList failed to instantiate");
            }
            DirectoryInfo runDirectoryInfo = new DirectoryInfo(iniFileData.InputDir);
            if (runDirectoryInfo == null)
            {
                Logger.LogError("CurrentInutJobsScanThread runDirectoryInfo failed to instantiate");
            }

            // Get the list of directories from the Input Buffer
            bool currentInputJobsFound = false;
            runDirectoryInfoList = runDirectoryInfo.EnumerateDirectories().ToList();
            if (runDirectoryInfoList.Count > 0)
            {
                currentInputJobsFound = true;
                StaticData.Log(logFile, "\nProcesssing unfinished Input jobs...");
            }
            else
            {
                StaticData.Log(logFile, "\nNo unfinished Input Jobs Found...");
            }

            // Start the jobs in the directory list found on initial scan of the Input Buffer
            foreach (var dir in runDirectoryInfoList)
            {
                if (StaticData.NumberOfJobsExecuting < iniFileData.ExecutionLimit)
                {
                    CurrentInutJobsScanThread newJobsScanThread = new CurrentInutJobsScanThread();
                    newJobsScanThread.StartJob(dir.ToString(), currentInputJobsFound, IniData, StatusData, Logger);
                    Thread.Sleep(iniFileData.ScanTime);
                }
                else
                {
                    // Get job name from directory name
                    string job = dir.ToString().Replace(IniData.InputDir, "").Remove(0, 1);
                    StaticData.NewInputJobsToRun.Add(job);
                }

                currentInputJobsFound = true;
            }

            if (runDirectoryInfoList.Count() > 0)
            {
                StaticData.Log(logFile, "\nUnfinished Input Jobs waiting...");
            }
            else if (currentInputJobsFound)
            {
                StaticData.Log(logFile, "\nNo more unfinished Input Jobs...");
            }

            StaticData.Log(logFile, "\nWatching for new Input Jobs...");

            // Start the Directory Watcher class to scan for new jobs
            DirectoryWatcherThread dirWatch = new DirectoryWatcherThread(IniData, StatusData, Logger);
            if (dirWatch == null)
            {
                Logger.LogError("CurrentInutJobsScanThread dirWatch failed to instantiate");
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
        /// <param name="job"></param>
        /// <param name="newJobsFound"></param>
        /// <param name="iniFileData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public void StartJob(string jobDirectory, bool newJobsFound, IniFileData iniFileData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            // Get job name from directory name
            string job = jobDirectory.Replace(iniFileData.InputDir, "").Remove(0, 1);
            string logFile = iniFileData.ProcessLogFile;

            // Start scan for new directory in the Input Buffer
            ScanDirectory scanDir = new ScanDirectory();
            if (scanDir == null)
            {
                Logger.LogError("CurrentInutJobsScanThread scanDir failed to instantiate");
            }
            JobXmlData jobXmlData = scanDir.GetJobXmlData(job, jobDirectory, logger);

            // Get data found in Job xml file
            JobXmlData xmlData = new JobXmlData();
            if (xmlData == null)
            {
                Logger.LogError("CurrentInutJobsScanThread data failed to instantiate");
            }
            xmlData.Job = job;
            xmlData.JobDirectory = jobXmlData.JobDirectory;
            xmlData.JobSerialNumber = jobXmlData.JobSerialNumber;
            xmlData.TimeStamp = jobXmlData.TimeStamp;
            xmlData.XmlFileName = jobXmlData.XmlFileName;

            // Display xmlData found
            StaticData.Log(logFile, " ");
            StaticData.Log(logFile, "Found new Input Job   = " + xmlData.Job);
            StaticData.Log(logFile, "New Job directory     = " + xmlData.JobDirectory);
            StaticData.Log(logFile, "New Serial Number     = " + xmlData.JobSerialNumber);
            StaticData.Log(logFile, "New Time Stamp        = " + xmlData.TimeStamp);
            StaticData.Log(logFile, "New Job Xml File      = " + xmlData.XmlFileName);

            StaticData.Log(logFile, String.Format("Job {0} started executing slot {1} at {2:HH:mm:ss.fff}",
                xmlData.Job, StaticData.NumberOfJobsExecuting, DateTime.Now));

            // Create a thread to execute the job, and start it
            JobRunThread thread = new JobRunThread(iniFileData.InputDir, newJobsFound, iniFileData, xmlData, statusData, logger);
            if (thread == null)
            {
                Logger.LogError("CurrentInutJobsScanThread thread failed to instantiate");
            }
            thread.ThreadProc();

            // Cieck if the shutdown flag is set, exit method
            if (StaticData.ShutdownFlag == true)
            {
                logger.LogInformation("Shutdown CurrentInutJobsScanThread job {0}", job);
                return;
            }
        }
    }
}
