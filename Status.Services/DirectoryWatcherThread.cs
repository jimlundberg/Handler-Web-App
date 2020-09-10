using Microsoft.Extensions.Logging;
using Status.Models;
using System;
using System.IO;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;

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
        private static readonly Object ListLock = new Object();

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
            int index = 0;

            Task AddTask = Task.Run(() =>
            {
                index = StaticClass.InputJobsToRun.Count + 1;
                StaticClass.InputJobsToRun.Add(index, job);
            });

            TimeSpan timeSpan = TimeSpan.FromMilliseconds(150);
            if (!AddTask.Wait(timeSpan))
            {
                StaticClass.Logger.LogError("DirectoryWatcherThread Add Job {0} timed out at {1:HH:mm:ss.fff}", job, DateTime.Now);
            }

            StaticClass.Log(String.Format("\nInput Directory Watcher added new Job {0} to Input Job list index {1} at {2:HH:mm:ss.fff}\n",
                job, index, DateTime.Now));
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
                watcher.NotifyFilter =
                    NotifyFilters.DirectoryName |
                    NotifyFilters.CreationTime;

                // Set the Path to scan for directories
                watcher.Path = directory;

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
