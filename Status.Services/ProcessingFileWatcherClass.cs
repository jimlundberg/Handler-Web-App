using Microsoft.Extensions.Logging;
using StatusModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;
using System.Threading;

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
        private static string Directory;
        private static Thread thread;
        public event EventHandler ProcessCompleted;
        public static ILogger<StatusRepository> Logger;
        public static int NumberOfFilesFound;
        public static int NumberOfFilesNeeded;
        private static readonly Object changedLock = new Object();
        private static Dictionary<string, bool> TcpIpScanComplete = new Dictionary<string, bool>();
        private static Dictionary<string, bool> ProcessingFileScanComplete = new Dictionary<string, bool>();

        public ProcessingFileWatcherThread() { }

        /// <summary>
        /// File Watcher scan
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public ProcessingFileWatcherThread(string directory, int numberOfFilesNeeded,
            IniFileData iniData, StatusMonitorData monitorData, List<StatusData> statusData,
            ILogger<StatusRepository> logger)
        {
            Directory = directory;
            IniData = iniData;
            MonitorData = monitorData;
            StatusData = statusData;
            Logger = logger;
            DirectoryInfo InputJobInfo = new DirectoryInfo(directory);
            NumberOfFilesFound = InputJobInfo.GetFiles().Length;
            NumberOfFilesNeeded = numberOfFilesNeeded;
            TcpIpScanComplete.Add(monitorData.Job, false);
            ProcessingFileScanComplete.Add(monitorData.Job, false);
        }

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
            thread = new Thread(() => WatchFiles(Directory, MonitorData, NumberOfFilesFound, NumberOfFilesNeeded));
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
        public static void OnChanged(object source, FileSystemEventArgs e)
        {
            // File Added(or changed???)
            StaticData.Log(IniData.ProcessLogFile, $"File Watcher detected: {e.FullPath} {e.ChangeType}");

            lock (changedLock)
            {
                if (e.ChangeType == WatcherChangeTypes.Created)
                {
                    NumberOfFilesFound++;
                    if (NumberOfFilesFound == NumberOfFilesNeeded)
                    {
                        // Get job name from directory name
                        string jobDirectory = e.FullPath;
                        string jobFile = jobDirectory.Replace(IniData.ProcessingDir, "").Remove(0, 1);
                        string job = jobFile.Substring(0, jobFile.IndexOf(@"\"));

                        StaticData.Log(IniData.ProcessLogFile,
                            String.Format("ProcessingFileWatcherThread Found {0} of {1} files for job {2} at {3:HH:mm:ss.fff}",
                            NumberOfFilesFound, NumberOfFilesNeeded, job, DateTime.Now));

                        // Signal the Run thread that the Processing files were found
                        TcpIpScanComplete[job] = true;
                        ProcessingFileScanComplete[job] = true;
                    }
                }
            }
        }

        /// <summary>
        /// The Delete of files callback
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public static void OnDeleted(object source, FileSystemEventArgs e)
        {
            // File is deleted
            // StaticData.Log(IniData.ProcessLogFile, ($"File Watcher detected: {e.FullPath} {e.ChangeType}");
        }

        /// <summary>
        /// TCP/IP Scan Complete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void TcpIp_ScanCompleted(object sender, EventArgs e)
        {
            // Set Flag for ending directory scan loop
            Console.WriteLine("ProcessingFileWatcherThread received Tcp/Ip Scan Completed!");
            TcpIpScanComplete[e.ToString()] = true;
        }

        /// <summary>
        /// Monitor a directory for a complete set of Input files for a job with a timeout
        /// </summary>
        /// <param name="directory"></param>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void WatchFiles(string directory, StatusMonitorData monitorData, int numberOfFilesFound, int numberOfFilesNeeded)
        {
            // Get job name from directory name
            string job = directory.Replace(IniData.ProcessingDir, "").Remove(0, 1);

            // Start the Tcp/Ip Communications thread before checking files
            JobTcpIpThread tcpIp = new JobTcpIpThread(IniData, monitorData, StatusData, Logger);
            if (tcpIp == null)
            {
                Logger.LogError("ProcessingFileWatcherThread tcpIp thread failed to instantiate");
            }
            tcpIp.ProcessCompleted += TcpIp_ScanCompleted;
            tcpIp.StartTcpIpScanProcess(IniData, monitorData, StatusData);

            if (numberOfFilesFound == numberOfFilesNeeded)
            {
                StaticData.Log(IniData.ProcessLogFile,
                   String.Format("ProcessingFileWatcherThread Found {0} of {1} files in job directory {2} at {3:HH:mm:ss.fff}",
                    NumberOfFilesFound, NumberOfFilesNeeded, directory, DateTime.Now));

                // Signal the Run thread that the Processing files were found
                ProcessingFileScanComplete[job] = true;
                TcpIpScanComplete[job] = true;
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
                watcher.Changed += OnChanged;
                watcher.Created += OnChanged;
                watcher.Deleted += OnDeleted;

                // Begin watching for changes to input directory
                watcher.EnableRaisingEvents = true;

                Console.WriteLine("ProcessingFileWatcherThread watching {0} at {1:HH:mm:ss.fff}", directory, DateTime.Now);

                // Wait for the TCP/IP Scan and Processing File Watching to Complete
                do
                {
                    Thread.Sleep(250);
                }
                while (((ProcessingFileScanComplete[job] == false) &&
                        (TcpIpScanComplete[job] == false)) &&
                        (StaticData.ShutdownFlag == false));

                ProcessingFileScanComplete[job] = true;
                TcpIpScanComplete[job] = true;

                // Set Processing file scan exit flag
                StaticData.ExitProcessingFileScan[job] = true;

                // Exiting thread message
                StaticData.Log(IniData.ProcessLogFile,
                    String.Format("Exiting ProcessingFileWatcherThread of job {0} with ExitProcessingFileScan={1} TcpIpScanComplete={2} and ShutdownFlag={3}",
                    monitorData.Job, ProcessingFileScanComplete[monitorData.Job], TcpIpScanComplete[monitorData.Job], StaticData.ShutdownFlag));
            }
        }
    }
}
