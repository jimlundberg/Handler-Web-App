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
    public class OldJobsScanThread
    {
        // State information used in the task.
        private IniFileData IniData;
        private List<StatusData> StatusData;
        public volatile bool endProcess = false;
        private static Thread thread;
        public event EventHandler ProcessCompleted;
        private static Object delLock = new Object();
        ILogger<StatusRepository> Logger;

        // The constructor obtains the state information.
        /// <summary>
        /// Process Thread constructor receiving data buffers
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="statusData"></param>
        /// <param name="globalJobIndex"></param>
        /// <param name="numberOfJobsRunning"></param>
        /// <param name="logger"></param>
        public OldJobsScanThread(IniFileData iniData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            IniData = iniData;
            StatusData = statusData;
            Logger = logger;
        }

        protected virtual void OnProcessCompleted(EventArgs e)
        {
            ProcessCompleted?.Invoke(this, e);
        }


        /// <summary>
        /// A Thread procedure that scans for new jobs
        /// </summary>
        public void ThreadProc()
        {
            thread = new Thread(() => ScanForOldJobs(IniData, StatusData, Logger));
            thread.Start();
        }

        /// <summary>
        /// Method to scan for old jobs in the Processing Buffer
        /// </summary>
        /// <param name="iniFileData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public void ScanForOldJobs(IniFileData iniFileData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            StatusModels.JobXmlData jobXmlData = new StatusModels.JobXmlData();
            List<string> newDirectoryList = new List<string>();
            if (newDirectoryList == null)
            {
                Logger.LogError("ScanForOldJobs newDirectoryList failed to instantiate");
            }
            List<string> runDirectoryList = new List<string>();
            if (runDirectoryList == null)
            {
                Logger.LogError("ScanForOldJobs runDirectoryList failed to instantiate");
            }

            do
            {
                // Get new directory list
                var newDirectoryInfo = new DirectoryInfo(iniFileData.ProcessingDir);
                if (newDirectoryInfo == null)
                {
                    Logger.LogError("ScanForOldJobs newDirectoryInfo failed to instantiate");
                }

                var newDirectoryInfoList = newDirectoryInfo.EnumerateDirectories().ToList();
                foreach (var subdirectory in newDirectoryInfoList)
                {
                    newDirectoryList.Add(subdirectory.ToString());
                }
                newDirectoryList.Sort();

                // Look for a difference between new and run directory lists
                if (newDirectoryList != runDirectoryList)
                {
                    runDirectoryList = newDirectoryList;
                }

                if (runDirectoryList.Count() == 0)
                {
                    logger.LogInformation("No unfinished job(s) Found...");
                    StaticData.oldJobScanComplete = true;
                    return;
                }

                for (int i = 0; i < runDirectoryList.Count(); i++)
                {
                    logger.LogInformation("Found unfinished job(s)...");

                    if (StaticData.NumberOfJobsExecuting < iniFileData.ExecutionLimit)
                    {
                        // Increment counts to track job execution and port id
                        StaticData.IncrementNumberOfJobsExecuting();

                        String job = runDirectoryList[i].Replace(iniFileData.ProcessingDir, "").Remove(0, 1);

                        // Delete the data.xml file if present
                        String dataXmlFile = iniFileData.ProcessingDir + @"\" + job + @"\" + "data.xml";
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
                            Logger.LogError("ScanForOldJobs scanDir failed to instantiate");
                        }
                        jobXmlData = scanDir.GetJobXmlData(job, iniFileData.ProcessingDir + @"\" + job, logger);

                        // Get data found in Xml file into Monitor Data
                        StatusModels.StatusMonitorData xmlData = new StatusModels.StatusMonitorData();
                        if (xmlData == null)
                        {
                            Logger.LogError("ScanForOldJobs xmlData failed to instantiate");
                        }
                        xmlData.Job = job;
                        xmlData.JobDirectory = jobXmlData.JobDirectory;
                        xmlData.JobSerialNumber = jobXmlData.JobSerialNumber;
                        xmlData.TimeStamp = jobXmlData.TimeStamp;
                        xmlData.XmlFileName = jobXmlData.XmlFileName;
                        xmlData.JobIndex = StaticData.RunningJobsIndex++;

                        // Display Monitor Data found
                        Logger.LogInformation("");
                        logger.LogInformation("Found unfinished Job  = " + xmlData.Job);
                        logger.LogInformation("New Job Directory     = " + xmlData.JobDirectory);
                        logger.LogInformation("New Serial Number     = " + xmlData.JobSerialNumber);
                        logger.LogInformation("New Time Stamp        = " + xmlData.TimeStamp);
                        logger.LogInformation("New Job Xml File      = " + xmlData.XmlFileName);
                        Logger.LogInformation("Job {0} Executing slot {1}", xmlData.Job, StaticData.NumberOfJobsExecuting);
                        logger.LogInformation("Starting Job " + xmlData.Job);

                        // Create a thread to execute the task, and then start the thread.
                        JobRunThread jobThread = new JobRunThread(iniFileData.ProcessingDir, iniFileData, xmlData, statusData, logger);
                        if (jobThread == null)
                        {
                            Logger.LogError("ScanForOldJobs jobThread failed to instantiate");
                        }
                        jobThread.ThreadProc();

                        // Check if the shutdown flag is set, exit method
                        if (StaticData.ShutdownFlag == true)
                        {
                            logger.LogInformation("Shutdown ScanForUnfinishedJobs job {0}", xmlData.Job);
                            return;
                        }

                        // Remove job from the run list when run
                        runDirectoryList.Remove(runDirectoryList[i]);

                        Thread.Sleep(1000);
                    }
                    else
                    {
                        Thread.Sleep(iniFileData.ScanTime);
                    }
                }

                StaticData.oldJobScanComplete = true;

                logger.LogInformation("No more unfinished job(s) Found...");
            }
            while (true);
        }
    }
}
