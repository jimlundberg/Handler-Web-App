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
        public event EventHandler ProcessCompleted;
        private static readonly Object RemoveLock = new Object();
        private readonly List<string> ProcessingJobsToRun = new List<string>();

        /// <summary>
        /// Old Jobs Scan Thread constructor receiving data buffers
        /// </summary>
        public ProcessingJobsScanThread()
        {
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
            StaticClass.ProcessingJobsScanThreadHandle = new Thread(() => CheckForUnfinishedProcessingJobs());
            if (StaticClass.ProcessingJobsScanThreadHandle == null)
            {
                StaticClass.Logger.LogError("ProcessingJobsScanThread ProcessingJobsScanThreadHandle failed to instantiate");
            }
            StaticClass.ProcessingJobsScanThreadHandle.Start();
        }

        /// <summary>
        /// Method to scan for unfinished jobs in the Processing Buffer
        /// </summary>
        public void CheckForUnfinishedProcessingJobs()
        {
            // Register with the Processing Buffer Jobs completion event and start its thread
            DirectoryInfo processingDirectoryInfo = new DirectoryInfo(StaticClass.IniData.ProcessingDir);
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
                if (StaticClass.NumberOfJobsExecuting < StaticClass.IniData.ExecutionLimit)
                {
                    string directory = dirInfo.ToString();
                    string job = directory.ToString().Replace(StaticClass.IniData.ProcessingDir, "").Remove(0, 1);

                    StaticClass.Log(string.Format("\nStarting Processing Job {0} at {1:HH:mm:ss.fff}", directory, DateTime.Now));

                    // Remove job run from Processing Job list
                    lock (RemoveLock)
                    {
                        if (processingDirectoryInfoList.Remove(dirInfo) == false)
                        {
                            StaticClass.Logger.LogError("ProcessingJobsScanThread failed to remove Job {0} from Processing Job list", job);
                        }
                    }

                    // Reset Processing file scan flag
                    StaticClass.ProcessingFileScanComplete[job] = false;

                    // Start a Processing Buffer Job
                    StartProcessingJob(directory);

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
                string job = directory.Replace(StaticClass.IniData.ProcessingDir, "").Remove(0, 1);
                ProcessingJobsToRun.Add(job);

                int index = ProcessingJobsToRun.IndexOf(job);
                StaticClass.Log(string.Format("\nUnfinished Processing jobs check added new Job {0} to Processing Job List index {1} at {2:HH:mm:ss.fff}",
                    job, index, DateTime.Now));
            }

            // Clear the Directory Info List after done with it
            processingDirectoryInfoList.Clear();

            // Run check loop until all unfinished Processing jobs are complete
            do
            {
                // Check for shutdown or pause
                if (StaticClass.ShutDownPauseCheck("Processing Jobs Scan Thread") == true)
                {
                    return;
                }

                // Run any unfinished Processing jobs
                RunUnfinishedProcessingJobs();

                Thread.Yield();
            }
            while (ProcessingJobsToRun.Count > 0);

            // Scan of the Processing Buffer jobs is complete
            StaticClass.UnfinishedProcessingJobsScanComplete = true;
        }

        /// <summary>
        /// Check for unfinished processing jobs after one completes
        /// </summary>
        public void RunUnfinishedProcessingJobs()
        {
            // Start Processing jobs currently waiting
            foreach (string job in ProcessingJobsToRun.Reverse<string>())
            {
                if (StaticClass.NumberOfJobsExecuting < StaticClass.IniData.ExecutionLimit)
                {
                    string directory = StaticClass.IniData.ProcessingDir + @"\" + job;

                    StaticClass.Log(string.Format("\nStarting Processing Job {0} at {1:HH:mm:ss.fff}", directory, DateTime.Now));

                    // Remove job run from Processing Job list
                    lock (RemoveLock)
                    {
                        if (ProcessingJobsToRun.Remove(job) == false)
                        {
                            StaticClass.Logger.LogError("ProcessingJobsScanThread failed to remove Job {0} from Processing Job list", job);
                        }
                    }

                    // Reset Processing job and file scan flags
                    StaticClass.ProcessingFileScanComplete[job] = false;
                    StaticClass.ProcessingJobScanComplete[job] = false;
                    StaticClass.JobShutdownFlag[job] = false;

                    // Start a Processing Buffer Job
                    StartProcessingJob(directory);

                    // Throttle the Job startups
                    Thread.Sleep(StaticClass.ScanWaitTime);
                }
            }
        }

        /// <summary>
        /// Start a Processing directory job
        /// </summary>
        /// <param name="directory"></param>
        public void StartProcessingJob(string directory)
        {
            // Delete the data.xml file in the Processing directory if found
            string dataXmlFile = directory + @"\" + "data.xml";
            if (File.Exists(dataXmlFile))
            {
                File.Delete(dataXmlFile);
            }

            // Get data found in job Xml file
            JobXmlData jobXmlData = StaticClass.GetJobXmlFileInfo(directory, DirectoryScanType.PROCESSING_BUFFER);
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

            StaticClass.Log(string.Format("Starting Processing directory Job {0} Executing Slot {1} at {2:HH:mm:ss.fff}",
                jobXmlData.Job, StaticClass.NumberOfJobsExecuting + 1, DateTime.Now));

            // Create a thread to run the job, and then start the thread
            JobRunThread jobRunThread = new JobRunThread(jobXmlData, DirectoryScanType.PROCESSING_BUFFER);
            if (jobRunThread == null)
            {
                StaticClass.Logger.LogError("ProcessingJobsScanThread jobRunThread failed to instantiate");
            }
            jobRunThread.ThreadProc();
        }
    }
}
