using StatusModels;
using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

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
        public static int NumberOfFilesNeeded = 0;
        public static int NumberOfFilesFound = 0;
        private static Thread thread;
        public event EventHandler ProcessCompleted;
        public static ILogger<StatusRepository> Logger;

        public FileWatcherThread() { }

        /// <summary>
        /// File Watcher scan
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public FileWatcherThread(int numberOfFilesNeeded, string directory, IniFileData iniData,
            StatusMonitorData monitorData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            Directory = directory;
            NumberOfFilesFound = 0;
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
            thread = new Thread(() => MonitorDirectory(Directory, StatusModels.DirectoryScanType.INPUT_BUFFER,
                IniData, MonitorData, StatusData, IniData.InputDir, MonitorData.NumFilesConsumed, Logger));
            if (thread == null)
            {
                Logger.LogError("NewJobsScanThread thread failed to instantiate");
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
            // Files Added(or changed???)
            StaticData.Log(IniData.ProcessLogFile, ($"File Watcher detected: {e.FullPath} {e.ChangeType}"));
            NumberOfFilesFound++;
            if (NumberOfFilesFound == NumberOfFilesNeeded)
            {
                // Signal the Run thread that the files were found
                StaticData.ExitFileScan = true;
                return;
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
        /// Monitor the Directory for a selected number of files with a timeout
        /// </summary>
        /// <param name="monitoredDir"></param>
        /// <param name="numberOfFilesNeeded"></param>
        /// <param name="timeout"></param>
        /// <param name="scanTime"></param>
        /// <param name="logger"></param>
        /// <returns>Pass/Fail</returns>
        public static void MonitorDirectory(string directory, StatusModels.DirectoryScanType scanType,
            IniFileData iniData, StatusMonitorData monitorData, List<StatusData> statusData,
            string monitoredDir, int numberOfFilesNeeded, ILogger<StatusRepository> logger)
        {
            // Create a new FileSystemWatcher and set its properties.
            using (FileSystemWatcher watcher = new FileSystemWatcher(directory))
            {
                if (watcher == null)
                {
                    Logger.LogError("FileWatcherThread watcher failed to instantiate");
                }

                // Watch for changes in the directory list
                watcher.NotifyFilter = NotifyFilters.DirectoryName;

                // Watch for any file to get directory changes
                watcher.Filter = "*.*";

                // Add event handlers
                watcher.Changed += OnChanged;
                watcher.Created += OnChanged;
                watcher.Deleted += OnDeleted;

                // Begin watching for changes to input directory
                watcher.EnableRaisingEvents = true;

                // Enter infinite loop waiting for changes
                do
                {
                    Thread.Sleep(100);
                }
                while ((StaticData.ExitFileScan == false) &&
                       (StaticData.ShutdownFlag == false));

                // Exiting thread message
                StaticData.Log(IniData.ProcessLogFile,
                    String.Format("Exiting FileWatcherThread with ExitFileScan {0} and ShutdownFlag {1}",
                    StaticData.ExitDirectoryScan, StaticData.ShutdownFlag));
            }
        }
    }
}
