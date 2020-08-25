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
    public class CurrentInputJobsScanThread
    {
        private static Thread thread;
        public static IniFileData IniData;
        public static List<StatusData> StatusData;
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
            StatusData = statusData;
            Logger = logger;
            StaticClass.CurrentInputJobsScanComplete = false;
        }

        /// <summary>
        /// Processing complete callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void currentInputJob_ProcessCompleted(object sender, EventArgs e)
        {
            string job = e.ToString();

            StaticClass.Log(IniData.ProcessLogFile,
                String.Format("\nCurrent Input Job Scan Received new job {0} at {0:HH:mm:ss.fff}",
                job, DateTime.Now));

            // Set Flag for ending file scan loop
            StaticClass.CurrentInputJobsScanComplete = true;
        }

        /// <summary>
        /// Processing complete callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void newJob_DirectoryFound(object sender, EventArgs e)
        {
            string directory = e.ToString();

            StaticClass.Log(IniData.ProcessLogFile,
                String.Format("\nnewJob_DirectoryFound Received new directory {0} at {1:HH:mm:ss.fff}",
                directory, DateTime.Now));

            // Set Flag for ending directory scan loop
            StaticClass.NewProcessingJobsToRun.Add(directory);
        }

        /// <summary>
        /// A Thread procedure that scans for new jobs
        /// </summary>
        public void ThreadProc()
        {
            thread = new Thread(() => CheckForCurrentInputJobs(IniData, StatusData, Logger));
            if (thread == null)
            {
                Logger.LogError("CurrentInputJobsScanThread thread failed to instantiate");
            }
            thread.Start();
        }

        /// <summary>
        /// Method to scan for new jobs in the Input Buffer
        /// </summary>
        /// <param name="iniFileData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public static void CheckForCurrentInputJobs(IniFileData iniData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            string logFile = iniData.ProcessLogFile;

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
                Thread.Sleep(StaticClass.ThreadWaitTime);

                if (StaticClass.ShutdownFlag == true)
                {
                    StaticClass.Log(iniData.ProcessLogFile,
                        String.Format("\nShutdown CurrentInputJobsScanThread CheckForCurrentInputJobs at {0:HH:mm:ss.fff}", DateTime.Now));
                    return;
                }
            }
            while (StaticClass.CurrentProcessingJobScanComplete == false);

            StaticClass.Log(logFile, "\nChecking for unfinished Input Jobs...");

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
                StaticClass.Log(logFile, "\nUnfinished Input Jobs waiting...");
            }
            else
            {
                StaticClass.Log(logFile, "\nNo unfinished Input Jobs Found...");
            }

            // Start the jobs in the directory list found on initial scan of the Input Buffer
            foreach (DirectoryInfo dir in InputDirectoryInfoList)
            {
                // Get job name by clearing the Input Directory string
                string job = dir.ToString().Replace(IniData.InputDir, "").Remove(0, 1);
                string directory = dir.ToString();

                if (StaticClass.NumberOfJobsExecuting < iniData.ExecutionLimit)
                {
                    // Create new Input job Scan thread and run
                    CurrentInputJobsScanThread newInputJobsScanThread = new CurrentInputJobsScanThread();
                    newInputJobsScanThread.StartInputJob(directory, IniData, StatusData, Logger);
                    Thread.Sleep(StaticClass.ScanWaitTime);
                }
                else
                {
                    // Add currently unfinished job to Input Jobs run list
                    StaticClass.NewInputJobsToRun.Add(job);
                }
            }

            // Flag that the Current Input job(s) scan is complete
            StaticClass.CurrentInputJobsScanComplete = true;

            StaticClass.Log(logFile, "\nWatching for new Input Jobs...");

            // Start the Directory Watcher class to scan for new jobs
            DirectoryWatcherThread dirWatch = new DirectoryWatcherThread(IniData, StatusData, Logger);
            if (dirWatch == null)
            {
                Logger.LogError("CurrentInputJobsScanThread dirWatch failed to instantiate");
            }

            dirWatch.ProcessCompleted += newJob_DirectoryFound;
            dirWatch.ThreadProc();

            // Wait forever while scanning for new jobs
            do
            {
                if (StaticClass.NewInputJobsToRun.Count > 0)
                {
                    // Check if there are jobs waiting to run
                    for (int i = 0; i < StaticClass.NewInputJobsToRun.Count; i++)
                    {
                        if (StaticClass.NumberOfJobsExecuting < iniData.ExecutionLimit)
                        {
                            string job = StaticClass.NewInputJobsToRun[i];
                            string directory = iniData.InputDir + @"\" + job;
                            CurrentInputJobsScanThread newInputJobsScan = new CurrentInputJobsScanThread();
                            newInputJobsScan.StartInputJob(directory, iniData, statusData, logger);
                            Thread.Sleep(StaticClass.ScanWaitTime);
                        }
                    }
                }

                Thread.Sleep(StaticClass.ThreadWaitTime);
            }
            while (StaticClass.ShutdownFlag == false);
        }

        /// <summary>
        /// Method to start new jobs from the Input Buffer 
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="iniFileData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public void StartInputJob(string directory, IniFileData iniFileData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            // Get job name from directory name
            string logFile = iniFileData.ProcessLogFile;
            string job = directory.Replace(iniFileData.InputDir, "").Remove(0, 1);

            if (StaticClass.NumberOfJobsExecuting < iniFileData.ExecutionLimit)
            {
                // Start scan for new directory in the Input Buffer
                ScanDirectory scanDir = new ScanDirectory();
                if (scanDir == null)
                {
                    Logger.LogError("CurrentInputJobsScanThread scanDir failed to instantiate");
                }

                // Get data found in Job xml file
                JobXmlData jobXmlData = scanDir.GetJobXmlData(job, directory, logger);
                if (jobXmlData == null)
                {
                    Logger.LogError("CurrentInputJobsScanThread scanDir GetJobXmlData failed");
                }

                jobXmlData.Job = job;
                jobXmlData.JobDirectory = jobXmlData.JobDirectory;
                jobXmlData.JobSerialNumber = jobXmlData.JobSerialNumber;
                jobXmlData.TimeStamp = jobXmlData.TimeStamp;
                jobXmlData.XmlFileName = jobXmlData.XmlFileName;

                // Display xmlData found
                StaticClass.Log(logFile, "");
                StaticClass.Log(logFile, "Found Input Job             : " + jobXmlData.Job);
                StaticClass.Log(logFile, "New Job directory           : " + jobXmlData.JobDirectory);
                StaticClass.Log(logFile, "New Serial Number           : " + jobXmlData.JobSerialNumber);
                StaticClass.Log(logFile, "New Time Stamp              : " + jobXmlData.TimeStamp);
                StaticClass.Log(logFile, "New Job Xml File            : " + jobXmlData.XmlFileName);

                StaticClass.Log(logFile, String.Format("Started Input Job {0} executing slot {1} at {2:HH:mm:ss.fff}",
                    jobXmlData.Job, StaticClass.NumberOfJobsExecuting + 1, DateTime.Now));

                // Create a thread to run the job, and then start the thread
                JobRunThread thread = new JobRunThread(DirectoryScanType.INPUT_BUFFER, jobXmlData, iniFileData, statusData, logger);
                if (thread == null)
                {
                    Logger.LogError("CurrentInputJobsScanThread thread failed to instantiate");
                }
                thread.ThreadProc();

                // Remove job after run thread launched
                StaticClass.NewInputJobsToRun.Remove(job);

                // Cieck if the shutdown flag is set, exit method
                if (StaticClass.ShutdownFlag == true)
                {
                    Console.WriteLine(String.Format("\nShutdown CurrentInputJobsScanThread StartInputJob of job {0} at {1:HH:mm:ss.fff}",
                        job, DateTime.Now));
                    return;
                }
            }
            else
            {
                // Add jobs over execution limit to the Input Job list
                StaticClass.NewInputJobsToRun.Add(job);
            }
        }
    }
}
