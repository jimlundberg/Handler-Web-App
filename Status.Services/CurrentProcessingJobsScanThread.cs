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
            StaticClass.ProcessingFileScanComplete = false;
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
            thread = new Thread(() => ScanForCurrentProcessingJobs(IniData, StatusData, Logger));
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
        public void ScanForCurrentProcessingJobs(IniFileData iniFileData, List<StatusData> statusData, ILogger<StatusRepository> logger)
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
            foreach (var subdirectory in newDirectoryInfoList)
            {
                StaticClass.NewProcessingJobsToRun.Add(subdirectory.ToString());
            }

            // Look for run directory list contents
            if (StaticClass.NewProcessingJobsToRun.Count() == 0)
            {
                StaticClass.Log(logFile, "\nNo unfinished Processing jobs found...");
                StaticClass.ProcessingFileScanComplete = true;
                return;
            }

            StaticClass.Log(logFile, "\nFound unfinished Processing job...");

            // Start Processing directory Job
            StartJob(iniFileData, statusData, logger);
        }

        /// <summary>
        /// Start a processing directory job
        /// </summary>
        public void StartJob(IniFileData iniFileData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            string logFile = iniFileData.ProcessLogFile;

            foreach (var dir in StaticClass.NewProcessingJobsToRun)
            {
                string job = dir.Replace(iniFileData.ProcessingDir, "").Remove(0, 1);

                if (StaticClass.NumberOfJobsExecuting < iniFileData.ExecutionLimit)
                {
                    StatusModels.JobXmlData jobXmlData = new StatusModels.JobXmlData();

                    // Delete the data.xml file if present
                    string dataXmlFile = iniFileData.ProcessingDir + @"\" + job + @"\" + "data.xml";
                    if (File.Exists(dataXmlFile))
                    {
                        lock (delLock)
                        {
                            File.Delete(dataXmlFile);
                        }
                    }

                    // Start scan for job files in the Output Buffer
                    ScanDirectory scanDir = new ScanDirectory();
                    if (scanDir == null)
                    {
                        Logger.LogError("CurrentProcessingJobsScanThread scanDir failed to instantiate");
                    }
                    jobXmlData = scanDir.GetJobXmlData(job, iniFileData.ProcessingDir + @"\" + job, logger);

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
                    StaticClass.Log(logFile, "Found unfinished Job  = " + xmlData.Job);
                    StaticClass.Log(logFile, "Old Job Directory     = " + xmlData.JobDirectory);
                    StaticClass.Log(logFile, "Old Serial Number     = " + xmlData.JobSerialNumber);
                    StaticClass.Log(logFile, "Old Time Stamp        = " + xmlData.TimeStamp);
                    StaticClass.Log(logFile, "Old Job Xml File      = " + xmlData.XmlFileName);
                    StaticClass.Log(logFile, String.Format("starting Processing directory Job {0} Executing slot {1}",
                        xmlData.Job, StaticClass.NumberOfJobsExecuting));

                    // Create a thread to execute the task, and then start the thread.
                    JobRunThread thread = new JobRunThread(iniFileData.ProcessingDir, false, iniFileData, xmlData, statusData, logger);
                    if (thread == null)
                    {
                        Logger.LogError("CurrentProcessingJobsScanThread thread failed to instantiate");
                    }
                    thread.ThreadProc();

                    // Check if the shutdown flag is set, exit method
                    if (StaticClass.ShutdownFlag == true)
                    {
                        logger.LogInformation("Shutdown ScanForOldProcessingJobs job {0}", xmlData.Job);
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
                StaticClass.ProcessingFileScanComplete = true;
            }

            StaticClass.Log(IniData.ProcessLogFile, "\nNo more unfinished Processing jobs Found...");
        }
    }
}
