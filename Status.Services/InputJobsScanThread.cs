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

        /// <summary>
        /// Current Input Jobs Scan thread default constructor
        /// </summary>
        public InputJobsScanThread() { }

        /// <summary>
        /// New jobs Scan thread
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="statusData"></param>
        public InputJobsScanThread(IniFileData iniData, List<StatusData> statusData)
        {
            IniData = iniData;
            StatusDataList = statusData;
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
        public static void jobRun_ProcessCompleted(object sender, EventArgs e)
        {
            string job = e.ToString();

            StaticClass.Log(String.Format("\nCurrent Input Job Scan Received Process Job {0} complete at {1:HH:mm:ss.fff}",
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

            StaticClass.Log(String.Format("Input Job Scan detected and added Job {0} to Input job list at {1:HH:mm:ss.fff}",
                job, DateTime.Now));
        }

        /// <summary>
        /// A Thread procedure that scans for new jobs
        /// </summary>
        public void ThreadProc()
        {
            // Check for expired Input jobs
            StaticClass.CheckForInputBufferTimeLimits(IniData);

            StaticClass.InputJobsScanThreadHandle = new Thread(() => CheckForUnfinishedInputJobs(IniData, StatusDataList));
            if (StaticClass.InputJobsScanThreadHandle == null)
            {
                StaticClass.Logger.LogError("InputJobsScanThread thread failed to instantiate");
            }
            StaticClass.InputJobsScanThreadHandle.Start();
        }

        /// <summary>
        /// Method to scan for unfinished jobs in the Input Buffer
        /// </summary>
        /// <param name="iniFileData"></param>
        /// <param name="statusData"></param>
        public void CheckForUnfinishedInputJobs(IniFileData iniData, List<StatusData> statusData)
        {
            // Check and delete expired Input Buffer job directories first
            StaticClass.CheckForInputBufferTimeLimits(iniData);

            // Start the Directory Watcher class to scan for new Input Buffer jobs
            DirectoryWatcherThread dirWatch = new DirectoryWatcherThread(iniData);
            if (dirWatch == null)
            {
                StaticClass.Logger.LogError("InputJobsScanThread dirWatch failed to instantiate");
            }
            dirWatch.ProcessCompleted += newJob_DirectoryFound;
            dirWatch.ThreadProc();

            // Thread timing
            Thread.Sleep(100);

            // Register with the Processing Buffer Jobs check completion event and start its thread
            ProcessingJobsScanThread unfinishedProcessingJobs = new ProcessingJobsScanThread(iniData, statusData);
            if (unfinishedProcessingJobs == null)
            {
                StaticClass.Logger.LogError("InputJobsScanThread currentProcessingJobs failed to instantiate");
            }
            unfinishedProcessingJobs.ProcessCompleted += currentInputJob_ProcessCompleted;
            unfinishedProcessingJobs.ThreadProc();

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
                    StaticClass.Log(String.Format("InputJobsScanThread CheckForUnfinishedInputJobs1 is in Pause mode at {0:HH:mm:ss.fff}", DateTime.Now));
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
            for (int i = 0; i < inputDirectoryInfoList.Count; i++)
            {
                do
                {
                    DirectoryInfo dirInfo = inputDirectoryInfoList[i];
                    string directory = dirInfo.ToString();
                    string job = directory.ToString().Replace(IniData.InputDir, "").Remove(0, 1);

                    StaticClass.Log(String.Format("\nStarting Input Job {0} at {1:HH:mm:ss.fff}", directory, DateTime.Now));

                    // Reset Input file scan flag
                    StaticClass.InputFileScanComplete[job] = false;

                    // Start an Input Buffer Job
                    StartInputJob(directory, iniData, statusData);

                    // Remove job just run from the Input Jobs list
                    inputDirectoryInfoList.Remove(dirInfo);

                    // Throttle the Job startups
                    Thread.Sleep(StaticClass.ScanWaitTime);
                }
                while (StaticClass.NumberOfJobsExecuting < iniData.ExecutionLimit);
            }

            if (inputDirectoryInfoList.Count > 0)
            {
                StaticClass.Log("\nMore unfinished Input Jobs then Execution Slots available...\n");
            }

            // Sort the Input Buffer directory list by older dates first
            inputDirectoryInfoList.Sort((x, y) => -x.LastAccessTime.CompareTo(y.LastAccessTime));

            // Add the jobs in the directory list to the Input Buffer Jobs to run list
            foreach (DirectoryInfo dirInfo in inputDirectoryInfoList)
            {
                string directory = dirInfo.ToString();
                string job = directory.Replace(IniData.InputDir, "").Remove(0, 1);
                StaticClass.InputJobsToRun.Add(job);
                StaticClass.Log(String.Format("Unfinished Input jobs check added Job {0} to Input Job waiting list", job));
            }

            // Clear the Directory Info List after done with it
            inputDirectoryInfoList.Clear();

            StaticClass.Log("\nStarting Watching for new Input Jobs...\n");

            // Run new Input Job watch loop here forever
            do
            {
                // Check if the shutdown flag is set, exit method
                if (StaticClass.ShutdownFlag == true)
                {
                    StaticClass.Log(String.Format("\nShutdown InputJobsScanThread CheckForUnfinishedInputJobs at {0:HH:mm:ss.fff}", DateTime.Now));
                    return;
                }

                // Check if the pause flag is set, then wait for reset
                if (StaticClass.PauseFlag == true)
                {
                    StaticClass.Log(String.Format("InputJobsScanThread CheckForUnfinishedInputJobs2 is in Pause mode at {0:HH:mm:ss.fff}", DateTime.Now));
                    do
                    {
                        Thread.Yield();
                    }
                    while (StaticClass.PauseFlag == true);
                }

                // Wait a sec between scans for unfinished Input jobs
                Thread.Sleep(1000);

                // Run any unfinished input jobs
                RunInputJobsFound(IniData, StatusDataList);
            }
            while (true);
        }

        /// <summary>
        /// Check for unfinished Input Buffer jobs
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="statusData"></param>
        public void RunInputJobsFound(IniFileData iniData, List<StatusData> statusData)
        {
            // Check if there are unfinished Input jobs waiting to run
            for (int i = 0; i < StaticClass.InputJobsToRun.Count; i++)
            {
                if (StaticClass.NumberOfJobsExecuting < iniData.ExecutionLimit)
                {
                    string job = StaticClass.InputJobsToRun[i];
                    string directory = iniData.InputDir + @"\" + job;

                    StaticClass.Log(String.Format("\nStarting Input Job {0} at {1:HH:mm:ss.fff}", directory, DateTime.Now));

                    // Reset Processing job and file scan flags
                    StaticClass.InputFileScanComplete[job] = false;

                    // Start an Input Buffer Job
                    StartInputJob(directory, iniData, statusData);

                    // Remove job just run from the Input Jobs to run list
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
        public void StartInputJob(string directory, IniFileData iniData, List<StatusData> statusData)
        {
            // Get data found in Job xml file
            JobXmlData jobXmlData = StaticClass.GetJobXmlFileInfo(directory, iniData, DirectoryScanType.INPUT_BUFFER);
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

            StaticClass.Log(String.Format("Started Input Job {0} executing Slot {1} at {2:HH:mm:ss.fff}",
                jobXmlData.Job, StaticClass.NumberOfJobsExecuting + 1, DateTime.Now));

            // Create a thread to run the job, and then start the thread
            JobRunThread jobRunThread = new JobRunThread(DirectoryScanType.INPUT_BUFFER, jobXmlData, iniData, statusData);
            if (jobRunThread == null)
            {
                StaticClass.Logger.LogError("InputJobsScanThread jobRunThread failed to instantiate");
            }
            jobRunThread.ProcessCompleted += jobRun_ProcessCompleted;
            jobRunThread.ThreadProc();
        }
    }
}
