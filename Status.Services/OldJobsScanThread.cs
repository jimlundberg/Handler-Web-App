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
        public OldJobsScanThread(IniFileData iniData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            IniData = iniData;
            StatusData = statusData;
            Logger = logger;
            StaticData.OldJobsScanComplete = false;
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
            thread = new Thread(() => ScanForOldProcessingJobs(IniData, StatusData, Logger));
            thread.Start();
        }

        /// <summary>
        /// Method to scan for old jobs in the Processing Buffer
        /// </summary>
        /// <param name="iniFileData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public void ScanForOldProcessingJobs(IniFileData iniFileData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            StatusModels.JobXmlData jobXmlData = new StatusModels.JobXmlData();
            List<String> runDirectoryList = new List<String>();
            if (runDirectoryList == null)
            {
                Logger.LogError("OldJobsScanThread runDirectoryList failed to instantiate");
            }

            // Get new directory list
            var runDirectoryInfo = new DirectoryInfo(iniFileData.ProcessingDir);
            if (runDirectoryInfo == null)
            {
                Logger.LogError("OldJobsScanThread runDirectoryInfo failed to instantiate");
            }

            var newDirectoryInfoList = runDirectoryInfo.EnumerateDirectories().ToList();
            foreach (var subdirectory in newDirectoryInfoList)
            {
                runDirectoryList.Add(subdirectory.ToString());
            }

            // Look for a difference between new and run directory lists
            if (runDirectoryList.Count() == 0)
            {
                StaticData.Log(IniData.ProcessLogFile, "\nNo unfinished Processing jobs found...");
                StaticData.OldJobsScanComplete = true;
                return;
            }

            StaticData.Log(IniData.ProcessLogFile, "\nFound unfinished Processing jobs...");

            foreach (var dir in runDirectoryList)
            {
                if (StaticData.NumberOfJobsExecuting < iniFileData.ExecutionLimit)
                {
                    // Increment counts to track number of jobs executing
                    StaticData.NumberOfJobsExecuting++;

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
                        Logger.LogError("OldJobsScanThread scanDir failed to instantiate");
                    }
                    jobXmlData = scanDir.GetJobXmlData(job, iniFileData.ProcessingDir + @"\" + job, logger);

                    // Get data found in Xml file into Monitor Data
                    JobXmlData xmlData = new JobXmlData();
                    if (xmlData == null)
                    {
                        Logger.LogError("OldJobsScanThread xmlData failed to instantiate");
                    }

                    xmlData.Job = job;
                    xmlData.JobDirectory = jobXmlData.JobDirectory;
                    xmlData.JobSerialNumber = jobXmlData.JobSerialNumber;
                    xmlData.TimeStamp = jobXmlData.TimeStamp;
                    xmlData.XmlFileName = jobXmlData.XmlFileName;

                    // Display Monitor Data found
                    StaticData.Log(iniFileData.ProcessLogFile, "");
                    StaticData.Log(iniFileData.ProcessLogFile, "Found unfinished Job  = " + xmlData.Job);
                    StaticData.Log(iniFileData.ProcessLogFile, "Old Job Directory     = " + xmlData.JobDirectory);
                    StaticData.Log(iniFileData.ProcessLogFile, "Old Serial Number     = " + xmlData.JobSerialNumber);
                    StaticData.Log(iniFileData.ProcessLogFile, "Old Time Stamp        = " + xmlData.TimeStamp);
                    StaticData.Log(iniFileData.ProcessLogFile, "Old Job Xml File      = " + xmlData.XmlFileName);
                    StaticData.Log(iniFileData.ProcessLogFile, String.Format("Old Job {0} Executing slot {1}",
                        xmlData.Job, StaticData.NumberOfJobsExecuting));
                    StaticData.Log(iniFileData.ProcessLogFile, "Starting Job " + xmlData.Job);

                    // Create a thread to execute the task, and then start the thread.
                    JobRunThread thread = new JobRunThread(iniFileData.ProcessingDir, false, iniFileData, xmlData, statusData, logger);
                    if (thread == null)
                    {
                        Logger.LogError("OldJobsScanThread thread failed to instantiate");
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

            StaticData.Log(IniData.ProcessLogFile, "\nNo more unfinished Processing jobs Found...");
        }
    }
}
