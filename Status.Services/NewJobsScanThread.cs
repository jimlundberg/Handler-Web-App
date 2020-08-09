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
        // State information used in the scanning task
        private static Thread thread;
        private static IniFileData IniData;
        private static List<StatusData> StatusData;
        public volatile bool endProcess = false;
        public static ILogger<StatusRepository> Logger;

        /// <summary>
        /// Processing complete callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void oldJob_ProcessCompleted(object sender, EventArgs e)
        {
            // Set Flag for ending directory scan loop
            Console.WriteLine("Old Job Scan Completed!");
            StaticData.oldJobScanComplete = true;
            ScanForNewJobs(IniData, StatusData, Logger);
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

            // Register with the Old Jobs Event and start its thread
            OldJobsScanThread oldJobs = new OldJobsScanThread(iniData, statusData, logger);
            if (oldJobs == null)
            {
                Logger.LogError("NewJobsScanThread oldJobs failed to instantiate");
            }
            oldJobs.ProcessCompleted += oldJob_ProcessCompleted;
            oldJobs.ScanForOldJobs(iniData, statusData, logger);
        }

        /// <summary>
        /// A Thread procedure that scans for new jobs
        /// </summary>
        public void ThreadProc()
        {
            thread = new Thread(() => ScanForNewJobs(IniData, StatusData, Logger));
            if (thread == null)
            {
                Logger.LogError("NewJobScanThread ScanForNewJobs thread failed to instantiate");
            }
            thread.Start();
        }

        /// <summary>
        /// Method to scan for new jobs in the Input Buffer
        /// </summary>
        /// <param name="iniFileData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public static void ScanForNewJobs(IniFileData iniFileData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            StatusModels.JobXmlData jobXmlData = new StatusModels.JobXmlData();
            List<string> newDirectoryList = new List<string>();
            if (newDirectoryList == null)
            {
                Logger.LogError("ScanForNewJobs newDirectoryList failed to instantiate");
            }
            List<string> runDirectoryList = new List<string>();
            if (runDirectoryList == null)
            {
                Logger.LogError("ScanForNewJobs runDirectoryList failed to instantiate");
            }

            Console.WriteLine("Scanning for new job(s)...");

            while (true)
            {
                // Get new directory list
                var newDirectoryInfo = new DirectoryInfo(iniFileData.InputDir);
                if (newDirectoryInfo == null)
                {
                    Logger.LogError("ScanForNewJobs newDirectoryInfo failed to instantiate");
                }

                var newDirectoryInfoList = newDirectoryInfo.EnumerateDirectories().ToList();
                foreach (var subdirectory in newDirectoryInfoList)
                {
                    newDirectoryList.Add(subdirectory.ToString());
                }
                newDirectoryList.Sort();

                // Look for a difference between new and run directory lists
                if (newDirectoryList != runDirectoryList)
                {
                    runDirectoryList = newDirectoryList;
                }

                for (int i = 0; i < runDirectoryList.Count(); i++)
                {
                    if (StaticData.NumberOfJobsExecuting < iniFileData.ExecutionLimit)
                    {
                        // Increment counters to track job execution
                        StaticData.IncrementNumberOfJobsExecuting();

                        // Get job name from directory name
                        String job = runDirectoryList[i].Replace(iniFileData.InputDir, "").Remove(0, 1);

                        // Start scan for new directory in the Input Buffer
                        ScanDirectory scanDir = new ScanDirectory();
                        if (scanDir == null)
                        {
                            Logger.LogError("ScanForNewJobs scanDir failed to instantiate");
                        }
                        jobXmlData = scanDir.GetJobXmlData(job, iniFileData.InputDir + @"\" + job, logger);

                        // Get data found in Job xml file
                        StatusModels.StatusMonitorData xmlData = new StatusModels.StatusMonitorData();
                        if (xmlData == null)
                        {
                            Logger.LogError("ScanForNewJobs data failed to instantiate");
                        }
                        xmlData.Job = job;
                        xmlData.JobDirectory = jobXmlData.JobDirectory;
                        xmlData.JobSerialNumber = jobXmlData.JobSerialNumber;
                        xmlData.TimeStamp = jobXmlData.TimeStamp;
                        xmlData.XmlFileName = jobXmlData.XmlFileName;
                        xmlData.JobIndex = StaticData.RunningJobsIndex++;

                        // Display xmlData found
                        Console.WriteLine("");
                        Console.WriteLine("Found new Job         = " + xmlData.Job);
                        Console.WriteLine("New Job Directory     = " + xmlData.JobDirectory);
                        Console.WriteLine("New Serial Number     = " + xmlData.JobSerialNumber);
                        Console.WriteLine("New Time Stamp        = " + xmlData.TimeStamp);
                        Console.WriteLine("New Job Xml File      = " + xmlData.XmlFileName);

                        Console.WriteLine("Job {0} started executing slot {1} at {2:HH:mm:ss.fff}", 
                            xmlData.Job, StaticData.NumberOfJobsExecuting, DateTime.Now);

                        // Create a thread to execute the task, and then start the thread.
                        JobRunThread jobThread = new JobRunThread(iniFileData.InputDir, iniFileData, xmlData, statusData, logger);
                        if (jobThread == null)
                        {
                            Logger.LogError("ScanForNewJobs jobThread failed to instantiate");
                        }
                        jobThread.ThreadProc();

                        // Cieck if the shutdown flag is set, exit method
                        if (StaticData.ShutdownFlag == true)
                        {
                            logger.LogInformation("Shutdown ScanForNewJobs job {0}", xmlData.Job);
                            return;
                        }

                        // Remove job from the run list when run
                        runDirectoryList.Remove(runDirectoryList[i]);
                    }
                    else
                    {
                        Thread.Sleep(iniFileData.ScanTime);
                    }
                }

                // Time between scans
                Thread.Sleep(iniFileData.ScanTime);
            }
        }
    }
}
