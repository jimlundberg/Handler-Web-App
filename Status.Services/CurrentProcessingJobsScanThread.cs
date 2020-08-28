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
    public class CurrentProcessingJobsScanThread
    {
        private IniFileData IniData;
        private List<StatusData> StatusData;
        public event EventHandler ProcessCompleted;
        private static readonly Object delLock = new Object();
        ILogger<StatusRepository> Logger;

        /// <summary>
        /// Current Processing Jobs Scan thread default constructor
        /// </summary>
        public CurrentProcessingJobsScanThread() { }

        /// <summary>
        /// Old Jobs Scan Thread constructor receiving data buffers
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public CurrentProcessingJobsScanThread(IniFileData iniData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            IniData = iniData;
            StatusData = statusData;
            Logger = logger;
            StaticClass.CurrentProcessingJobsScanComplete = false;
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
            StaticClass.CurrentProcessingJobsScanThreadHandle = new Thread(() => CheckForCurrentProcessingJobs(IniData));
            if (StaticClass.CurrentProcessingJobsScanThreadHandle == null)
            {
                Logger.LogError("CurrentProcessingJobsScanThread thread failed to instantiate");
            }
            StaticClass.CurrentProcessingJobsScanThreadHandle.Start();
        }

        /// <summary>
        /// Method to scan for old jobs in the Processing Buffer
        /// </summary>
        /// <param name="iniFileData"></param>
        public void CheckForCurrentProcessingJobs(IniFileData iniFileData)
        {
            string logFile = iniFileData.ProcessLogFile;

            StaticClass.Log(logFile, "\nChecking for unfinished Processing Jobs...");

            DirectoryInfo ProcessingDirectoryInfo = new DirectoryInfo(iniFileData.ProcessingDir);
            if (ProcessingDirectoryInfo == null)
            {
                Logger.LogError("CurrentProcessingJobsScanThread ProcessingDirectoryInfo failed to instantiate");
            }

            // Get the current list of directories from the Processing Buffer
            List<DirectoryInfo> ProcessingDirectoryInfoList = ProcessingDirectoryInfo.EnumerateDirectories().ToList();
            if (ProcessingDirectoryInfoList == null)
            {
                Logger.LogError("CurrentProcessingJobsScanThread ProcessingDirectoryInfoList failed to instantiate");
            }

            if (ProcessingDirectoryInfoList.Count > 0)
            {
                StaticClass.Log(logFile, "\nStarting unfinished Processing jobs...");
            }
            else
            {
                StaticClass.Log(logFile, "\nNo unfinished Processing Jobs Found...");
            }

            // Start the jobs in the directory list found on initial scan of the Processing Buffer
            foreach (DirectoryInfo dir in ProcessingDirectoryInfoList)
            {
                string job = dir.ToString().Replace(IniData.ProcessingDir, "").Remove(0, 1);
                string directory = dir.ToString();

                if (StaticClass.NumberOfJobsExecuting < iniFileData.ExecutionLimit)
                {
                    CurrentProcessingJobsScanThread newProcessingJobsScanThread = new CurrentProcessingJobsScanThread();
                    newProcessingJobsScanThread.StartProcessingJob(directory, IniData, StatusData, Logger);

                    // Throttle the Job startups
                    var jobWaitTask = Task.Run(async delegate
                    {
                        await Task.Delay(StaticClass.ScanWaitTime);
                        return;
                    });
                    jobWaitTask.Wait();
                }
                else
                {
                    // Add currently unfinished job to Processing Jobs run list
                    StaticClass.NewProcessingJobsToRun.Add(job);
                }
            }

            StaticClass.CurrentProcessingJobsScanComplete = true;
        }

        /// <summary>
        /// Start a processing directory job
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="iniFileData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public void StartProcessingJob(string directory, IniFileData iniFileData, 
            List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            string logFile = iniFileData.ProcessLogFile;
            string job = directory.Replace(iniFileData.ProcessingDir, "").Remove(0, 1);

            if (StaticClass.NumberOfJobsExecuting < iniFileData.ExecutionLimit)
            {
                // Delete the data.xml file in the Processing directory if found
                string deleteXmlFile = directory + @"\" + "data.xml";
                if (File.Exists(deleteXmlFile))
                {
                    lock (delLock)
                    {
                        File.Delete(deleteXmlFile);
                    }
                }

                // Start scan directory for job files in the Processing Buffer
                ScanDirectory scanDir = new ScanDirectory();
                if (scanDir == null)
                {
                    Logger.LogError("CurrentProcessingJobsScanThread scanDir failed to instantiate");
                }

                // Get data found in Xml file into Monitor Data
                JobXmlData jobXmlData = scanDir.GetJobXmlData(job, directory);
                if (jobXmlData == null)
                {
                    Logger.LogError("CurrentProcessingJobsScanThread scanDir GetJobXmlData failed");
                }

                jobXmlData.Job = job;
                jobXmlData.JobDirectory = jobXmlData.JobDirectory;
                jobXmlData.JobSerialNumber = jobXmlData.JobSerialNumber;
                jobXmlData.TimeStamp = jobXmlData.TimeStamp;
                jobXmlData.XmlFileName = jobXmlData.XmlFileName;

                // Display Monitor Data found
                StaticClass.Log(logFile, "");
                StaticClass.Log(logFile, "Found Processing Job        : " + jobXmlData.Job);
                StaticClass.Log(logFile, "Old Job Directory           : " + jobXmlData.JobDirectory);
                StaticClass.Log(logFile, "Old Serial Number           : " + jobXmlData.JobSerialNumber);
                StaticClass.Log(logFile, "Old Time Stamp              : " + jobXmlData.TimeStamp);
                StaticClass.Log(logFile, "Old Job Xml File            : " + jobXmlData.XmlFileName);

                StaticClass.Log(logFile, String.Format("Starting Processing directory Job {0} Executing slot {1} at {2:HH:mm:ss.fff}",
                    jobXmlData.Job, StaticClass.NumberOfJobsExecuting + 1, DateTime.Now));

                // Create a thread to run the job, and then start the thread
                JobRunThread thread = new JobRunThread(DirectoryScanType.PROCESSING_BUFFER, jobXmlData, iniFileData, statusData, logger);
                if (thread == null)
                {
                    Logger.LogError("CurrentProcessingJobsScanThread thread failed to instantiate");
                }
                thread.ThreadProc();

                // Remove Processing job after start thread complete
                StaticClass.NewProcessingJobsToRun.Remove(job);

                // Check if the shutdown flag is set, exit method
                if (StaticClass.ShutdownFlag == true)
                {
                    StaticClass.Log(IniData.ProcessLogFile,
                        String.Format("\nShutdown CurrentProcessingJobsScanThread StartProcessingJob at {0:HH:mm:ss.fff}",
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
}
