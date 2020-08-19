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
            StaticData.CurrentProcessingJobsScanComplete = false;
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

            StaticData.Log(logFile, "\nChecking for unfinished Processing Jobs...");

            StatusModels.JobXmlData jobXmlData = new StatusModels.JobXmlData();
            List<String> runDirectoryList = new List<String>();
            if (runDirectoryList == null)
            {
                Logger.LogError("CurrentProcessingJobsScanThread runDirectoryList failed to instantiate");
            }

            // Get new directory list
            var runDirectoryInfo = new DirectoryInfo(iniFileData.ProcessingDir);
            if (runDirectoryInfo == null)
            {
                Logger.LogError("CurrentProcessingJobsScanThread runDirectoryInfo failed to instantiate");
            }

            var newDirectoryInfoList = runDirectoryInfo.EnumerateDirectories().ToList();
            foreach (var subdirectory in newDirectoryInfoList)
            {
                runDirectoryList.Add(subdirectory.ToString());
            }

            // Look for run directory list contents
            if (runDirectoryList.Count() == 0)
            {
                StaticData.Log(IniData.ProcessLogFile, "\nNo unfinished Processing jobs found...");
                StaticData.CurrentProcessingJobsScanComplete = true;
                return;
            }

            StaticData.Log(IniData.ProcessLogFile, "\nFound unfinished Processing jobs...");

            foreach (var dir in runDirectoryList)
            {
                if (StaticData.NumberOfJobsExecuting < iniFileData.ExecutionLimit)
                {
                    string job = dir.Replace(iniFileData.ProcessingDir, "").Remove(0, 1);

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
                    StaticData.Log(logFile, "");
                    StaticData.Log(logFile, "Found unfinished Job  = " + xmlData.Job);
                    StaticData.Log(logFile, "Old Job Directory     = " + xmlData.JobDirectory);
                    StaticData.Log(logFile, "Old Serial Number     = " + xmlData.JobSerialNumber);
                    StaticData.Log(logFile, "Old Time Stamp        = " + xmlData.TimeStamp);
                    StaticData.Log(logFile, "Old Job Xml File      = " + xmlData.XmlFileName);
                    StaticData.Log(logFile, String.Format("Old Job {0} Executing slot {1}",
                        xmlData.Job, StaticData.NumberOfJobsExecuting));
                    StaticData.Log(logFile, "Starting Job " + xmlData.Job);

                    // Create a thread to execute the task, and then start the thread.
                    JobRunThread thread = new JobRunThread(iniFileData.ProcessingDir, false, iniFileData, xmlData, statusData, logger);
                    if (thread == null)
                    {
                        Logger.LogError("CurrentProcessingJobsScanThread thread failed to instantiate");
                    }
                    thread.ThreadProc();

                    // Check if the shutdown flag is set, exit method
                    if (StaticData.ShutdownFlag == true)
                    {
                        logger.LogInformation("Shutdown ScanForOldProcessingJobs job {0}", xmlData.Job);
                        return;
                    }

                    Thread.Sleep(iniFileData.ScanTime);
                }
                else
                {
                    Thread.Sleep(iniFileData.ScanTime);
                }
            }

            StaticData.CurrentProcessingJobsScanComplete = true;

            StaticData.Log(IniData.ProcessLogFile, "\nNo more unfinished Processing jobs Found...");
        }
    }
}
