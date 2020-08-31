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
    public class InputJobsScanThread
    {
        private readonly IniFileData IniData;
        private readonly List<StatusData> StatusDataList;
        public static ILogger<StatusRepository> Logger;

        /// <summary>
        /// Current Input Jobs Scan thread default constructor
        /// </summary>
        public InputJobsScanThread() { }

        /// <summary>
        /// New jobs Scan thread
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public InputJobsScanThread(IniFileData iniData, List<StatusData> statusData, ILogger<StatusRepository> logger)
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

            StaticClass.Log(String.Format("\nCurrent Input Job Scan Received new Input Job {0} at {1:HH:mm:ss.fff}",
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
            // Check for expired Input jobs
            StaticClass.CheckForInputBufferTimeLimits(IniData);

            StaticClass.InputJobsScanThreadHandle = new Thread(() =>
                CheckForUnfinishedInputJobs(IniData, StatusDataList, Logger));
            
            if (StaticClass.InputJobsScanThreadHandle == null)
            {
                Logger.LogError("InputJobsScanThread thread failed to instantiate");
            }
            StaticClass.InputJobsScanThreadHandle.Start();
        }

        /// <summary>
        /// Method to scan for unfinished jobs in the Input Buffer
        /// </summary>
        /// <param name="iniFileData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public void CheckForUnfinishedInputJobs(IniFileData iniData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            // Check and delete expired Input Buffer job directories first
            StaticClass.CheckForInputBufferTimeLimits(iniData);

            // Register with the Input Buffer Jobs class event and start its thread
            ProcessingJobsScanThread currentProcessingJobs = new ProcessingJobsScanThread(iniData, statusData, logger);
            if (currentProcessingJobs == null)
            {
                Logger.LogError("InputJobsScanThread currentProcessingJobs failed to instantiate");
            }
            currentProcessingJobs.ProcessCompleted += currentInputJob_ProcessCompleted;
            currentProcessingJobs.ThreadProc();

            // Wait while scanning for unfinished Processing jobs
            do
            {
                Thread.Yield();

                if (StaticClass.ShutdownFlag == true)
                {
                    StaticClass.Log(String.Format("\nShutdown InputJobsScanThread CheckForUnfinishedInputJobs at {0:HH:mm:ss.fff}", DateTime.Now));
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

            DirectoryInfo InputDirectoryInfo = new DirectoryInfo(iniData.InputDir);
            if (InputDirectoryInfo == null)
            {
                Logger.LogError("InputJobsScanThread InputDirectoryInfo failed to instantiate");
            }

            // Get the current list of directories from the Input Buffer
            List<DirectoryInfo> InputDirectoryInfoList = InputDirectoryInfo.EnumerateDirectories().ToList();
            if (InputDirectoryInfoList == null)
            {
                Logger.LogError("InputJobsScanThread InputDirectoryInfoList failed to instantiate");
            }

            if (InputDirectoryInfoList.Count > 0)
            {
                StaticClass.Log("\nUnfinished Input Jobs waiting...");
            }
            else
            {
                StaticClass.Log("\nNo unfinished Input Jobs found...");
            }

            // Start the jobs in the directory list found for the Input Buffer
            bool foundUnfinishedJobs = false;
            for (int i = 0; i < InputDirectoryInfoList.Count; i++)
            {
                if (StaticClass.NumberOfJobsExecuting < iniData.ExecutionLimit)
                {
                    DirectoryInfo dirInfo = InputDirectoryInfoList[i];
                    string directory = dirInfo.ToString();
                    string job = directory.ToString().Replace(IniData.InputDir, "").Remove(0, 1);

                    StaticClass.Log(String.Format("\nStarting Input Job {0} at {1:HH:mm:ss.fff}", directory, DateTime.Now));

                    StaticClass.InputFileScanComplete[job] = false;
                    InputJobsScanThread inputJobsScanThread = new InputJobsScanThread();
                    inputJobsScanThread.StartInputJob(directory, iniData, statusData, logger);
                    InputDirectoryInfoList.Remove(dirInfo);

                    // Throttle the Job startups
                    Thread.Sleep(StaticClass.ScanWaitTime);
                }
                else
                {
                    foundUnfinishedJobs = true;
                }

                if (foundUnfinishedJobs == true)
                {
                    StaticClass.Log("\nMore unfinished Input jobs then execution slots found...\n");
                }

                // Start the jobs in the directory list found on initial scan of the Input Buffer
                foreach (DirectoryInfo dirInfo in InputDirectoryInfoList)
                {
                    string directory = dirInfo.ToString();
                    string job = directory.Replace(IniData.InputDir, "").Remove(0, 1);
                    StaticClass.InputJobsToRun.Add(job);
                    StaticClass.Log(String.Format("Unfinished Input jobs check added job {0} to Input Job waiting list", job));
                }
            }

            // Clear the Directory Info List after done with it
            InputDirectoryInfoList.Clear();

            // Start the Directory Watcher class to scan for new jobs
            DirectoryWatcherThread dirWatch = new DirectoryWatcherThread(IniData, Logger);
            if (dirWatch == null)
            {
                Logger.LogError("InputJobsScanThread dirWatch failed to instantiate");
            }

            dirWatch.ProcessCompleted += newJob_DirectoryFound;
            dirWatch.ThreadProc();

            // Run check loop until all unfinished Input jobs are complete
            do
            {
                // Check if the shutdown flag is set, exit method
                if (StaticClass.ShutdownFlag == true)
                {
                    StaticClass.Log(String.Format("\nShutdown InputJobsScanThread CheckForCurrentProcessingJobs at {0:HH:mm:ss.fff}", DateTime.Now));
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

                // Wait a sec between scans for unfinished Input jobs
                Thread.Sleep(1000);

                // Run any unfinished Processing jobs
                RunUnfinishedInputJobs(IniData, StatusDataList, Logger);
            }
            while (StaticClass.InputJobsToRun.Count > 0);
        }

        /// <summary>
        /// Check for unfinished Input Buffer jobs
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public void RunUnfinishedInputJobs(IniFileData iniData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            // Check if there are unfinished Input jobs waiting to run
            for (int i = 0; i < StaticClass.InputJobsToRun.Count; i++)
            {
                if (StaticClass.NumberOfJobsExecuting < iniData.ExecutionLimit)
                {
                    string job = StaticClass.InputJobsToRun[i];
                    string directory = iniData.InputDir + @"\" + job;

                    StaticClass.Log(String.Format("\nStarting Input Job {0} at {1:HH:mm:ss.fff}", directory, DateTime.Now));

                    StaticClass.InputFileScanComplete[job] = false;

                    InputJobsScanThread unfinishedInputJobsScan = new InputJobsScanThread();
                    unfinishedInputJobsScan.StartInputJob(directory, iniData, statusData, logger);

                    StaticClass.InputJobsToRun.Remove(job);

                    // Throttle the Job startups
                    Thread.Sleep(StaticClass.ScanWaitTime);
                }
            }
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
            // Get data found in Job xml file
            JobXmlData jobXmlData = StaticClass.GetJobXmlFileInfo(directory, iniData, DirectoryScanType.INPUT_BUFFER);
            if (jobXmlData == null)
            {
                Logger.LogError("InputJobsScanThread GetJobXmlData failed");
            }

            // Check that the xml job and directory job strings match
            string job = directory.Replace(iniData.InputDir, "").Remove(0, 1);
            if (job != jobXmlData.Job)
            {
                logger.LogError(String.Format("Input Jobs don't match {0} {1}", job, jobXmlData.Job));
            }

            // Display job xml data found
            StaticClass.Log("Input Job                      : " + jobXmlData.Job);
            StaticClass.Log("Input Job Directory            : " + jobXmlData.JobDirectory);
            StaticClass.Log("Input Job Serial Number        : " + jobXmlData.JobSerialNumber);
            StaticClass.Log("Input Job Time Stamp           : " + jobXmlData.TimeStamp);
            StaticClass.Log("Input Job Xml File             : " + jobXmlData.XmlFileName);

            StaticClass.Log(String.Format("Started Input Job {0} executing Slot {1} at {2:HH:mm:ss.fff}",
                jobXmlData.Job, StaticClass.NumberOfJobsExecuting + 1, DateTime.Now));

            // Create a thread to run the job, and then start the thread
            JobRunThread thread = new JobRunThread(DirectoryScanType.INPUT_BUFFER, jobXmlData, iniData, statusData, logger);
            if (thread == null)
            {
                Logger.LogError("InputJobsScanThread thread failed to instantiate");
            }
            thread.ThreadProc();

            // Check if the shutdown flag is set, exit method
            if (StaticClass.ShutdownFlag == true)
            {
                StaticClass.Log(String.Format("\nShutdown InputJobsScanThread StartInputJob of job {0} at {1:HH:mm:ss.fff}",
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
