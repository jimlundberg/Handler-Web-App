using Microsoft.Extensions.Logging;
using StatusModels;
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
    public class CurrentProcessingJobsScanThread
    {
        private IniFileData IniData;
        private List<StatusData> StatusData;
        private static Thread thread;
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
            StaticClass.CurrentProcessingJobScanComplete = false;
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
            thread = new Thread(() => CheckForCurrentProcessingJobs(IniData, StatusData, Logger));
            if (thread == null)
            {
                Logger.LogError("CurrentProcessingJobsScanThread thread failed to instantiate");
            }
            thread.Start();
        }

        /// <summary>
        /// Method to scan for old jobs in the Processing Buffer
        /// </summary>
        /// <param name="iniFileData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public void CheckForCurrentProcessingJobs(IniFileData iniFileData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            string logFile = iniFileData.ProcessLogFile;

            StaticClass.Log(logFile, "\nChecking for unfinished Processing Jobs...");

            // Get new Processing directory list of directories
            var runDirectoryInfo = new DirectoryInfo(iniFileData.ProcessingDir);
            if (runDirectoryInfo == null)
            {
                Logger.LogError("CurrentProcessingJobsScanThread runDirectoryInfo failed to instantiate");
            }

            var newDirectoryInfoList = runDirectoryInfo.EnumerateDirectories().ToList();
            foreach (var dir in newDirectoryInfoList)
            {
                StaticClass.NewProcessingJobsToRun.Add(dir.ToString());
            }

            // Look for run directory list contents
            if (StaticClass.NewProcessingJobsToRun.Count() == 0)
            {
                StaticClass.Log(logFile, "\nNo unfinished Processing jobs found...");
                StaticClass.CurrentProcessingJobScanComplete = true;
                return;
            }

            StaticClass.Log(logFile, "\nFound unfinished Processing job(s)...");

            // Start as many current Processing directory Job(s) as possible
            foreach (var dir in StaticClass.NewProcessingJobsToRun)
            {
                StartProcessingJobs(dir.ToString(), iniFileData, statusData, logger);
            }

            StaticClass.Log(IniData.ProcessLogFile, "\nDone with current Processing jobs Found...");
        }

        /// <summary>
        /// Start a processing directory job
        /// </summary>
        /// <param name="iniFileData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public void StartProcessingJobs(string directory, IniFileData iniFileData, List<StatusData> statusData, ILogger<StatusRepository> logger)
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

                // Start scan for job files in the Output Buffer
                ScanDirectory scanDir = new ScanDirectory();
                if (scanDir == null)
                {
                    Logger.LogError("CurrentProcessingJobsScanThread scanDir failed to instantiate");
                }
                JobXmlData jobXmlData = scanDir.GetJobXmlData(job, directory, logger);

                // Get data found in Xml file into Monitor Data
                JobXmlData xmlData = new JobXmlData();
                if (xmlData == null)
                {
                    Logger.LogError("CurrentProcessingJobsScanThread xmlData failed to instantiate");
                }

                xmlData.Job = job;
                xmlData.JobDirectory = jobXmlData.JobDirectory;
                xmlData.JobSerialNumber = jobXmlData.JobSerialNumber;
                xmlData.TimeStamp = jobXmlData.TimeStamp;
                xmlData.XmlFileName = jobXmlData.XmlFileName;

                // Display Monitor Data found
                StaticClass.Log(logFile, "");
                StaticClass.Log(logFile, "Found Processing Job  = " + xmlData.Job);
                StaticClass.Log(logFile, "Old Job Directory     = " + xmlData.JobDirectory);
                StaticClass.Log(logFile, "Old Serial Number     = " + xmlData.JobSerialNumber);
                StaticClass.Log(logFile, "Old Time Stamp        = " + xmlData.TimeStamp);
                StaticClass.Log(logFile, "Old Job Xml File      = " + xmlData.XmlFileName);

                StaticClass.Log(logFile, String.Format("starting Processing directory Job {0} Executing slot {1} at {2:HH:mm:ss.fff}",
                    xmlData.Job, StaticClass.NumberOfJobsExecuting, DateTime.Now));

                // Create a thread to execute the task, and then start the thread.
                JobRunThread thread = new JobRunThread(DirectoryScanType.PROCESSING_BUFFER,
                    iniFileData, xmlData, statusData, logger);
                if (thread == null)
                {
                    Logger.LogError("CurrentProcessingJobsScanThread thread failed to instantiate");
                }
                thread.ThreadProc();

                // Check if the shutdown flag is set, exit method
                if (StaticClass.ShutdownFlag == true)
                {
                    logger.LogInformation("CurrentProcessingJobsScanThread Shutdown of job {0}", xmlData.Job);
                    return;
                }

                // Remove this job from the Processing Job list
                StaticClass.NewProcessingJobsToRun.Remove(job);
                Thread.Sleep(iniFileData.ScanTime);
            }
            else
            {
                // Add new job to the Processing Job list
                StaticClass.NewProcessingJobsToRun.Add(job);
                Thread.Sleep(iniFileData.ScanTime);
            }

            // Flag that the Processing Scan is complete
            StaticClass.CurrentProcessingJobScanComplete = true;
        }
    }
}
