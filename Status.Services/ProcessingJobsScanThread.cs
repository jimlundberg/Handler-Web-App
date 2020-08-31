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
    public class ProcessingJobsScanThread
    {
        private readonly IniFileData IniData;
        private readonly List<StatusData> StatusDataList;
        public event EventHandler ProcessCompleted;
        private static readonly Object DelLock = new Object();
        public static ILogger<StatusRepository> Logger;

        /// <summary>
        /// Current Processing Jobs Scan thread default constructor
        /// </summary>
        public ProcessingJobsScanThread() { }

        /// <summary>
        /// Old Jobs Scan Thread constructor receiving data buffers
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public ProcessingJobsScanThread(IniFileData iniData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            IniData = iniData;
            StatusDataList = statusData;
            Logger = logger;
            StaticClass.UnfinishedProcessingJobsScanComplete = false;
        }

        /// <summary>
        /// On Process Completed event callback
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnProcessCompleted(EventArgs e)
        {
            ProcessCompleted?.Invoke(this, e);
        }

        /// <summary>
        /// A Thread procedure that scans for unfinished Processing jobs
        /// </summary>
        public void ThreadProc()
        {
            StaticClass.CurrentProcessingJobsScanThreadHandle = new Thread(() =>
                CheckForCurrentProcessingJobs(IniData, StatusDataList, Logger));

            if (StaticClass.CurrentProcessingJobsScanThreadHandle == null)
            {
                Logger.LogError("CurrentProcessingJobsScanThread thread failed to instantiate");
            }
            StaticClass.CurrentProcessingJobsScanThreadHandle.Start();
        }

        /// <summary>
        /// Method to scan for old jobs in the Processing Buffer
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public void CheckForCurrentProcessingJobs(IniFileData iniData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            StaticClass.Log("\nChecking for unfinished Processing Jobs...");

            DirectoryInfo processingDirectoryInfo = new DirectoryInfo(iniData.ProcessingDir);
            if (processingDirectoryInfo == null)
            {
                Logger.LogError("CurrentProcessingJobsScanThread ProcessingDirectoryInfo failed to instantiate");
            }

            // Get the current list of directories from the Processing Buffer
            List<DirectoryInfo> processingDirectoryInfoList = processingDirectoryInfo.EnumerateDirectories().ToList();
            if (processingDirectoryInfoList == null)
            {
                Logger.LogError("CurrentProcessingJobsScanThread ProcessingDirectoryInfoList failed to instantiate");
            }

            if (processingDirectoryInfoList.Count > 0)
            {
                StaticClass.Log("\nUnfinished Processing jobs found...");
            }
            else
            {
                StaticClass.Log("\nNo unfinished Processing Jobs found...");
            }

            // Start the jobs in the directory list found for the Processing Buffer
            bool foundUnfinishedJobs = false;
            if (StaticClass.NumberOfJobsExecuting < iniData.ExecutionLimit)
            {
                for (int i = 0; i < processingDirectoryInfoList.Count; i++)
                {
                    if (StaticClass.NumberOfJobsExecuting < iniData.ExecutionLimit)
                    {
                        DirectoryInfo dirInfo = processingDirectoryInfoList[i];
                        string directory = dirInfo.ToString();
                        string job = directory.ToString().Replace(IniData.ProcessingDir, "").Remove(0, 1);

                        StaticClass.Log(String.Format("\nStarting Processing Job {0} at {1:HH:mm:ss.fff}", directory, DateTime.Now));

                        StaticClass.ProcessingFileScanComplete[job] = false;
                        ProcessingJobsScanThread processingJobsScanThread = new ProcessingJobsScanThread();
                        processingJobsScanThread.StartProcessingJob(directory, iniData, statusData, logger);
                        processingDirectoryInfoList.Remove(dirInfo);

                        // Throttle the Job startups
                        Thread.Sleep(StaticClass.ScanWaitTime);
                    }
                    else
                    {
                        foundUnfinishedJobs = true;
                    }
                }
            }

            if (foundUnfinishedJobs == true)
            {
                StaticClass.Log("\nMore unfinished Processing jobs then execution slots found...\n");
            }

            // Put the extra jobs found into the Processing Buffer directory into the ProcessingJobsToRun list
            foreach (DirectoryInfo dirInfo in processingDirectoryInfoList)
            {
                string directory = dirInfo.ToString();
                string job = directory.Replace(IniData.ProcessingDir, "").Remove(0, 1);
                StaticClass.ProcessingJobsToRun.Add(job);
                StaticClass.Log(String.Format("Unfinished Processing jobs check added job {0} to Processing Job waiting list", job));
            }

            // Clear the Directory Info List after done with it
            processingDirectoryInfoList.Clear();

            // Run check loop until all unfinished Processing jobs are complete
            do
            {
                // Check if the shutdown flag is set, exit method
                if (StaticClass.ShutdownFlag == true)
                {
                    StaticClass.Log(String.Format("\nShutdown CurrentProcessingJobsScanThread CheckForCurrentProcessingJobs at {0:HH:mm:ss.fff}", DateTime.Now));
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

                // Wait a sec before scanning for unfinished jobs
                Thread.Sleep(1000);

                // Check if there are any more unfinished Processing jobs
                RunAnyUnfinishedProcessingsJobs(IniData, StatusDataList, Logger);
            }
            while (StaticClass.ProcessingJobsToRun.Count > 0);

            // Scan of the Processing Buffer jobs is complete
            StaticClass.UnfinishedProcessingJobsScanComplete = true;
        }

        /// <summary>
        /// Check for unfinished processing jobs after one completes
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public void RunAnyUnfinishedProcessingsJobs(IniFileData iniData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            // Start Processing jobs currently waiting
            for (int i = 0; i < StaticClass.ProcessingJobsToRun.Count; i++)
            {
                if (StaticClass.NumberOfJobsExecuting < iniData.ExecutionLimit)
                {
                    string directory = iniData.ProcessingDir + @"\" + StaticClass.ProcessingJobsToRun[i];
                    string job = directory.ToString().Replace(IniData.ProcessingDir, "").Remove(0, 1);

                    StaticClass.Log(String.Format("\nStarting Processing Job {0} at {1:HH:mm:ss.fff}", directory, DateTime.Now));

                    StaticClass.ProcessingFileScanComplete[job] = false;
                    StaticClass.ProcessingJobScanComplete[job] = false;

                    ProcessingJobsScanThread unfinishedProcessingJobsScan = new ProcessingJobsScanThread();
                    unfinishedProcessingJobsScan.StartProcessingJob(directory, iniData, statusData, logger);
                    
                    StaticClass.ProcessingJobsToRun.Remove(job);

                    // Throttle the Job startups
                    Thread.Sleep(iniData.ScanWaitTime);
                }
            }
        }

        /// <summary>
        /// Start a processing directory job
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="iniData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public void StartProcessingJob(string directory, IniFileData iniData, 
            List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            // Delete the data.xml file in the Processing directory if found
            string dataXmlFile = directory + @"\" + "data.xml";
            if (File.Exists(dataXmlFile))
            {
                lock (DelLock)
                {
                    File.Delete(dataXmlFile);
                }
            }

            // Get data found in job Xml file
            JobXmlData jobXmlData = StaticClass.GetJobXmlFileInfo(directory, iniData, DirectoryScanType.PROCESSING_BUFFER);
            if (jobXmlData == null)
            {
                Logger.LogError("CurrentProcessingJobsScanThread GetJobXmlData failed");
            }

            // Check that the xml job and directory job strings match
            string job = directory.Replace(iniData.ProcessingDir, "").Remove(0, 1);
            if (job != jobXmlData.Job)
            {
                logger.LogError(String.Format("Processing Jobs don't match {0} {1}", job, jobXmlData.Job));
            }

            // Display job xml Data found
            StaticClass.Log("Processing Job                 : " + jobXmlData.Job);
            StaticClass.Log("Processing Job Directory       : " + jobXmlData.JobDirectory);
            StaticClass.Log("Processing Job Serial Number   : " + jobXmlData.JobSerialNumber);
            StaticClass.Log("Processing Job Time Stamp      : " + jobXmlData.TimeStamp);
            StaticClass.Log("Processing Job Xml File        : " + jobXmlData.XmlFileName);

            StaticClass.Log(String.Format("Starting Processing directory Job {0} Executing slot {1} at {2:HH:mm:ss.fff}",
                jobXmlData.Job, StaticClass.NumberOfJobsExecuting + 1, DateTime.Now));

            // Create a thread to run the job, and then start the thread
            JobRunThread thread = new JobRunThread(DirectoryScanType.PROCESSING_BUFFER, jobXmlData, iniData, statusData, logger);
            if (thread == null)
            {
                Logger.LogError("CurrentProcessingJobsScanThread thread failed to instantiate");
            }
            thread.ThreadProc();

            // Check if the shutdown flag is set, exit method
            if (StaticClass.ShutdownFlag == true)
            {
                StaticClass.Log(String.Format("\nShutdown CurrentProcessingJobsScanThread StartProcessingJob at {0:HH:mm:ss.fff}",
                    DateTime.Now));
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
