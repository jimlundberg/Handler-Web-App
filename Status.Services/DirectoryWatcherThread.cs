﻿using Microsoft.Extensions.Logging;
using Status.Models;
using System;
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
        private readonly string DirectoryName;
        private readonly IniFileData IniData;
        public event EventHandler ProcessCompleted;
        public static ILogger<StatusRepository> Logger;

        /// <summary>
        /// New Jobs directory Scan Thread constructor receiving data buffers
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="logger"></param>
        public DirectoryWatcherThread(IniFileData iniData, ILogger<StatusRepository> logger)
        {
            IniData = iniData;
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
        /// A Thread procedure that scans for directories created in selected directory
        /// </summary>
        public void ThreadProc()
        {
            StaticClass.DirectoryWatcherThreadHandle = new Thread(() => WatchDirectory(DirectoryName, IniData));
            if (StaticClass.DirectoryWatcherThreadHandle == null)
            {
                Logger.LogError("DirectoryWatcherThread thread failed to instantiate");
            }
            StaticClass.DirectoryWatcherThreadHandle.Start();
        }

        // Define the event handlers.
        /// <summary>
        /// The directory created callback
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void OnCreated(object source, FileSystemEventArgs e)
        {
            // Store job to run now or later
            string directory = e.FullPath;
            string job = directory.Replace(IniData.InputDir, "").Remove(0, 1);

            // Directory Add detected
            StaticClass.Log((String.Format("Input Directory Watcher detected new Input job {0} at {1:HH:mm:ss.fff}",
                directory, DateTime.Now)));

            // Add new job found to the Input job list
            StaticClass.InputJobsToRun.Add(job);

            StaticClass.Log(String.Format("Input Job Scan detected and added new Input job {0} to Input job list at {1:HH:mm:ss.fff}",
                job, DateTime.Now));
        }

        /// <summary>
        /// Scan selected directory for created directories
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="iniData"></param>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void WatchDirectory(string directory, IniFileData iniData)
        {
            // Get job name from directory name
            string job = directory.Replace(iniData.ProcessingDir, "").Remove(0, 1);

            // Create a new FileSystemWatcher and set its properties
            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                if (watcher == null)
                {
                    Logger.LogError("DirectoryWatcherThread watcher failed to instantiate");
                }

                // Watch for changes in the directory list
                watcher.NotifyFilter = NotifyFilters.DirectoryName;
                watcher.Path = directory;

                // Watch for any directories names added
                watcher.Filter = "*.*";
                watcher.IncludeSubdirectories = true;

                // Add event handlers
                watcher.Created += OnCreated;

                // Begin watching for directory changes to Input directory
                watcher.EnableRaisingEvents = true;

                // Scan Input directory forever
                do
                {
                    // Check if the shutdown flag is set, exit method
                    if (StaticClass.ShutdownFlag == true)
                    {
                        StaticClass.Log(String.Format("\nShutdown InputJobsScanThread StartInputJob of job {0} at {1:HH:mm:ss.fff}",
                            job, DateTime.Now));
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
                while (true);
            }
        }
    }
}
