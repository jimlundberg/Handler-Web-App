using Microsoft.Extensions.Logging;
using Status.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Status.Services
{
    /// <summary>
    /// Class to Monitor the number of files in a directory
    /// </summary>
    public class ProcessingFileWatcherThread
    {
        public static IniFileData IniData;
        private StatusMonitorData MonitorData;
        private List<StatusData> StatusData;
        private static string DirectoryName;
        private static string Job;
        public event EventHandler ProcessCompleted;
        public static ILogger<StatusRepository> Logger;
        private static readonly Object xmlLock = new Object();

        /// <summary>
        /// Processing directory file watcher thread
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="numberOfFilesNeeded"></param>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public ProcessingFileWatcherThread(string directory, int numberOfFilesNeeded, IniFileData iniData,
            StatusMonitorData monitorData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            DirectoryName = directory;
            IniData = iniData;
            MonitorData = monitorData;
            StatusData = statusData;
            Logger = logger;
            Job = monitorData.Job;
            DirectoryInfo ProcessingJobInfo = new DirectoryInfo(directory);
            StaticClass.NumberOfProcessingFilesFound[Job] = ProcessingJobInfo.GetFiles().Length;
            StaticClass.NumberOfProcessingFilesNeeded[Job] = numberOfFilesNeeded;
            StaticClass.TcpIpScanComplete[Job] = false;
            StaticClass.ProcessingFileScanComplete[Job] = false;
            StaticClass.ProcessingJobScanComplete[Job] = false;

            // Check for current unfinished job(s) in the Processing Buffer
            ProcessingsJobsReadyCheck(Job, iniData, statusData, logger);
        }

        /// <summary>
        /// Check if unfinished Processing Jobs jobs are currently waiting to run
        /// </summary>
        /// <param name="job"></param>
        /// <param name="iniData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public void ProcessingsJobsReadyCheck(string job, IniFileData iniData,
            List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            if (StaticClass.NumberOfJobsExecuting < iniData.ExecutionLimit)
            {
                // Start Processing jobs currently waiting
                for (int i = 0; i < StaticClass.NewProcessingJobsToRun.Count; i++)
                {
                    string directory = iniData.InputDir + @"\" + StaticClass.NewProcessingJobsToRun[i];
                    CurrentProcessingJobsScanThread currentProcessingJobsScan = new CurrentProcessingJobsScanThread();
                    currentProcessingJobsScan.StartProcessingJob(directory, iniData, statusData, logger);

                    // Throttle the Job startups
                    var jobWaitTask = Task.Run(async delegate
                    {
                        await Task.Delay(StaticClass.ScanWaitTime);
                        return;
                    });
                    jobWaitTask.Wait();
                }
            }
            else
            {
                // Add currently unfinished job to Processing Jobs run list
                StaticClass.NewProcessingJobsToRun.Add(job);

                StaticClass.Log(IniData.ProcessLogFile,
                    String.Format("Processing file watcher added new job {0} to Processing Job run list at {1:HH:mm:ss.fff}",
                    job, DateTime.Now));
            }
        }

        /// <summary>
        /// Process complete callback
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnProcessCompleted(EventArgs e)
        {
            ProcessCompleted?.Invoke(this, e);
       }

        // The thread procedure performs the task
        /// <summary>
        /// Thread procedure to start the Processing job file watching
        /// </summary>
        public void ThreadProc()
        {
            StaticClass.ProcessingFileWatcherThreadHandle = new Thread(() => WatchFiles(DirectoryName, MonitorData));
            if (StaticClass.ProcessingFileWatcherThreadHandle == null)
            {
                Logger.LogError("ProcessingFileWatcherThread thread failed to instantiate");
            }
            StaticClass.ProcessingFileWatcherThreadHandle.Start();
        }

        /// <summary>
        /// The Add or Change of files callback
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public static void OnCreated(object source, FileSystemEventArgs e)
        {
            // Get job name from directory name
            string jobDirectory = e.FullPath;
            string jobFile = jobDirectory.Replace(IniData.ProcessingDir, "").Remove(0, 1);
            string job = jobFile.Substring(0, jobFile.IndexOf(@"\"));

            StaticClass.NumberOfProcessingFilesFound[job]++;

            // Processing job file added
            StaticClass.Log(IniData.ProcessLogFile,
                String.Format("Processing File Watcher detected: {0} file {1} of {2} at {3:HH:mm:ss.fff}",
                jobDirectory, StaticClass.NumberOfProcessingFilesFound[job],
                StaticClass.NumberOfProcessingFilesNeeded[job], DateTime.Now));

            if (StaticClass.NumberOfProcessingFilesFound[job] == StaticClass.NumberOfProcessingFilesNeeded[job])
            {
                StaticClass.Log(IniData.ProcessLogFile,
                    String.Format("Processing File Watcher detected the complete set {0} of {1} Processing job {2} files at {3:HH:mm:ss.fff}",
                    StaticClass.NumberOfProcessingFilesFound[job], StaticClass.NumberOfProcessingFilesNeeded[job], job, DateTime.Now));

                // Signal the Processing job Scan thread that all the Processing files were found for a job
                StaticClass.ProcessingFileScanComplete[job] = true;
            }
        }

        /// <summary>
        /// Check if the Modeler has deposited the OverallResult entry in the job data.xml file
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public bool OverallResultEntryCheck(string directory)
        {
            bool OverallResultEntryFound = false;

            do
            {
                // Wait for the data.xml file to be ready
                string xmlFileName = directory + @"\" + "Data.xml";
                var task = StaticClass.IsFileReady(xmlFileName, Logger);
                task.Wait();

                // Read output Xml file data
                XmlDocument XmlDoc = new XmlDocument();
                XmlDoc.Load(xmlFileName);

                // Check if the OverallResult node exists
                XmlNode OverallResult = XmlDoc.DocumentElement.SelectSingleNode("/Data/OverallResult/result");
                if (OverallResult != null)
                {
                    OverallResultEntryFound = true;
                }

                if (StaticClass.ShutdownFlag == true)
                {
                    StaticClass.Log(IniData.ProcessLogFile,
                        String.Format("\nShutdown ProcessingFileWatcher Thread OverallResultEntryCheck for file {0} at {1:HH:mm:ss.fff}",
                        directory, DateTime.Now));
                    return false;
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

                Thread.Yield();
            }
            while (OverallResultEntryFound == false);

            return OverallResultEntryFound;
        }

        /// <summary>
        /// TCP/IP Scan Complete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void TcpIp_ScanCompleted(object sender, EventArgs e)
        {
            string job = e.ToString();

            StaticClass.Log(IniData.ProcessLogFile,
                String.Format("Processing File Watcher received Tcp/Ip Scan Completed for job {0} at {1:HH:mm:ss.fff}",
                job, DateTime.Now));

            StaticClass.TcpIpScanComplete[job] = true;
        }

        /// <summary>
        /// Monitor a directory for a complete set of Input files for a job with a timeout
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="monitorData"></param>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void WatchFiles(string directory, StatusMonitorData monitorData)
        {
            // Get job name from directory name
            string job = directory.Replace(IniData.ProcessingDir, "").Remove(0, 1);

            // Quick check to see if the directory is already full
            if (StaticClass.NumberOfProcessingFilesFound[job] == StaticClass.NumberOfProcessingFilesNeeded[job])
            {
                // Signal the Run thread that the Processing files were found
                StaticClass.ProcessingFileScanComplete[job] = true;
                return;
            }

            // Start the Tcp/Ip Communications thread before checking for Processing job files
            TcpIpListenThread tcpIp = new TcpIpListenThread(IniData, monitorData, StatusData, Logger);
            if (tcpIp == null)
            {
                Logger.LogError("ProcessingFileWatcherThread tcpIp thread failed to instantiate");
            }
            tcpIp.ProcessCompleted += TcpIp_ScanCompleted;
            tcpIp.StartTcpIpScanProcess(IniData, monitorData, StatusData);

            // Create a new FileSystemWatcher and set its properties.
            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                // Watch for file changes in the watched directory
                watcher.NotifyFilter = NotifyFilters.FileName;
                watcher.Path = directory;

                // Watch for any file to get directory changes
                watcher.Filter = "*.*";

                // Add event handlers
                watcher.Created += OnCreated;

                // Begin watching for changes to Processing directory
                watcher.EnableRaisingEvents = true;

                StaticClass.Log(IniData.ProcessLogFile,
                    String.Format("Processing File Watcher watching {0} at {1:HH:mm:ss.fff}",
                    directory, DateTime.Now));

                // Wait for Processing file scan to Complete with a full set of job output files
                do
                {
                    Thread.Yield();

                    if (StaticClass.ShutdownFlag == true)
                    {
                        StaticClass.Log(IniData.ProcessLogFile,
                            String.Format("\nShutdown ProcessingFileWatcherThread watching {0} at {1:HH:mm:ss.fff}",
                            directory, DateTime.Now));
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
                while ((StaticClass.ProcessingFileScanComplete[job] == false) || (StaticClass.TcpIpScanComplete[job] == false));

                // Wait for the data.xml file to contain a result
                if (OverallResultEntryCheck(directory))
                {
                    // Exiting thread message
                    StaticClass.Log(IniData.ProcessLogFile,
                    String.Format("Processing File Watcher completion of scan of {0} with ProcessingFileScanComplete={1} and TcpIpScanComplete={2} at at {3:HH:mm:ss.fff}",
                        directory, StaticClass.ProcessingFileScanComplete[job], StaticClass.TcpIpScanComplete[job], DateTime.Now));

                    StaticClass.ProcessingJobScanComplete[job] = true;
                }
            }
        }
    }
}
