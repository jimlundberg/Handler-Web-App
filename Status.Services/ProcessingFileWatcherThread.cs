using Microsoft.Extensions.Logging;
using StatusModels;
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
        public static IniFileData IniData;
        private StatusMonitorData MonitorData;
        private List<StatusData> StatusData;
        private static string DirectoryName;
        private static Thread thread;
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
        public ProcessingFileWatcherThread(string directory, int numberOfFilesNeeded,
            IniFileData iniData, StatusMonitorData monitorData, List<StatusData> statusData,
            ILogger<StatusRepository> logger)
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
            if (StaticClass.ProcessingFileScanComplete[job] == true)
            {
                if (StaticClass.NewProcessingJobsToRun.Count > 0)
                {
                    if (StaticClass.NumberOfJobsExecuting < iniData.ExecutionLimit)
                    {
                        // Start Processing jobs currently waiting
                        for (int i = 0; i < StaticClass.NewProcessingJobsToRun.Count; i++)
                        {
                            StaticClass.CurrentProcessingJobScanComplete = false;
                            string directory = iniData.InputDir + @"\" + StaticClass.NewProcessingJobsToRun[i];
                            CurrentProcessingJobsScanThread currentProcessingJobsScan = new CurrentProcessingJobsScanThread();
                            currentProcessingJobsScan.StartProcessingJobs(directory, iniData, statusData, logger);
                            StaticClass.NewProcessingJobsToRun.RemoveAt(i);
                            Thread.Sleep(iniData.ScanTime);
                        }
                    }
                }
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
            thread = new Thread(() => WatchFiles(DirectoryName, MonitorData));
            if (thread == null)
            {
                Logger.LogError("ProcessingFileWatcherThread thread failed to instantiate");
            }
            thread.Start();
        }

        /// <summary>
        /// The Add or Change of files callback
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public static void OnCreated(object source, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                // Get job name from directory name
                string jobDirectory = e.FullPath;
                string jobFile = jobDirectory.Replace(IniData.ProcessingDir, "").Remove(0, 1);
                string job = jobFile.Substring(0, jobFile.IndexOf(@"\"));

                StaticClass.NumberOfProcessingFilesFound[job]++;

                // Processing job file added
                StaticClass.Log(IniData.ProcessLogFile,
                    String.Format("\nProcessing File Watcher detected: {0} file {1} of {2} at {3:HH:mm:ss.fff}",
                    e.FullPath, StaticClass.NumberOfProcessingFilesFound[job],
                    StaticClass.NumberOfProcessingFilesNeeded[job], DateTime.Now));

                if (StaticClass.NumberOfProcessingFilesFound[job] == StaticClass.NumberOfProcessingFilesNeeded[job])
                {
                    // Signal the Job Run thread that TCP/IP scan is complete and all the Processing files were found
                    StaticClass.TcpIpScanComplete[job] = true;
                    StaticClass.CurrentProcessingJobScanComplete = true;
                }
            }
        }

        /// <summary>
        /// Check if the Modeler has deposited the OverallResult entry in the job data.xml file
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public bool OverallResultEntryCheck(string directory)
        {
            bool xmlFileFound = false;
            bool OverallResultEntryFound = false;
            string xmlFileName = directory + @"\" + "Data.xml";
            XmlDocument XmlDoc;
            do
            {
                xmlFileFound = File.Exists(xmlFileName);
                Thread.Sleep(250);
            }
            while ((xmlFileFound == false) && (StaticClass.ShutdownFlag == true));

            lock (xmlLock)
            {
                // Read output Xml file data
                XmlDoc = new XmlDocument();
                XmlDoc.Load(xmlFileName);
            }

            // Check if the OverallResult node exists
            XmlNode OverallResult = XmlDoc.DocumentElement.SelectSingleNode("/Data/OverallResult/result");
            if (OverallResult != null)
            {
                OverallResultEntryFound = true;
            }

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

            // Set Flag for ending directory scan loop
            Console.WriteLine(String.Format("ProcessingFileWatcherThread received Tcp/Ip Scan Completed for job {0} at {1:HH:mm:ss.fff}", 
                job, DateTime.Now));

            StaticClass.TcpIpScanComplete[job] = true;
        }

        /// <summary>
        /// Monitor a directory for a complete set of Input files for a job with a timeout
        /// </summary>
        /// <param name="directory"></param>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void WatchFiles(string directory, StatusMonitorData monitorData)
        {
            // Get job name from directory name
            string job = directory.Replace(IniData.ProcessingDir, "").Remove(0, 1);

            // Start the Tcp/Ip Communications thread before checking files
            TcpIpListenThread tcpIp = new TcpIpListenThread(IniData, monitorData, StatusData, Logger);
            if (tcpIp == null)
            {
                Logger.LogError("ProcessingFileWatcherThread tcpIp thread failed to instantiate");
            }
            tcpIp.ProcessCompleted += TcpIp_ScanCompleted;
            tcpIp.StartTcpIpScanProcess(IniData, monitorData, StatusData);

            if (StaticClass.NumberOfProcessingFilesFound[job] == StaticClass.NumberOfProcessingFilesNeeded[job])
            {
                // Signal the Run thread that the Processing files were found
                StaticClass.CurrentProcessingJobScanComplete = true;
            }

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

                // Begin watching for changes to input directory
                watcher.EnableRaisingEvents = true;

                Console.WriteLine("ProcessingFileWatcherThread watching {0} at {1:HH:mm:ss.fff}", 
                    directory, DateTime.Now);

                // Wait for Processing file scan to Complete with a full set of job output files
                do
                {
                    Thread.Sleep(250);
                }
                while ((StaticClass.ProcessingFileScanComplete[job] == false) && (StaticClass.ShutdownFlag == false));

                // Remove job started from the Processing job list
                StaticClass.NewProcessingJobsToRun.Remove(job);

                // Wait for the TCP/IP Scan to Complete when the Modeler deposits the results in data.xml
                bool foundOverallResultEntry = false;
                do
                {
                    foundOverallResultEntry = OverallResultEntryCheck(directory);
                    Thread.Sleep(250);
                }
                while ((foundOverallResultEntry == false) && (StaticClass.ShutdownFlag == false));

                // Exiting thread message
                StaticClass.Log(IniData.ProcessLogFile,
                    String.Format("ProcessingFileWatcherThread scan of job {0} Complete at {1:HH:mm:ss.fff}",
                    directory, DateTime.Now));
            }
        }
    }
}
