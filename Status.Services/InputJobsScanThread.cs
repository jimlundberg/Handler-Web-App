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
    public class InputJobsScanThread
    {
        private static readonly Object RemoveLock = new Object();

        /// <summary>
        /// New jobs Scan thread
        /// </summary>
        public InputJobsScanThread() { }

        /// <summary>
        /// Input Jobs Scan thread default destructor
        /// </summary>
        ~InputJobsScanThread()
        {
            StaticClass.Logger.LogInformation("InputJobsScanThread default destructor called");
        }

        /// <summary>
        /// A Thread procedure that scans for new jobs
        /// </summary>
        public void ThreadProc()
        {
            // Check for expired Input jobs
            StaticClass.CheckForInputBufferTimeLimits();

            StaticClass.InputJobsScanThreadHandle = new Thread(() => CheckForUnfinishedInputJobs());
            if (StaticClass.InputJobsScanThreadHandle == null)
            {
                StaticClass.Logger.LogError("InputJobsScanThread InputJobsScanThreadHandle thread failed to instantiate");
            }
            StaticClass.InputJobsScanThreadHandle.Start();
        }

        /// <summary>
        /// Method to scan for unfinished jobs in the Input Buffer
        /// </summary>
        public void CheckForUnfinishedInputJobs()
        {
            // Start the Directory Watcher class to scan for new Input Buffer jobs
            DirectoryWatcherThread dirWatch = new DirectoryWatcherThread();
            if (dirWatch == null)
            {
                StaticClass.Logger.LogError("InputJobsScanThread dirWatch failed to instantiate");
            }
            dirWatch.ThreadProc();

            // Register with the Processing Buffer Jobs check completion event and start its thread
            ProcessingJobsScanThread unfinishedProcessingJobs = new ProcessingJobsScanThread();
            if (unfinishedProcessingJobs == null)
            {
                StaticClass.Logger.LogError("InputJobsScanThread currentProcessingJobs failed to instantiate");
            }
            unfinishedProcessingJobs.ThreadProc();

            // Wait while scanning for unfinished Processing jobs
            do
            {
                if (StaticClass.ShutDownPauseCheck("CheckForUnfinishedInputJobs") == true)
                {
                    StaticClass.Log(string.Format("\nShutdown InputJobsScanThread CheckForUnfinishedInputJobs at {0:HH:mm:ss.fff}", DateTime.Now));
                    return;
                }

                Thread.Yield();
            }
            while (StaticClass.UnfinishedProcessingJobsScanComplete == false);

            StaticClass.Log("\nChecking for unfinished Input Jobs...");

            DirectoryInfo InputDirectoryInfo = new DirectoryInfo(StaticClass.IniData.InputDir);
            if (InputDirectoryInfo == null)
            {
                StaticClass.Logger.LogError("InputJobsScanThread InputDirectoryInfo failed to instantiate");
            }

            // Get the current list of directories from the Input Buffer
            List<DirectoryInfo> inputDirectoryInfoList = InputDirectoryInfo.EnumerateDirectories().ToList();
            if (inputDirectoryInfoList == null)
            {
                StaticClass.Logger.LogError("InputJobsScanThread inputDirectoryInfoList failed to instantiate");
            }

            if (inputDirectoryInfoList.Count > 0)
            {
                StaticClass.Log("\nUnfinished Input Jobs waiting...");
            }
            else
            {
                StaticClass.Log("\nNo unfinished Input Jobs found...");
            }

            // Start the jobs in the directory list found for the Input Buffer
            foreach (DirectoryInfo dirInfo in inputDirectoryInfoList.Reverse<DirectoryInfo>())
            {
                if (StaticClass.NumberOfJobsExecuting < StaticClass.IniData.ExecutionLimit)
                {
                    string directory = dirInfo.ToString();
                    string job = directory.ToString().Replace(StaticClass.IniData.InputDir, "").Remove(0, 1);

                    StaticClass.Log(string.Format("\nStarting Input Job {0} at {1:HH:mm:ss.fff}", directory, DateTime.Now));

                    // Remove job run from Input Job directory list
                    lock (RemoveLock)
                    {
                        if (inputDirectoryInfoList.Remove(dirInfo) == false)
                        {
                            StaticClass.Logger.LogError("InputJobsScanThread failed to remove Job {0} from Input Job list", job);
                        }
                    }

                    // Reset Input file scan flag
                    StaticClass.InputFileScanComplete[job] = false;

                    // Start an Input Buffer Job
                    StartInputJob(directory);

                    // Throttle the Job startups
                    Thread.Sleep(StaticClass.ScanWaitTime);
                }
            }

            if (inputDirectoryInfoList.Count > 0)
            {
                StaticClass.Log("\nMore unfinished Input Jobs then Execution Slots available...");

                // Sort the Input Buffer directory list by older dates first
                inputDirectoryInfoList.Sort((x, y) => -x.LastAccessTime.CompareTo(y.LastAccessTime));

                // Add the jobs in the directory list to the Input Buffer Jobs to run list
                foreach (DirectoryInfo dirInfo in inputDirectoryInfoList)
                {
                    string directory = dirInfo.ToString();
                    string job = directory.Replace(StaticClass.IniData.InputDir, "").Remove(0, 1);
                    int index = 0;

                    Task AddTask = Task.Run(() =>
                    {
                        index = StaticClass.InputJobsToRun.Count + 1;
                        StaticClass.InputJobsToRun.Add(index, job);
                    });

                    TimeSpan timeSpan = TimeSpan.FromMilliseconds(StaticClass.ADD_TASK_DELAY);
                    if (!AddTask.Wait(timeSpan))
                    {
                        StaticClass.Logger.LogError("InputJobScanThread Add Job {0} timed out at {1:HH:mm:ss.fff}", job, DateTime.Now);
                    }

                    StaticClass.Log(string.Format("\nUnfinished Input Jobs Scan added new Job {0} to Input Job List index {1} at {2:HH:mm:ss.fff}",
                        job, index, DateTime.Now));
                }

                // Clear the Directory Info List after done with it
                inputDirectoryInfoList.Clear();
            }

            StaticClass.Log("\nStarted Watching for new Input Jobs...");

            // Run new Input Job watch loop here forever
            do
            {
                // Check if the shutdown flag is set, exit method
                if (StaticClass.ShutDownPauseCheck("CheckForUnfinishedInputJobs") == true)
                {
                    StaticClass.Log(string.Format("\nShutdown InputJobsScanThread CheckForUnfinishedInputJobs at {0:HH:mm:ss.fff}", DateTime.Now));
                    return;
                }

                // Run any unfinished input jobs
                RunInputJobsFound();

                Thread.Yield();
            }
            while (true);
        }

        /// <summary>
        /// Check for unfinished Input Buffer jobs
        /// </summary>
        public void RunInputJobsFound()
        {
            // Check if there are unfinished Input jobs waiting to run
            if (StaticClass.NumberOfJobsExecuting < StaticClass.IniData.ExecutionLimit)
            {
                int index = 0;
                string job = String.Empty;
                Task AddTask = Task.Run(() =>
                {
                    if (StaticClass.InputJobsToRun.Count > 0)
                    {
                        index = StaticClass.InputJobsToRun.Count;
                        job = StaticClass.InputJobsToRun.Read(index);
                        StaticClass.InputJobsToRun.Delete(index);
                    }
                });

                TimeSpan timeSpan = TimeSpan.FromMilliseconds(StaticClass.ADD_TASK_DELAY);
                if (!AddTask.Wait(timeSpan))
                {
                    StaticClass.Logger.LogError("InputJobScanThread Run Job {0} timed out at {1:HH:mm:ss.fff}", job, DateTime.Now);
                }

                if (job != String.Empty)
                {
                    string directory = StaticClass.IniData.InputDir + @"\" + job;

                    StaticClass.Log(string.Format("\nStarting Input Job {0} at {1:HH:mm:ss.fff}", directory, DateTime.Now));

                    // Reset Input job file scan flag
                    StaticClass.InputFileScanComplete[job] = false;

                    // Start an Input Buffer Job
                    StartInputJob(directory);
                }

                // Throttle the Job startups
                Thread.Sleep(StaticClass.ScanWaitTime);
            }
        }

        /// <summary>
        /// Method to start new jobs from the Input Buffer 
        /// </summary>
        /// <param name="directory"></param>
        public void StartInputJob(string directory)
        {
            // Get data found in Job xml file
            JobXmlData jobXmlData = StaticClass.GetJobXmlFileInfo(directory, DirectoryScanType.INPUT_BUFFER);
            if (jobXmlData == null)
            {
                StaticClass.Logger.LogError("InputJobsScanThread GetJobXmlData failed");
            }

            // Display job xml data found
            StaticClass.Log("Input Job                      : " + jobXmlData.Job);
            StaticClass.Log("Input Job Directory            : " + jobXmlData.JobDirectory);
            StaticClass.Log("Input Job Serial Number        : " + jobXmlData.JobSerialNumber);
            StaticClass.Log("Input Job Time Stamp           : " + jobXmlData.TimeStamp);
            StaticClass.Log("Input Job Xml File             : " + jobXmlData.XmlFileName);

            StaticClass.Log(string.Format("Started Input Job {0} executing Slot {1} at {2:HH:mm:ss.fff}",
                jobXmlData.Job, StaticClass.NumberOfJobsExecuting + 1, DateTime.Now));

            // Create a thread to run the job, and then start the thread
            JobRunThread jobRunThread = new JobRunThread(jobXmlData, DirectoryScanType.INPUT_BUFFER);
            if (jobRunThread == null)
            {
                StaticClass.Logger.LogError("InputJobsScanThread jobRunThread failed to instantiate");
            }
            jobRunThread.ThreadProc();
        }
    }
}
