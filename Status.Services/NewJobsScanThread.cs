﻿using StatusModels;
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
            // Set Flag for ending directory scan loop
            Console.WriteLine("\new Job Scan Received directories");
            StaticData.FoundNewJobsReady = true;

            // What the heck next?
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

            // Register with the Old Jobs Processing class event and start its thread
            OldJobsScanThread oldJobs = new OldJobsScanThread(iniData, statusData, logger);
            if (oldJobs == null)
            {
                Logger.LogError("NewJobsScanThread oldJobs failed to instantiate");
            }
            oldJobs.ProcessCompleted += oldJob_ProcessCompleted;
            oldJobs.ScanForOldJobs(iniData, statusData, logger);

            // Register with the Directory Watcher class event and start its thread
            DirectoryWatcherThread dirWatch = new DirectoryWatcherThread(iniData, statusData, logger);
            if (dirWatch == null)
            {
                Logger.LogError("NewJobsScanThread dirWatch failed to instantiate");
            }

            dirWatch.ProcessCompleted += newJob_DirectoryFound;
            dirWatch.ThreadProc();
        }

        /// <summary>
        /// A Thread procedure that scans for new jobs
        /// </summary>
        public void ThreadProc()
        {
            thread = new Thread(() => ScanForCurrentNewJobs(IniData, StatusData, Logger));
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
        public static void ScanForCurrentNewJobs(IniFileData iniFileData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            StatusModels.JobXmlData jobXmlData = new StatusModels.JobXmlData();
            if (jobXmlData == null)
            {
                Logger.LogError("ScanForCurrentNewJobs jobXmlDatafailed failed to instantiate");
            }

            List<DirectoryInfo> runDirectoryInfoList = new List<DirectoryInfo>();
            if (runDirectoryInfoList == null)
            {
                Logger.LogError("ScanForNewJobs runDirectoryInfoList failed to instantiate");
            }

            List<String> runDirectoryList = new List<String>();
            if (runDirectoryList == null)
            {
                Logger.LogError("ScanForNewJobs runDirectoryList failed to instantiate");
            }

            DirectoryInfo runDirectoryInfo = new DirectoryInfo(iniFileData.InputDir);
            if (runDirectoryInfo == null)
            {
                Logger.LogError("ScanForNewJobs runDirectoryInfo failed to instantiate");
            }
            runDirectoryInfoList = runDirectoryInfo.EnumerateDirectories().ToList();
            if (runDirectoryInfoList.Count > 0)
            {
                Console.WriteLine("\nProcesssing unfinished new job(s)...");
            }

            foreach (var dir in runDirectoryInfoList)
            {
                // Run the directory list found on initial scan of the Input Buffer
                StartJob(dir.ToString(), iniFileData, statusData, logger);
            }
        }

        /// <summary>
        /// Method to start new jobs from the Input Buffer
        /// </summary>
        /// <param name="jobList"></param>
        /// <param name="iniFileData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public static void StartJob(string jobDirectory, IniFileData iniFileData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            if (StaticData.NumberOfJobsExecuting < iniFileData.ExecutionLimit)
            {
                // Increment counters to track job execution
                StaticData.IncrementNumberOfJobsExecuting();

                // Get job name from directory name
                string job = jobDirectory.Replace(iniFileData.InputDir, "").Remove(0, 1);

                Console.WriteLine("Starting job {0} index {1} at {2:HH:mm:ss.fff}", 
                    job, StaticData.NumberOfJobsExecuting, DateTime.Now);

                // Start scan for new directory in the Input Buffer
                ScanDirectory scanDir = new ScanDirectory();
                if (scanDir == null)
                {
                    Logger.LogError("ScanForNewJobs scanDir failed to instantiate");
                }
                StatusModels.JobXmlData jobXmlData = scanDir.GetJobXmlData(job, iniFileData.InputDir + @"\" + job, logger);

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
                    logger.LogInformation("Shutdown ScanForNewJobs job {0}", job);
                    return;
                }

                Thread.Sleep(iniFileData.ScanTime);
            }
            else
            {
                Thread.Sleep(iniFileData.ScanTime);
            }
        }
    }
}