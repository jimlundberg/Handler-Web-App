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
    /// Class to do an infinite scan for new directories created in the Input directory
    /// </summary>
    public class DirectoryWatcherThread
    {
        public static string DirectoryName;
        public static IniFileData IniData;
        public static List<StatusData> StatusData;
        private static Thread thread;
        public event EventHandler ProcessCompleted;
        public static ILogger<StatusRepository> Logger;

        /// <summary>
        /// New Jobs Directory Scan Thread constructor receiving data buffers
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public DirectoryWatcherThread(IniFileData iniData, List<StatusData> statusData, 
            ILogger<StatusRepository> logger)
        {
            IniData = iniData;
            StatusData = statusData;
            DirectoryName = iniData.InputDir;
            Logger = logger;
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
        /// A Thread procedure that scans for new directories added to Directory
        /// </summary>
        public void ThreadProc()
        {
            thread = new Thread(() => WatchDirectory(DirectoryName));
            if (thread == null)
            {
                Logger.LogError("DirectoryWatcherThread thread failed to instantiate");
            }
            thread.Start();
        }

        // Define the event handlers.
        /// <summary>
        /// The Add or Change of directory callback
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public static void OnCreated(object source, FileSystemEventArgs e)
        {
            // Store job to run now or later
            string newJobDirectory = e.FullPath;
            string newJobName = newJobDirectory.Replace(IniData.InputDir, "").Remove(0, 1);

            // Directory Add detected
            StaticClass.Log(IniData.ProcessLogFile,
                (String.Format("\nInput Directory Watcher detected new directory {0} at {1:HH:mm:ss.fff}",
                newJobDirectory, DateTime.Now)));

            // Add new job found to the Input job list
            StaticClass.NewInputJobsToRun.Add(newJobName);

            // Check the list to see if you can run a job
            for (int i = 0; i < StaticClass.NewInputJobsToRun.Count; i++)
            {
                // Check how many jobs are executing
                if (StaticClass.NumberOfJobsExecuting < IniData.ExecutionLimit)
                {
                    // Run the job and remove it from the list
                    string job = StaticClass.NewInputJobsToRun[i];
                    string directory = IniData.InputDir + @"\" + job;
                    CurrentInputJobsScanThread newJobsScanThread = new CurrentInputJobsScanThread();
                    newJobsScanThread.StartInputJob(directory, IniData, StatusData, Logger);
                }
            }
        }

        /// <summary>
        /// Scan directory for added and deleted directories
        /// </summary>
        /// <param name="directory"></param>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void WatchDirectory(string Directory)
        {
            // Get job name from directory name
            string job = Directory.Replace(IniData.ProcessingDir, "").Remove(0, 1);

            // Create a new FileSystemWatcher and set its properties
            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                if (watcher == null)
                {
                    Logger.LogError("DirectoryWatcherThread watcher failed to instantiate");
                }

                // Watch for changes in the directory list
                watcher.NotifyFilter = NotifyFilters.DirectoryName;
                watcher.Path = Directory;

                // Watch for any directories names added
                watcher.Filter = "*.*";
                watcher.IncludeSubdirectories = true;

                // Add event handlers
                watcher.Created += OnCreated;

                // Begin watching for changes to input directory
                watcher.EnableRaisingEvents = true;

                // Scan Input directory forever or at least until shutdown
                do
                {
                    Thread.Sleep(250);
                }
                while (StaticClass.ShutdownFlag == false);
            }
        }
    }
}
