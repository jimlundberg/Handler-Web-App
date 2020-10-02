using Microsoft.Extensions.Logging;
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
        public event EventHandler ProcessCompleted;

        /// <summary>
        /// New Jobs directory Scan Thread constructor receiving data buffers
        /// </summary>
        public DirectoryWatcherThread() { }

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
            StaticClass.DirectoryWatcherThreadHandle = new Thread(() => WatchDirectory());
            if (StaticClass.DirectoryWatcherThreadHandle == null)
            {
                StaticClass.Logger.LogError("DirectoryWatcherThread DirectoryWatcherThreadHandle thread failed to instantiate");
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
            string job = jobDirectory.Replace(StaticClass.IniData.InputDir, "").Remove(0, 1);

            StaticClass.Log(string.Format("\nInput Directory Watcher checking new Job {0} for Input Job list at {1:HH:mm:ss.fff}",
                job, DateTime.Now));

            if (StaticClass.ShutDownPauseCheck("Directory Watcher OnCreated") == false)
            {
                Thread.Sleep(StaticClass.WAIT_FOR_FILES_TO_COMPLETE);

                // Check directory contents complete
                if (StaticClass.CheckDirectoryReady(jobDirectory) == true)
                {
                    StaticClass.AddJobToList(job);

                    if ((StaticClass.IniData.DebugMode & (1 << (byte)DebugModeState.JOB_LIST)) > 0)
                    {
                        StaticClass.DisplayJobList();
                    }
                }
                else
                {
                    StaticClass.Logger.LogError("DirectoryWatcherThread Job {0} directory check failed at {1:HH:mm:ss.fff}",
                        job, DateTime.Now);
                }
            }
        }

        /// <summary>
        /// The Change of files callback
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void OnChanged(object source, FileSystemEventArgs e)
        {
            // Ignore Changes
        }

        /// <summary>
        /// Watch selected directory for new directories created
        /// </summary>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void WatchDirectory()
        {
            // Create a new FileSystemWatcher and set its properties
            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                if (watcher == null)
                {
                    StaticClass.Logger.LogError("DirectoryWatcherThread watcher failed to instantiate");
                }

                // Watch for changes in the directory list
                watcher.NotifyFilter =
                    NotifyFilters.DirectoryName |
                    NotifyFilters.CreationTime;

                // Set the Input Buffre as path to watch for new directory additions
                watcher.Path = StaticClass.IniData.InputDir;

                // Watch for any directories names added
                watcher.Filter = "*.*";
                watcher.IncludeSubdirectories = true;

                // Add OnCreated event handler which is the only one needed
                watcher.Created += OnCreated;
                watcher.Changed += OnChanged;

                // Begin watching for directory changes to Input directory
                watcher.EnableRaisingEvents = true;

                // Scan Input directory forever
                do
                {
                    Thread.Yield();
                }
                while (StaticClass.ShutDownPauseCheck("Directory Watcher") == false);
            }
        }
    }
}
