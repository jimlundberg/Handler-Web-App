using Microsoft.Extensions.Logging;
using Status.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Status.Services
{
    /// <summary>
    /// Class to run the whole monitoring process as a thread
    /// </summary>
    public class CurrentInputJobsScanThread
    {
        private static IniFileData IniData;
        private static List<StatusData> StatusDataList;
        public static ILogger<StatusRepository> Logger;

        /// <summary>
        /// Current Input Jobs Scan thread default constructor
        /// </summary>
        public CurrentInputJobsScanThread() { }

        /// <summary>
        /// New jobs Scan thread
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public CurrentInputJobsScanThread(IniFileData iniData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            IniData = iniData;
            StatusDataList = statusData;
            Logger = logger;
        }

        /// <summary>
        /// Processing complete callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void currentInputJob_ProcessCompleted(object sender, EventArgs e)
        {
            string job = e.ToString();

            StaticClass.Log(String.Format("\nCurrent Input Job Scan Received new job {0} at {1:HH:mm:ss.fff}",
                job, DateTime.Now));
        }

        /// <summary>
        /// Processing complete callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void newJob_DirectoryFound(object sender, EventArgs e)
        {
            string job = e.ToString();

            // Set Flag for ending directory scan loop
            StaticClass.InputJobsToRun.Add(job);

            StaticClass.Log(String.Format("Input Job Scan detected and added job {0} to Input job list at {1:HH:mm:ss.fff}",
                job, DateTime.Now));
        }

        /// <summary>
        /// A Thread procedure that scans for new jobs
        /// </summary>
        public void ThreadProc()
        {
            StaticClass.CurrentInputJobsScanThreadHandle = new Thread(() =>
                CheckForCurrentInputJobs(IniData, StatusDataList, Logger));
            
            if (StaticClass.CurrentInputJobsScanThreadHandle == null)
            {
                Logger.LogError("CurrentInputJobsScanThread thread failed to instantiate");
            }
            StaticClass.CurrentInputJobsScanThreadHandle.Start();
        }

        /// <summary>
        /// Method to scan for new jobs in the Input Buffer
        /// </summary>
        /// <param name="iniFileData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public static void CheckForCurrentInputJobs(IniFileData iniData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            // Register with the Old Jobs Processing class event and start its thread
            CurrentProcessingJobsScanThread currentProcessingJobs = new CurrentProcessingJobsScanThread(iniData, statusData, logger);
            if (currentProcessingJobs == null)
            {
                Logger.LogError("CurrentInputJobsScanThread oldJobs failed to instantiate");
            }
            currentProcessingJobs.ProcessCompleted += currentInputJob_ProcessCompleted;
            currentProcessingJobs.ThreadProc();

            // Wait while scanning for unfinished Processing jobs
            do
            {
                Thread.Yield();

                if (StaticClass.ShutdownFlag == true)
                {
                    StaticClass.Log(String.Format("\nShutdown CurrentInputJobsScanThread CheckForCurrentInputJobs at {0:HH:mm:ss.fff}", DateTime.Now));
                    return;
                }

                // Check if the pause flag is set, then wait for reset
                if (StaticClass.PauseFlag == true)
                {
                    do
                    {
                        Thread.Yield();
                    }
                    while (StaticClass.PauseFlag == true);
                }
            }
            while (StaticClass.UnfinishedProcessingJobsScanComplete == false);

            StaticClass.Log("\nChecking for unfinished Input Jobs...");

            // Check and delete expired Input Buffer job directories first
            StaticClass.CheckForInputBufferTimeLimits(iniData);

            DirectoryInfo InputDirectoryInfo = new DirectoryInfo(iniData.InputDir);
            if (InputDirectoryInfo == null)
            {
                Logger.LogError("CurrentInputJobsScanThread InputDirectoryInfo failed to instantiate");
            }

            // Get the current list of directories from the Input Buffer
            List<DirectoryInfo> InputDirectoryInfoList = InputDirectoryInfo.EnumerateDirectories().ToList();
            if (InputDirectoryInfoList == null)
            {
                Logger.LogError("CurrentInputJobsScanThread InputDirectoryInfoList failed to instantiate");
            }

            if (InputDirectoryInfoList.Count > 0)
            {
                StaticClass.Log("\nUnfinished Input Jobs waiting...\n");
            }
            else
            {
                StaticClass.Log("\nNo unfinished Input Jobs Found...");
            }

            // Start the jobs in the directory list found on initial scan of the Input Buffer
            foreach (DirectoryInfo dir in InputDirectoryInfoList)
            {
                // Get job name by clearing the Input Directory string
                string job = dir.ToString().Replace(IniData.InputDir, "").Remove(0, 1);
                string directory = dir.ToString();

                if (StaticClass.NumberOfJobsExecuting < iniData.ExecutionLimit)
                {
                    // Create new Input job start thread and run
                    CurrentInputJobsScanThread newInputJobsScanThread = new CurrentInputJobsScanThread();
                    newInputJobsScanThread.StartInputJob(directory, IniData, StatusDataList, Logger);

                    // Throttle the Job startups
                    Thread.Sleep(StaticClass.ScanWaitTime);
                }
                else
                {
                    // Add currently unfinished job to Input Jobs run list
                    StaticClass.InputJobsToRun.Add(job);

                    StaticClass.Log(String.Format("Input Job Scan added waiting job {0} to Input job list at {1:HH:mm:ss.fff}",
                        job, DateTime.Now));
                }
            }

            StaticClass.Log("\nWatching for new Input Jobs...");

            // Start the Directory Watcher class to scan for new jobs
            DirectoryWatcherThread dirWatch = new DirectoryWatcherThread(IniData, StatusDataList, Logger);
            if (dirWatch == null)
            {
                Logger.LogError("CurrentInputJobsScanThread dirWatch failed to instantiate");
            }

            dirWatch.ProcessCompleted += newJob_DirectoryFound;
            dirWatch.ThreadProc();

            // Wait forever while scanning for new jobs
            do
            {
                if (StaticClass.InputJobsToRun.Count > 0)
                {
                    if (StaticClass.NumberOfJobsExecuting < iniData.ExecutionLimit)
                    {
                        // Check if there are jobs waiting to run
                        for (int i = 0; i < StaticClass.InputJobsToRun.Count; i++)
                        {
                            string job = StaticClass.InputJobsToRun[i];
                            string directory = iniData.InputDir + @"\" + job;
                            CurrentInputJobsScanThread newInputJobsScan = new CurrentInputJobsScanThread();
                            newInputJobsScan.StartInputJob(directory, iniData, statusData, logger);

                            // Throttle the Job startups
                            Thread.Sleep(StaticClass.ScanWaitTime);
                        }
                    }
                }

                StaticClass.CheckForInputBufferTimeLimits(iniData);

                Thread.Yield();
            }
            while (StaticClass.ShutdownFlag == false);
        }

        /// <summary>
        /// Method to start new jobs from the Input Buffer 
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="iniData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public void StartInputJob(string directory, IniFileData iniData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            // Get job name from directory name
            string job = directory.Replace(iniData.InputDir, "").Remove(0, 1);

            if (StaticClass.NumberOfJobsExecuting < iniData.ExecutionLimit)
            {
                // Get data found in Job xml file
                JobXmlData jobXmlData = StaticClass.GetJobXmlData(directory, iniData, DirectoryScanType.INPUT_BUFFER);
                if (jobXmlData == null)
                {
                    Logger.LogError("CurrentInputJobsScanThread GetJobXmlData failed");
                }

                jobXmlData.Job = job;
                jobXmlData.JobDirectory = jobXmlData.JobDirectory;
                jobXmlData.JobSerialNumber = jobXmlData.JobSerialNumber;
                jobXmlData.TimeStamp = jobXmlData.TimeStamp;
                jobXmlData.XmlFileName = jobXmlData.XmlFileName;

                // Display xmlData found
                StaticClass.Log("");
                StaticClass.Log("Found Input Job             : " + jobXmlData.Job);
                StaticClass.Log("New Job directory           : " + jobXmlData.JobDirectory);
                StaticClass.Log("New Serial Number           : " + jobXmlData.JobSerialNumber);
                StaticClass.Log("New Time Stamp              : " + jobXmlData.TimeStamp);
                StaticClass.Log("New Job Xml File            : " + jobXmlData.XmlFileName);

                StaticClass.Log(String.Format("Started Input Job {0} executing slot {1} at {2:HH:mm:ss.fff}",
                    jobXmlData.Job, StaticClass.NumberOfJobsExecuting + 1, DateTime.Now));

                // Create a thread to run the job, and then start the thread
                JobRunThread thread = new JobRunThread(DirectoryScanType.INPUT_BUFFER, jobXmlData, iniData, statusData, logger);
                if (thread == null)
                {
                    Logger.LogError("CurrentInputJobsScanThread thread failed to instantiate");
                }
                thread.ThreadProc();

                // Remove Input job after start thread complete
                StaticClass.InputJobsToRun.Remove(job);
                StaticClass.InputFileScanComplete[job] = true;

                // Cieck if the shutdown flag is set, exit method
                if (StaticClass.ShutdownFlag == true)
                {
                    StaticClass.Log(String.Format("\nShutdown CurrentInputJobsScanThread StartInputJob of job {0} at {1:HH:mm:ss.fff}",
                        job, DateTime.Now));
                    return;
                }

                // Check if the pause flag is set, then wait for reset
                if (StaticClass.PauseFlag == true)
                {
                    do
                    {
                        Thread.Yield();
                    }
                    while (StaticClass.PauseFlag == true);
                }
            }
        }
    }
}
