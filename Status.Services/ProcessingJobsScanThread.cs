﻿using Microsoft.Extensions.Logging;
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
        private static readonly Object ListLock = new Object();

        /// <summary>
        /// Current Processing Jobs Scan thread default constructor
        /// </summary>
        public ProcessingJobsScanThread() { }

        /// <summary>
        /// Old Jobs Scan Thread constructor receiving data buffers
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="statusData"></param>
        public ProcessingJobsScanThread(IniFileData iniData, List<StatusData> statusData)
        {
            IniData = iniData;
            StatusDataList = statusData;
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
        /// Processing complete callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void jobRun_ProcessCompleted(object sender, EventArgs e)
        {
            string job = e.ToString();

            StaticClass.Log(String.Format("\nCurrent Process Job Scan Received Process Job {0} complete at {1:HH:mm:ss.fff}",
                job, DateTime.Now));
        }

        /// <summary>
        /// A Thread procedure that scans for unfinished Processing jobs
        /// </summary>
        public void ThreadProc()
        {
            StaticClass.ProcessingJobsScanThreadHandle = new Thread(() =>
                CheckForUnfinishedProcessingJobs(IniData, StatusDataList));

            if (StaticClass.ProcessingJobsScanThreadHandle == null)
            {
                StaticClass.Logger.LogError("ProcessingJobsScanThread thread failed to instantiate");
            }
            StaticClass.ProcessingJobsScanThreadHandle.Start();
        }

        /// <summary>
        /// Method to scan for unfinished jobs in the Processing Buffer
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="statusData"></param>
        public void CheckForUnfinishedProcessingJobs(IniFileData iniData, List<StatusData> statusData)
        {
            // Register with the Processing Buffer Jobs completion event and start its thread
            DirectoryInfo processingDirectoryInfo = new DirectoryInfo(iniData.ProcessingDir);
            if (processingDirectoryInfo == null)
            {
                StaticClass.Logger.LogError("ProcessingJobsScanThread processingDirectoryInfo failed to instantiate");
            }

            // Get the current list of directories from the Job Processing Buffer
            List<DirectoryInfo> processingDirectoryInfoList = processingDirectoryInfo.EnumerateDirectories().ToList();
            if (processingDirectoryInfoList == null)
            {
                StaticClass.Logger.LogError("ProcessingJobsScanThread processingDirectoryInfoList failed to instantiate");
            }

            StaticClass.Log("\nChecking for unfinished Processing Jobs...");

            if (processingDirectoryInfoList.Count > 0)
            {
                StaticClass.Log("\nUnfinished Processing Jobs waiting...");
            }
            else
            {
                StaticClass.Log("\nNo unfinished Processing Jobs waiting...");
            }

            // Start the jobs in the directory list found for the Processing Buffer
            foreach (DirectoryInfo dirInfo in processingDirectoryInfoList.Reverse<DirectoryInfo>())
            {
                if (StaticClass.NumberOfJobsExecuting < iniData.ExecutionLimit)
                {
                    string directory = dirInfo.ToString();
                    string job = directory.ToString().Replace(IniData.ProcessingDir, "").Remove(0, 1);

                    StaticClass.Log(String.Format("\nStarting Processing Job {0} at {1:HH:mm:ss.fff}", directory, DateTime.Now));

                    // Remove job run from Processing Job list
                    lock (ListLock)
                    {
                        if (processingDirectoryInfoList.Remove(dirInfo) == false)
                        {
                            StaticClass.Logger.LogError("ProcessingJobsScanThread failed to remove Job {0} from Processing Job list", job);
                        }
                    }

                    // Reset Processing file scan flag
                    StaticClass.ProcessingFileScanComplete[job] = false;

                    // Start a Processing Buffer Job
                    StartProcessingJob(directory, iniData, statusData);

                    // Throttle the Job startups
                    Thread.Sleep(StaticClass.ScanWaitTime);
                }
            }

            if (processingDirectoryInfoList.Count > 0)
            {
                StaticClass.Log("\nMore unfinished Processing Jobs then Execution Slots available...");
            }

            // Sort the Processing Buffer directory list by older dates first
            processingDirectoryInfoList.Sort((x, y) => -x.LastAccessTime.CompareTo(y.LastAccessTime));

            // Add the jobs in the directory list to the Processing Buffer Jobs to run list
            foreach (DirectoryInfo dirInfo in processingDirectoryInfoList)
            {
                string directory = dirInfo.ToString();
                string job = directory.Replace(IniData.ProcessingDir, "").Remove(0, 1);
                StaticClass.ProcessingJobsToRun.Add(job);

                int index = StaticClass.ProcessingJobsToRun.IndexOf(job);
                StaticClass.Log(String.Format("\nUnfinished Processing jobs check added new Job {0} to Processing Job List index {1} at {2:HH:mm:ss.fff}",
                    job, index, DateTime.Now));
            }

            // Clear the Directory Info List after done with it
            processingDirectoryInfoList.Clear();

            // Run check loop until all unfinished Processing jobs are complete
            do
            {
                // Check if the shutdown flag is set, exit method
                if (StaticClass.ShutdownFlag == true)
                {
                    StaticClass.Log(String.Format("\nShutdown ProcessingJobsScanThread CheckForUnfinishedProcessingJobs at {0:HH:mm:ss.fff}", DateTime.Now));
                    return;
                }

                // Check if the pause flag is set, then wait for reset
                if (StaticClass.PauseFlag == true)
                {
                    StaticClass.Log(String.Format("ProcessingJobsScanThread CheckForUnfinishedProcessingJobs is in Pause mode at {0:HH:mm:ss.fff}", DateTime.Now));
                    do
                    {
                        Thread.Yield();
                    }
                    while (StaticClass.PauseFlag == true);
                }

                // Run any unfinished Processing jobs
                RunUnfinishedProcessingJobs(IniData, StatusDataList);
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
        public void RunUnfinishedProcessingJobs(IniFileData iniData, List<StatusData> statusData)
        {
            // Start Processing jobs currently waiting
            foreach (string job in StaticClass.ProcessingJobsToRun.Reverse<string>())
            {
                if (StaticClass.NumberOfJobsExecuting < iniData.ExecutionLimit)
                {
                    string directory = iniData.ProcessingDir + @"\" + job;

                    StaticClass.Log(String.Format("\nStarting Processing Job {0} at {1:HH:mm:ss.fff}", directory, DateTime.Now));

                    // Remove job run from Processing Job list
                    lock (ListLock)
                    {
                        if (StaticClass.ProcessingJobsToRun.Remove(job) == false)
                        {
                            StaticClass.Logger.LogError("ProcessingJobsScanThread failed to remove Job {0} from Processing Job list", job);
                        }
                    }

                    // Reset Processing job and file scan flags
                    StaticClass.ProcessingFileScanComplete[job] = false;
                    StaticClass.ProcessingJobScanComplete[job] = false;

                    // Start a Processing Buffer Job
                    StartProcessingJob(directory, iniData, statusData);

                    // Throttle the Job startups
                    Thread.Sleep(StaticClass.ScanWaitTime);
                }
            }
        }

        /// <summary>
        /// Start a Processing directory job
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="iniData"></param>
        /// <param name="statusData"></param>
        public void StartProcessingJob(string directory, IniFileData iniData, List<StatusData> statusData)
        {
            // Delete the data.xml file in the Processing directory if found
            string dataXmlFile = directory + @"\" + "data.xml";
            if (File.Exists(dataXmlFile))
            {
                File.Delete(dataXmlFile);
            }

            // Get data found in job Xml file
            JobXmlData jobXmlData = StaticClass.GetJobXmlFileInfo(directory, iniData, DirectoryScanType.PROCESSING_BUFFER);
            if (jobXmlData == null)
            {
                StaticClass.Logger.LogError("ProcessingJobsScanThread GetJobXmlData failed");
            }

            // Display job xml Data found
            StaticClass.Log("Processing Job                 : " + jobXmlData.Job);
            StaticClass.Log("Processing Job Directory       : " + jobXmlData.JobDirectory);
            StaticClass.Log("Processing Job Serial Number   : " + jobXmlData.JobSerialNumber);
            StaticClass.Log("Processing Job Time Stamp      : " + jobXmlData.TimeStamp);
            StaticClass.Log("Processing Job Xml File        : " + jobXmlData.XmlFileName);

            StaticClass.Log(String.Format("Starting Processing directory Job {0} Executing Slot {1} at {2:HH:mm:ss.fff}",
                jobXmlData.Job, StaticClass.NumberOfJobsExecuting + 1, DateTime.Now));

            // Create a thread to run the job, and then start the thread
            JobRunThread jobRunThread = new JobRunThread(DirectoryScanType.PROCESSING_BUFFER, jobXmlData, iniData, statusData);
            if (jobRunThread == null)
            {
                StaticClass.Logger.LogError("ProcessingJobsScanThread jobRunThread failed to instantiate");
            }
            jobRunThread.ProcessCompleted += jobRun_ProcessCompleted;
            jobRunThread.ThreadProc();
        }
    }
}
