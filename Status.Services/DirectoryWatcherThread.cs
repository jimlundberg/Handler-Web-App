using Microsoft.Extensions.Logging;
using StatusModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;
using System.Threading;

namespace Status.Services
{
    public class DirectoryWatcherThread
    {
        public string Directory;
        private IniFileData IniData;
        private static readonly List<String> directoryInfoList = new List<String>();
        private static Thread thread;
        public event EventHandler ProcessCompleted;
        public static ILogger<StatusRepository> Logger;

        /// <summary>
        /// New Jobs Directory Scan Thread constructor receiving data buffers
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public DirectoryWatcherThread(IniFileData iniData, ILogger<StatusRepository> logger)
        {
            IniData = iniData;
            Directory = iniData.InputDir;
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
            thread = new Thread(() => WatchDirectory(Directory));
            if (thread == null)
            {
                Logger.LogError("DirectoryWatcherThread WatchDirectory thread failed to instantiate");
            }
            thread.Start();
        }

        // Define the event handlers.
        /// <summary>
        /// The Add or Change of directory callback
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public static void OnChanged(object source, FileSystemEventArgs e)
        {
            // Directory Added (or changed???)
            Console.WriteLine($"WatchDirectory detected: {e.FullPath} {e.ChangeType}");
            directoryInfoList.Add(e.FullPath);
        }

        /// <summary>
        /// The Delete of directory callback
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public static void OnDeleted(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a directory is deleted.
            Console.WriteLine($"WatchDirectory detected: {e.FullPath} {e.ChangeType}");
            directoryInfoList.Remove(e.FullPath);
        }

        /// <summary>
        /// Access method to get the current directory list
        /// </summary>
        /// <returns></returns>
        public static List<String> GetCurrentDirectoryList()
        {
            return directoryInfoList;
        }

        /// <summary>
        /// Scan directory for added and deleted directories
        /// </summary>
        /// <param name="directory"></param>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void WatchDirectory(string directory)
        {
            // Create a new FileSystemWatcher and set its properties.
            using (FileSystemWatcher watcher = new FileSystemWatcher(directory))
            {
                if (watcher == null)
                {
                    Logger.LogError("NewJobScanThread watcher failed to instantiate");
                }

                // Watch for changes in the directory list
                watcher.NotifyFilter = NotifyFilters.DirectoryName;

                // Watch for any file to get directory changes
                watcher.Filter = "*.*";
                watcher.IncludeSubdirectories = true;

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
                while ((StaticData.ExitDirectoryScan == false) && 
                       (StaticData.ShutdownFlag == false));

                // Exiting thread message
                Console.WriteLine("Exiting DirectoryWatcherThread with ExitDirectoryScan {0} and ShutdownFlag {1}",
                    StaticData.ExitDirectoryScan, StaticData.ShutdownFlag);
            }
        }
    }
}
