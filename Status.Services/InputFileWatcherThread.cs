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
    /// Class to Monitor the number of files in the Input job directory
    /// </summary>
    public class InputFileWatcherThread
    {
        public static IniFileData IniData;
        public static StatusMonitorData MonitorData;
        public static List<StatusData> StatusData;
        public static string DirectoryPath;
        private static Thread thread;
        public event EventHandler ProcessCompleted;
        public static ILogger<StatusRepository> Logger;
        public static int NumberOfFilesFound;
        public static int NumberOfFilesNeeded;
        public static bool TcpIpScanComplete = false;

        /// <summary>
        /// Default Input File Watcher Thread Constructore
        /// </summary>
        public InputFileWatcherThread() { }

        /// <summary>
        /// Input directory file watcher thread
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="numberOfFilesNeeded"></param>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public InputFileWatcherThread(string directory, int numberOfFilesNeeded,
            IniFileData iniData, StatusMonitorData monitorData, List<StatusData> statusData, 
            ILogger<StatusRepository> logger)
        {
            DirectoryPath = directory;
            IniData = iniData;
            MonitorData = monitorData;
            StatusData = statusData;
            Logger = logger;
            DirectoryInfo InputJobInfo = new DirectoryInfo(directory);
            NumberOfFilesFound = InputJobInfo.GetFiles().Length;
            NumberOfFilesNeeded = numberOfFilesNeeded;
        }

        /// <summary>
        /// Input File watcher Callback
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnProcessCompleted(EventArgs e)
        {
            ProcessCompleted?.Invoke(this, e);
        }

        /// <summary>
        /// Thread procedure to run Input job files watcher
        /// </summary>
        // The thread procedure performs the task
        public void ThreadProc()
        {
            thread = new Thread(() => WatchFiles(DirectoryPath, NumberOfFilesFound));
            if (thread == null)
            {
                Logger.LogError("InputFileWatcherThread thread failed to instantiate");
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
            // File Added
            StaticData.Log(IniData.ProcessLogFile, $"File watcher detected: {e.FullPath} {e.ChangeType}");

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
                        String.Format("InputFileWatcherThread Found {0} of {1} files in job {2} at {3:HH:mm:ss.fff}",
                        NumberOfFilesFound, NumberOfFilesNeeded, job, DateTime.Now));

                    // Signal the Run thread that the Input files were found
                    StaticData.ExitInputFileScan= true;
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
            // StaticData.Log(IniData.ProcessLogFile, ($"File watcher detected: {e.FullPath} {e.ChangeType}");
        }

        /// <summary>
        /// TCP/IP Scan Complete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void TcpIp_ScanCompleted(object sender, EventArgs e)
        {
            // Set Flag for ending directory scan loop
            Console.WriteLine("InputFileWatcherThread received Tcp/Ip Scan Completed!");
            TcpIpScanComplete = true;
        }

        /// <summary>
        /// Monitor a Directory for a selected number of files with a timeout
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="numberOfFilesFound"></param>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void WatchFiles(string directory, int numberOfFilesFound)
        {
            if (NumberOfFilesFound == NumberOfFilesNeeded)
            {
                StaticData.Log(IniData.ProcessLogFile,
                   String.Format("InputFileWatcherThread Found {0} of {1} files for directory {2} at {3:HH:mm:ss.fff}",
                    NumberOfFilesFound, NumberOfFilesNeeded, directory, DateTime.Now));

                // Signal the Run thread that the Input files were found
                StaticData.ExitInputFileScan = true;
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

                Console.WriteLine("InputFileWatcherThread watching {0} at {1:HH:mm:ss.fff}", directory, DateTime.Now);

                // Check for changes
                do
                {
                    Thread.Sleep(250);
                }
                while (((TcpIpScanComplete == false) || (StaticData.ExitInputFileScan == false)) &&
                       (StaticData.ShutdownFlag == false));

                // Exiting thread message
                StaticData.Log(IniData.ProcessLogFile,
                    String.Format("Exiting InputFileWatcherThread of dir {0} with ExitInputFileScan={1} and ShutdownFlag={2}",
                    directory, StaticData.ExitInputFileScan, StaticData.ShutdownFlag));
            }
        }
    }
}
