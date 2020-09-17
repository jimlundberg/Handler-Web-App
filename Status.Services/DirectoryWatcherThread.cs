using Microsoft.Extensions.Logging;
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
        public event EventHandler ProcessCompleted;

        /// <summary>
        /// New Jobs directory Scan Thread constructor receiving data buffers
        /// </summary>
        public DirectoryWatcherThread() { }

        /// <summary>
        /// Directory Watcher thread default destructor
        /// </summary>
        ~DirectoryWatcherThread()
        {
            //StaticClass.Logger.LogInformation("DirectoryWatcherThread default destructor called");
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
            int index = 0;

            // Do Shutdown Pause check
            if (StaticClass.ShutDownPauseCheck("Directory Watcher OnCreated") == false)
            {
                // Loop shutdown/Pause check
                Task AddTask = Task.Run(() =>
                {
                    index = StaticClass.InputJobsToRun.Count + 1;
                    StaticClass.InputJobsToRun.Add(index, job);
                });

                TimeSpan timeSpan = TimeSpan.FromMilliseconds(StaticClass.ADD_JOB_DELAY);
                if (!AddTask.Wait(timeSpan))
                {
                    StaticClass.Logger.LogError("DirectoryWatcherThread Add Job {0} timed out at {1:HH:mm:ss.fff}", job, DateTime.Now);
                }

                StaticClass.Log(string.Format("\nInput Directory Watcher added new Job {0} to Input Job list index {1} at {2:HH:mm:ss.fff}\n",
                    job, index, DateTime.Now));
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
