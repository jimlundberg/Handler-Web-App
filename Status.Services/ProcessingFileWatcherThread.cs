using Microsoft.Extensions.Logging;
using Status.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;
using System.Threading;
using System.Xml;

namespace Status.Services
{
    /// <summary>
    /// Class to Monitor the number of files in a directory
    /// </summary>
    public class ProcessingFileWatcherThread
    {
        private readonly IniFileData IniData;
        private readonly StatusMonitorData MonitorData;
        private readonly List<StatusData> StatusDataList;
        private readonly string DirectoryName;
        private readonly string Job;
        public event EventHandler ProcessCompleted;
        public static ILogger<StatusRepository> Logger;

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
            StatusDataList = statusData;
            Logger = logger;
            Job = monitorData.Job;
            DirectoryInfo ProcessingJobInfo = new DirectoryInfo(directory);
            StaticClass.NumberOfProcessingFilesFound[Job] = ProcessingJobInfo.GetFiles().Length;
            StaticClass.NumberOfProcessingFilesNeeded[Job] = numberOfFilesNeeded;
            StaticClass.TcpIpScanComplete[Job] = false;
            StaticClass.ProcessingFileScanComplete[Job] = false;
            StaticClass.ProcessingJobScanComplete[Job] = false;
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
            StaticClass.ProcessingFileWatcherThreadHandle = new Thread(() =>
                WatchFiles(DirectoryName, IniData, MonitorData, StatusDataList));

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
        public void OnCreated(object source, FileSystemEventArgs e)
        {
            string jobDirectory = e.FullPath;
            string jobFile = jobDirectory.Replace(IniData.ProcessingDir, "").Remove(0, 1);
            string job = jobFile.Substring(0, jobFile.IndexOf(@"\"));

            StaticClass.NumberOfProcessingFilesFound[job]++;

            // Processing job file added
            StaticClass.Log(String.Format("\nProcessing File Watcher detected: {0} file {1} of {2} at {3:HH:mm:ss.fff}",
                jobDirectory, StaticClass.NumberOfProcessingFilesFound[job], StaticClass.NumberOfProcessingFilesNeeded[job], DateTime.Now));

            if (StaticClass.NumberOfProcessingFilesFound[job] == StaticClass.NumberOfProcessingFilesNeeded[job])
            {
                StaticClass.Log(String.Format("\nProcessing File Watcher detected a complete set {0} of {1} Processing job {2} files at {3:HH:mm:ss.fff}",
                    StaticClass.NumberOfProcessingFilesFound[job], StaticClass.NumberOfProcessingFilesNeeded[job], job, DateTime.Now));

                // Signal the Processing job Scan thread that all the Processing files were found for a job
                StaticClass.ProcessingFileScanComplete[job] = true;
            }
        }

        /// <summary>
        /// Check if the Modeler has deposited the OverallResult entry in the job data.xml file
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="iniData"></param>
        /// <returns></returns>
        public bool OverallResultEntryCheck(string directory)
        {
            bool OverallResultEntryFound = false;
            do
            {
                string xmlFileName = directory + @"\" + "Data.xml";

                // Wait for xml file to be ready
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
                    StaticClass.Log(String.Format("\nShutdown ProcessingFileWatcherThread OverallResultEntryCheck for file {0} at {1:HH:mm:ss.fff}",
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

            StaticClass.Log(String.Format("Processing File Watcher received TCP/IP Scan Completed for job {0} at {1:HH:mm:ss.fff}",
                job, DateTime.Now));

            StaticClass.TcpIpScanComplete[job] = true;
        }

        /// <summary>
        /// Monitor a directory for a complete set of Processing files for a job 
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></param>
        /// <param name="statusData"></param>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void WatchFiles(string directory, IniFileData iniData, StatusMonitorData monitorData, List<StatusData> statusData)
        {
            // Get job name from directory name
            string job = directory.Replace(iniData.ProcessingDir, "").Remove(0, 1);

            // Quick check to see if the directory is already full
            if (StaticClass.NumberOfProcessingFilesFound[job] == StaticClass.NumberOfProcessingFilesNeeded[job])
            {
                // Signal the Run thread that the Processing files were found
                StaticClass.ProcessingFileScanComplete[job] = true;
                return;
            }

            // Start the TCP/IP Communications thread before checking for Processing job files
            TcpIpListenThread tcpIp = new TcpIpListenThread(iniData, monitorData, statusData, Logger);
            if (tcpIp == null)
            {
                Logger.LogError("ProcessingFileWatcherThread tcpIp thread failed to instantiate");
            }
            tcpIp.ProcessCompleted += TcpIp_ScanCompleted;
            tcpIp.StartTcpIpScanProcess(iniData, monitorData, statusData);

            // Create a new FileSystemWatcher and set its properties
            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                if (watcher == null)
                {
                    Logger.LogError("ProcessingFileWatcherThread watcher failed to instantiate");
                }

                // Watch for file changes in the watched directory
                watcher.NotifyFilter = NotifyFilters.FileName;
                watcher.Path = directory;

                // Watch for any file to get directory changes
                watcher.Filter = "*.*";

                // Add event handlers
                watcher.Created += OnCreated;

                // Begin watching for file changes to Processing job directory
                watcher.EnableRaisingEvents = true;

                StaticClass.Log(String.Format("Processing File Watcher watching {0} at {1:HH:mm:ss.fff}",
                    directory, DateTime.Now));

                // Wait for Processing file scan to Complete with a full set of job output files
                do
                {
                    if (StaticClass.ShutdownFlag == true)
                    {
                        StaticClass.Log(String.Format("\nShutdown ProcessingFileWatcherThread watching {0} at {1:HH:mm:ss.fff}",
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

                    Thread.Yield();
                }
                while ((StaticClass.ProcessingFileScanComplete[job] == false) || (StaticClass.TcpIpScanComplete[job] == false));

                // Check if the Processing Job Complete is still set because of possible shutdown
                if (StaticClass.ProcessingJobScanComplete[job] == false)
                {
                    // Wait for the data.xml file to contain a result
                    if (OverallResultEntryCheck(directory))
                    {
                        StaticClass.ProcessingJobScanComplete[job] = true;
                    }
                }

                // Exiting thread message
                StaticClass.Log(String.Format("Processing File Watcher thread completed the scan for job {0} at {1:HH:mm:ss.fff}",
                    directory, DateTime.Now));
            }
        }
    }
}
