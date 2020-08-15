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
        public static StatusMonitorData MonitorData;
        public static List<StatusData> StatusData;
        public static string Directory;
        private static Thread thread;
        public event EventHandler ProcessCompleted;
        public static ILogger<StatusRepository> Logger;
        public static int NumberOfFilesFound;
        public static int NumberOfFilesNeeded;
        private static readonly Object changedLock = new Object();

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
            NumberOfFilesNeeded = numberOfFilesNeeded;
            IniData = iniData;
            MonitorData = monitorData;
            StatusData = statusData;
            Logger = logger;
            NumberOfFilesFound = monitorData.NumFilesConsumed;
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
            thread = new Thread(() => WatchFiles(Directory, NumberOfFilesFound));
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
                        StaticData.Log(IniData.ProcessLogFile,
                            String.Format("ProcessingFileWatcherThread Found {0} of {1} files in directory {2} at {3:HH:mm:ss.fff}",
                            NumberOfFilesFound, NumberOfFilesNeeded, Directory, DateTime.Now));

                        // Signal the Run thread that the Processing files were found
                        StaticData.ExitProcessingFileScan = true;
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
            StaticData.TcpIpScanComplete = true;
        }

        /// <summary>
        /// Monitor a Directory for a selected number of files with a timeout
        /// </summary>
        /// <param name="directory"></param>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public static void WatchFiles(string directory, int numberOfFilesFound)
        {
            NumberOfFilesFound = numberOfFilesFound;

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

                // Register with the Tcp/Ip Event and start it's thread
                JobTcpIpThread tcpIp = new JobTcpIpThread(IniData, MonitorData, StatusData, Logger);
                if (tcpIp == null)
                {
                    Logger.LogError("ProcessingFileWatcherThread tcpIp thread failed to instantiate");
                }
                tcpIp.ProcessCompleted += TcpIp_ScanCompleted;
                tcpIp.StartTcpIpScanProcess(IniData, MonitorData, StatusData);

                // Wait for Scan
                new System.Threading.AutoResetEvent(false).WaitOne();

                // Wait for the TCP/IP Scan and Processing File Watching to Complete
                do
                {
                    Thread.Sleep(250);
                }
                while (((StaticData.ExitProcessingFileScan == false) &&
                        (StaticData.TcpIpScanComplete == false)) &&
                        (StaticData.ShutdownFlag == false));

                // Exiting thread message
                StaticData.Log(IniData.ProcessLogFile,
                    String.Format("Exiting ProcessingFileWatcherThread with ExitProcessingFileScan={0} TcpIpScanComplete={1} and ShutdownFlag={2}",
                    StaticData.ExitProcessingFileScan, StaticData.TcpIpScanComplete, StaticData.ShutdownFlag));
            }
        }
    }
}
