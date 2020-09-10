using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;
using Microsoft.Extensions.Logging;
using Status.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

        /// <summary>
        /// Current Processing Jobs Scan thread default constructor
        /// </summary>
        public ProcessingJobsScanThread()
        {
            StaticClass.Logger.LogInformation("ProcessingJobsScanThread default constructor called");
        }

        /// <summary>
        /// Current Processing Jobs Scan thread default destructor
        /// </summary>
        ~ProcessingJobsScanThread()
        {
            StaticClass.Logger.LogInformation("ProcessingJobsScanThread default destructor called");
        }

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

            // Add the jobs in the directory list to the Processing Buffer Jobs to run list
            for (int i = 0; i < processingDirectoryInfoList.Count; i++)
            {
                if (StaticClass.NumberOfJobsExecuting < iniData.ExecutionLimit)
                {
                    DirectoryInfo dirInfo = processingDirectoryInfoList[i];
                    string directory = dirInfo.FullName;
                    string job = directory.Replace(IniData.ProcessingDir, "").Remove(0, 1);

                    // Remove Job run from Processing Job list
                    processingDirectoryInfoList.Remove(dirInfo);

                    StaticClass.Log(String.Format("\nStarting Processing Job {0} at {1:HH:mm:ss.fff}", directory, DateTime.Now));

                    // Unset Processing Job file scan flag to start job
                    StaticClass.ProcessingFileScanComplete[job] = false;

                    // Start an Processing Buffer Job
                    StartProcessingJob(directory, iniData, statusData);

                    // Throttle the Job startups
                    Thread.Sleep(StaticClass.ScanWaitTime);
                }
            }

            if (processingDirectoryInfoList.Count > 0)
            {
                StaticClass.Log("\nMore unfinished Processing Jobs then Execution Slots available...");

                // Sort the Processing Buffer directory list by older dates first
                processingDirectoryInfoList.Sort((x, y) => -x.LastAccessTime.CompareTo(y.LastAccessTime));

                // Do Synchronized add of jobs to Processing Job List
                CancellationTokenSource ts = new CancellationTokenSource();
                SynchronizedCache sc = new SynchronizedCache();
                Task addTask = Task.Run(() =>
                {
                    // Add the jobs in the directory list to the Processing Buffer Jobs to run list
                    for (int i = 0; i < processingDirectoryInfoList.Count; i++)
                    {
                        string directory = processingDirectoryInfoList[i].ToString();
                        string job = directory.Replace(IniData.ProcessingDir, "").Remove(0, 1);

                        // Sychronized Add of Job to Processing Buffer Job list
                        sc.Add(i, job);

                        StaticClass.Log(String.Format("\nUnfinished Processing Jobs Scan adding new Job {0} to Processing Job List index {1} at {2:HH:mm:ss.fff}",
                            job, i, DateTime.Now));
                    }
                });

                // Wait a sec for the task to complete
                if (addTask.Wait(1000, ts.Token))
                {
                    StaticClass.Logger.LogError("ProcessingJobWatcherThread failed to Add all Jobs");
                }

                // Clear the Directory Info List after done with it
                processingDirectoryInfoList.Clear();
            }

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
            // Do Synchronized add of jobs to Processing Job List
            CancellationTokenSource ts = new CancellationTokenSource();
            SynchronizedCache sc = new SynchronizedCache();
            Task runTask = Task.Run(() =>
            {
                // Add the Jobs in the directory list to the Processing Buffer Jobs to run list
                for (int i = 0; i < StaticClass.ProcessingJobsToRun.Count; i++)
                {
                    if (StaticClass.NumberOfJobsExecuting < iniData.ExecutionLimit)
                    {
                        string job = StaticClass.ProcessingJobsToRun[i];
                        string directory = iniData.ProcessingDir + @"\" + job;

                        StaticClass.Log(String.Format("\nStarting Processing Job {0} at {1:HH:mm:ss.fff}", directory, DateTime.Now));

                        // Clear Processing Job file scan flag to start job
                        StaticClass.ProcessingFileScanComplete[job] = false;

                        // Start an Processing Buffer Job
                        StartProcessingJob(directory, iniData, statusData);

                        // Remove job run from Processing Job list
                        sc.Delete(i);

                        // Throttle the Job startups
                        Thread.Sleep(StaticClass.ScanWaitTime);
                    }
                }
            });

            // Wait for the task to complete
            bool result = runTask.Wait(1000, ts.Token);
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
            jobRunThread.ThreadProc();
        }
    }
}
