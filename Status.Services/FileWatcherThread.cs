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
    public class FileWatcherThread
    {
        public static IniFileData IniData;
        public static StatusMonitorData MonitorData;
        public static List<StatusData> StatusData;
        public static string Directory;
        private static Thread thread;
        public event EventHandler ProcessCompleted;
        public static ILogger<StatusRepository> Logger;
        // The directory scan has an xml file, so start files found count with 1
        public static int NumberOfFilesFound = 1;
        public static int NumberOfFilesNeeded = 0;
        public static DirectoryScanType ScanType;

        public FileWatcherThread() { }

        /// <summary>
        /// File Watcher scan
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public FileWatcherThread(string directory, int numberOfFilesNeeded, DirectoryScanType scanType,
            IniFileData iniData, StatusMonitorData monitorData, List<StatusData> statusData, 
            ILogger<StatusRepository> logger)
        {
            Directory = directory;
            NumberOfFilesFound = 0;
            ScanType = scanType;
            NumberOfFilesNeeded = numberOfFilesNeeded;
            IniData = iniData;
            MonitorData = monitorData;
            StatusData = statusData;
            Logger = logger;
        }

        protected virtual void OnProcessCompleted(EventArgs e)
        {
            ProcessCompleted?.Invoke(this, e);
        }

        // The thread procedure performs the task
        public void ThreadProc()
        {
            thread = new Thread(() => WatchFiles(Directory));
            if (thread == null)
            {
                Logger.LogError("FileWatcherThred thread failed to instantiate");
            }
            thread.Start();
        }

        // Define the event handlers.
        /// <summary>
        /// The Add or Change of files callback
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public static void OnChanged(object source, FileSystemEventArgs e)
        {
            // File Added(or changed???)
            StaticData.Log(IniData.ProcessLogFile, ($"File Watcher detected: {e.FullPath} {e.ChangeType}"));

            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                NumberOfFilesFound++;
                if (NumberOfFilesFound == NumberOfFilesNeeded)
                {
                    StaticData.Log(IniData.ProcessLogFile, 
                        String.Format("File Watcher Found {0} of {1} files in directory {2} at {3:HH:mm:ss.fff}",
                        NumberOfFilesFound, NumberOfFilesNeeded, Directory, DateTime.Now));

                    if (ScanType == DirectoryScanType.INPUT_BUFFER)
                    {
                        // Signal the Run thread that the Input files were found
                        StaticData.ExitInputFileScan = true;
                    }
                    else if (ScanType == DirectoryScanType.PROCESSING_BUFFER)
                    {
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
            Console.WriteLine("Monitor directory received Tcp/Ip Scan Completed!");
            StaticData.TcpIpScanComplete = true;
        }

        /// <summary>
        /// Monitor a Directory for a selected number of files with a timeout
        /// </summary>
        /// <param name="directory"></param>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public static void WatchFiles(string directory)
        {
            if (ScanType == StatusModels.DirectoryScanType.INPUT_BUFFER)
            {
                // Start with the xml file as 1
                NumberOfFilesFound = 1;
            }
            if (ScanType == StatusModels.DirectoryScanType.PROCESSING_BUFFER)
            {
                // Start with the number of start files
                NumberOfFilesFound = MonitorData.NumFilesConsumed;
            }

            // Create a new FileSystemWatcher and set its properties.
            using (FileSystemWatcher watcher = new FileSystemWatcher(directory))
            {
                if (watcher == null)
                {
                    Logger.LogError("FileWatcherThread watcher failed to instantiate");
                }

                // Watch for file changes in the watched directory
                watcher.NotifyFilter = NotifyFilters.FileName;

                // Watch for any file to get directory changes
                watcher.Filter = "*.*";

                // Add event handlers
                watcher.Changed += OnChanged;
                watcher.Created += OnChanged;
                watcher.Deleted += OnDeleted;

                // Begin watching for changes to input directory
                watcher.EnableRaisingEvents = true;

                Console.WriteLine("FileWatcher watching {0} at {1:HH:mm:ss.fff}",
                    directory, DateTime.Now);

                // Start the TCP/IP scan when monitoring the Process Files
                if (ScanType == StatusModels.DirectoryScanType.PROCESSING_BUFFER)
                {
                    // Register with the Tcp/Ip Event and start it's thread
                    JobTcpIpThread tcpIp = new JobTcpIpThread(IniData, MonitorData, StatusData, Logger);
                    if (tcpIp == null)
                    {
                        Logger.LogError("MonitorDirectory tcpIp thread failed to instantiate");
                    }
                    tcpIp.ProcessCompleted += TcpIp_ScanCompleted;
                    tcpIp.StartTcpIpScanProcess(IniData, MonitorData, StatusData);
                }

                // Enter infinite loop waiting for changes
                if (ScanType == DirectoryScanType.INPUT_BUFFER)
                {
                    do
                    {
                        Thread.Sleep(250);
                    }
                    while ((StaticData.ExitInputFileScan == false) && (StaticData.ShutdownFlag == false));

                    // Exiting thread message
                    StaticData.Log(IniData.ProcessLogFile,
                        String.Format("Exiting FileWatcherThread with ExitInputFileScan {0} and ShutdownFlag {1}",
                        StaticData.ExitInputFileScan, StaticData.ShutdownFlag));
                }
                else if (ScanType == DirectoryScanType.PROCESSING_BUFFER)
                {
                    do
                    {
                        Thread.Sleep(250);
                    }
                    while (((StaticData.ExitProcessingFileScan == false) &&
                           (StaticData.TcpIpScanComplete == false)) &&
                           (StaticData.ShutdownFlag == false));

                    // Exiting thread message
                    StaticData.Log(IniData.ProcessLogFile,
                        String.Format("Exiting FileWatcherThread with ExitProcessingFileScan {0} and ShutdownFlag {1}",
                        StaticData.ExitProcessingFileScan, StaticData.ShutdownFlag));
                }
            }
        }
    }
}
