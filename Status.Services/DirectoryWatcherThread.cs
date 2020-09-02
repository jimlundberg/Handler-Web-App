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

        /// <summary>
        /// New Jobs directory Scan Thread constructor receiving data buffers
        /// </summary>
        /// <param name="iniData"></param>
        public DirectoryWatcherThread(IniFileData iniData)
        {
            DirectoryName = iniData.InputDir;
            IniData = iniData;
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
            StaticClass.DirectoryWatcherThreadHandle = new Thread(() => WatchDirectory(DirectoryName));
            if (StaticClass.DirectoryWatcherThreadHandle == null)
            {
                StaticClass.Logger.LogError("DirectoryWatcherThread thread failed to instantiate");
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
            string jobDirectory = e.FullPath;
            string job = jobDirectory.Replace(IniData.InputDir, "").Remove(0, 1);

            // Add new job detected to the Input job list
            StaticClass.InputJobsToRun.Add(job);

            StaticClass.Log(String.Format("Input Buffer Directory Watcher added new job {0} to job list at {1:HH:mm:ss.fff}",
                job, DateTime.Now));
        }

        /// <summary>
        /// Scan selected directory for created directories
        /// </summary>
        /// <param name="directory"></param>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void WatchDirectory(string directory)
        {
            // Create a new FileSystemWatcher and set its properties
            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                if (watcher == null)
                {
                    StaticClass.Logger.LogError("DirectoryWatcherThread watcher failed to instantiate");
                }

                // Watch for changes in the directory list
                watcher.NotifyFilter = NotifyFilters.DirectoryName;
                watcher.Path = directory;

                // Watch for any directories names added
                watcher.Filter = "*.*";
                watcher.IncludeSubdirectories = true;

                // Add OnCreated event handler which is the only one needed
                watcher.Created += OnCreated;

                // Begin watching for directory changes to Input directory
                watcher.EnableRaisingEvents = true;

                StaticClass.Log(String.Format("\nDirectory Watcher watching directory {0} at {1:HH:mm:ss.fff}",
                    directory, DateTime.Now));

                // Scan Input directory forever
                do
                {
                    // Check if the shutdown flag is set, exit method
                    if (StaticClass.ShutdownFlag == true)
                    {
                        StaticClass.Log(String.Format("\nShutdown DirectoryWatcherThread WatchDirectory at {0:HH:mm:ss.fff}", DateTime.Now));
                        return;
                    }

                    // Check if the pause flag is set, then wait for reset
                    if (StaticClass.PauseFlag == true)
                    {
                        StaticClass.Log(String.Format("DirectoryWatcherThread WatchDirectory is in Pause mode at {0:HH:mm:ss.fff}", DateTime.Now));
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
