using Handler.Services;
using Microsoft.Extensions.Logging;
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
        private static readonly Object RemoveLock = new Object();
        private static int CurrentJobIndex = 1;

        /// <summary>
        /// New jobs Scan thread
        /// </summary>
        public InputJobsScanThread() { }

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
                    string jobDirectory = dirInfo.ToString();
                    string job = jobDirectory.ToString().Replace(StaticClass.IniData.InputDir, "").Remove(0, 1);

                    // Check if the directory has a full set of Job files
                    if (StaticClass.CheckIfJobFilesComplete(jobDirectory) == true)
                    {
                        StaticClass.Log(string.Format("Starting Input Job {0} at {1:HH:mm:ss.fff}", jobDirectory, DateTime.Now));

                        // Remove job run from Input Job directory list
                        lock (RemoveLock)
                        {
                            if (inputDirectoryInfoList.Remove(dirInfo) == false)
                            {
                                StaticClass.Logger.LogError("InputJobsScanThread failed to remove Job {0} from Input Job list", job);
                            }
                        }

                        StaticClass.Log(string.Format("Input Directory Watcher removed Job {0} from Input Directory Jobs list at {1:HH:mm:ss.fff}",
                            job, DateTime.Now));

                        // Reset Input file scan flag
                        StaticClass.InputFileScanComplete[job] = false;

                        // Start an Input Buffer Job
                        StartInputJob(jobDirectory);

                        // Throttle the Job startups
                        Thread.Sleep(StaticClass.ScanWaitTime);
                    }
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
                    StaticClass.AddJobToList(job);
                }

                // Clear the Directory Info List after done with it
                inputDirectoryInfoList.Clear();

                // Display new job list
                StaticClass.DisplayJobList();
            }

            StaticClass.Log("\nStarted Watching for new Input Jobs...");

            // Run new Input Job watch loop here forever
            do
            {
                // Check if the shutdown flag is set, exit method
                if (StaticClass.ShutDownPauseCheck("CheckForNewInputJobs") == true)
                {
                    return;
                }

                // Run any unfinished input jobs
                RunInputJobsFound();

                // Throttle calling new Jobs Found handler
                Thread.Sleep(StaticClass.IniData.ScanWaitTime);
            }
            while (true);
        }

        /// <summary>
        /// Check for new Input Buffer jobs
        /// </summary>
        public void RunInputJobsFound()
        {
            if (StaticClass.NumberOfJobsExecuting < StaticClass.IniData.ExecutionLimit)
            {
                if (StaticClass.GetTotalNumberOfJobs() > 0)
                {
                    string job = StaticClass.GetJobFromList(StaticClass.CurrentJobIndex);
                    if (job != string.Empty)
                    {
                        StaticClass.Log(string.Format("Input Job handler got Job {0} from list index {1} at {2:HH:mm:ss.fff}",
                            job, StaticClass.CurrentJobIndex, DateTime.Now));

                        // Check for complete jobs and run them first
                        string jobDirectory = StaticClass.IniData.InputDir + @"\" + job;
                        if (StaticClass.CheckIfJobFilesComplete(jobDirectory) == true)
                        {
                            StaticClass.Log(string.Format("Starting Input Job {0} index {1} at {2:HH:mm:ss.fff}",
                                jobDirectory, StaticClass.CurrentJobIndex, DateTime.Now));

                            // Start the new completely ready Job
                            StartInputJob(jobDirectory);

                            // Delete the job from the list and display the new list
                            StaticClass.DeleteJobFromList(StaticClass.CurrentJobIndex);
                        }
                        else // Partial Job handling
                        {
                            // Skip Partial Job if there are more in the list
                            if (StaticClass.CurrentJobIndex < StaticClass.LastJobIndex)
                            {
                                StaticClass.Log(string.Format("Input Job scanner skipping Job {0} index {1} as not ready at {2:HH:mm:ss.fff}",
                                    job, StaticClass.CurrentJobIndex, DateTime.Now));

                                StaticClass.CurrentJobIndex++;
                            }
                            else // Run last job in list
                            {
                                StaticClass.Log(string.Format("Starting Partial Input Job {0} index {1} at {2:HH:mm:ss.fff}",
                                    jobDirectory, StaticClass.CurrentJobIndex, DateTime.Now));

                                // Start the new partial Job
                                StartInputJob(jobDirectory);

                                // Delete the job from the list
                                StaticClass.DeleteJobFromList(StaticClass.CurrentJobIndex);

                                // If there is a skipped partial job, start it next by setting index to previous one
                                if ((StaticClass.CurrentJobIndex > StaticClass.PreviousJobIndex))
                                {
                                    StaticClass.CurrentJobIndex = StaticClass.PreviousJobIndex;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (CurrentJobIndex != StaticClass.CurrentJobIndex)
                    {
                        StaticClass.Log(string.Format("Input Jobs Scanner Resetting Current Job Index from {0} at {1:HH:mm:ss.fff}",
                            StaticClass.CurrentJobIndex, DateTime.Now));
                        CurrentJobIndex = StaticClass.CurrentJobIndex = 1;
                    }
                }
            }
        }

        /// <summary>
        /// Start Input Job
        /// </summary>
        /// <param name="directory"></param>
        public void StartInputJob(string directory)
        {
            // Start the Input Job Thread class to start a new Input Buffer job and continue
            StartInputJobThread startInputJobThread = new StartInputJobThread(directory);
            if (startInputJobThread == null)
            {
                StaticClass.Logger.LogError("InputJobsScanThread StartInputJobThread failed to instantiate");
            }
            startInputJobThread.ThreadProc();
        }
    }
}
