using Microsoft.Extensions.Logging;
using StatusModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Permissions;
using System.Threading;

namespace Status.Services
{
    /// <summary>
    /// Class to monitor for new directories created in a specified directory
    /// </summary>
    public class DirectoryWatcherThread
    {
        public static string DirectoryName;
        public static IniFileData IniData;
        public static List<StatusData> StatusData;
        private static Thread thread;
        public event EventHandler ProcessCompleted;
        public static ILogger<StatusRepository> Logger;
        public static bool jobWaiting = false;

        /// <summary>
        /// New Jobs Directory Scan Thread constructor receiving data buffers
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public DirectoryWatcherThread(IniFileData iniData,
            List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            IniData = iniData;
            StatusData = statusData;
            DirectoryName = iniData.InputDir;
            Logger = logger;
            StaticData.DirectoryScanComplete = false;
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

        /// <summary>
        /// Timeout Handler
        /// </summary>
        /// <param name="job"></param>
        public static void TimeoutHandler(StatusMonitorData monitorData)
        {
            string job = monitorData.Job;

            Console.WriteLine(String.Format("Timeout Handler for job {0}", job));

            // Get job name from directory name
            string processingBufferDirectory = IniData.ProcessingDir + @"\" + job;
            string repositoryDirectory = IniData.RepositoryDir + @"\" + job;

            // If the repository directory does not exist, create it
            if (!Directory.Exists(repositoryDirectory))
            {
                Directory.CreateDirectory(repositoryDirectory);
            }

            // Move Processing Buffer Files to the Repository directory when failed
            FileHandling.CopyFolderContents(processingBufferDirectory, repositoryDirectory, Logger, true, true);

            StaticData.NumberOfJobsExecuting--;
            StaticData.TcpIpScanComplete[job] = true;
            StaticData.DirectoryScanComplete = true;
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
            string job = e.FullPath;
            StaticData.NewJobsToRun.Add(job);

            // Directory Add detected
            StaticData.Log(IniData.ProcessLogFile,
                (String.Format("\nInput Directory Watcher detected new directory {0} at {1:HH:mm:ss.fff}",
                e.FullPath, DateTime.Now)));

            if (StaticData.NumberOfJobsExecuting < IniData.ExecutionLimit)
            {
                // Run the job and remove it from the list
                CurrentInutJobsScanThread newJobsScanThread = new CurrentInutJobsScanThread();
                newJobsScanThread.StartJob(job, false, IniData, StatusData, Logger);
                StaticData.NumberOfJobsExecuting++;
                StaticData.NewJobsToRun.Remove(job);
                StaticData.FoundNewJobReadyToRun = true;
                Thread.Sleep(IniData.ScanTime);
                StaticData.DirectoryScanComplete = false;
            }
            else
            {
                StaticData.DirectoryScanComplete = true;
            }
        }

        /// <summary>
        /// Scan directory for added and deleted directories
        /// </summary>
        /// <param name="directory"></param>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void WatchDirectory(string directory)
        {
            // Get job name from directory name
            string job = directory.Replace(IniData.ProcessingDir, "").Remove(0, 1);

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

                // Begin watching for changes to input directory
                watcher.EnableRaisingEvents = true;

                // Enter infinite loop waiting for job and Tcp/IP scan complete
                do
                {
                    // Run new jobs waiting
                    if (jobWaiting && (StaticData.DirectoryScanComplete == false))
                    {
                        if (StaticData.NewJobsToRun.Count > 0)
                        {
                            if (StaticData.NumberOfJobsExecuting < IniData.ExecutionLimit)
                            {
                                foreach (var dir in StaticData.NewJobsToRun)
                                {
                                    CurrentInutJobsScanThread newJobsScanThread = new CurrentInutJobsScanThread();
                                    newJobsScanThread.StartJob(dir, true, IniData, StatusData, Logger);
                                    StaticData.NumberOfJobsExecuting++;
                                    Thread.Sleep(IniData.ScanTime);
                                }
                                StaticData.DirectoryScanComplete = true;
                            }
                        }
                    }

                    Thread.Sleep(250);
                }
                while ((StaticData.DirectoryScanComplete == false) && (StaticData.ShutdownFlag == false));

                // Exiting thread message
                StaticData.Log(IniData.ProcessLogFile,
                    String.Format("Exiting DirectoryWatcherThread with ExitDirectoryScan {0} and ShutdownFlag {1}",
                    StaticData.DirectoryScanComplete, StaticData.ShutdownFlag));
            }
        }
    }
}
