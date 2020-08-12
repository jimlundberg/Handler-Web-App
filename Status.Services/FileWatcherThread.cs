using StatusModels;
using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Security.Permissions;

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
            // StaticData.Log(IniData.ProcessLogFile, ($"File Watcher detected: {e.FullPath} {e.ChangeType}"));
            NumberOfFilesFound++;
            if (NumberOfFilesFound == NumberOfFilesNeeded)
            {
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
        /// Monitor a Directory for a selected number of files with a timeout
        /// </summary>
        /// <param name="directory"></param>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public static void WatchFiles(string directory)
        {
            // Create a new FileSystemWatcher and set its properties.
            using (FileSystemWatcher watcher = new FileSystemWatcher(directory))
            {
                if (watcher == null)
                {
                    Logger.LogError("FileWatcherThread watcher failed to instantiate");
                }

                // Watch for changes in the directory list
                watcher.NotifyFilter = NotifyFilters.LastAccess
                                     | NotifyFilters.LastWrite
                                     | NotifyFilters.FileName
                                     | NotifyFilters.DirectoryName;

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

                // Enter infinite loop waiting for changes
                if (ScanType == DirectoryScanType.INPUT_BUFFER)
                {
                    do
                    {
                        Thread.Sleep(100);
                    }
                    while ((StaticData.ExitInputFileScan == false) && (StaticData.ShutdownFlag == false));

                    // Exiting thread message
                    StaticData.Log(IniData.ProcessLogFile,
                        String.Format("Exiting FileWatcherThread with ExitFileScan {0} and ShutdownFlag {1}",
                        StaticData.ExitInputFileScan, StaticData.ShutdownFlag));
                }
                else if (ScanType == DirectoryScanType.INPUT_BUFFER)
                {
                    do
                    {
                        Thread.Sleep(100);
                    }
                    while ((StaticData.ExitProcessingFileScan == false) && (StaticData.ShutdownFlag == false));

                    // Exiting thread message
                    StaticData.Log(IniData.ProcessLogFile,
                        String.Format("Exiting FileWatcherThread with ExitFileScan {0} and ShutdownFlag {1}",
                        StaticData.ExitProcessingFileScan, StaticData.ShutdownFlag));
                }
            }
        }
    }
}
