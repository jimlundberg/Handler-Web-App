﻿using Microsoft.Extensions.Logging;
using Status.Models;
using System;
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
        private readonly IniFileData IniData;
        private readonly string DirectoryName;
        private readonly string Job;
        public event EventHandler ProcessCompleted;

        /// <summary>
        /// Input directory file watcher thread
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="numberOfFilesNeeded"></param>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></param>
        public InputFileWatcherThread(string directory, int numberOfFilesNeeded,
            IniFileData iniData, StatusMonitorData monitorData)
        {
            DirectoryName = directory;
            IniData = iniData;
            Job = monitorData.Job;
            DirectoryInfo InputJobInfo = new DirectoryInfo(DirectoryName);
            if (InputJobInfo == null)
            {
                StaticClass.Logger.LogError("InputFileWatcherThread InputJobInfo failed to instantiate");
            }

            StaticClass.NumberOfInputFilesFound[Job] = InputJobInfo.GetFiles().Length;
            StaticClass.NumberOfInputFilesNeeded[Job] = numberOfFilesNeeded;
            StaticClass.InputFileScanComplete[Job] = false;
            StaticClass.InputJobScanComplete[Job] = false;
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
        public void ThreadProc()
        {
            StaticClass.InputFileWatcherThreadHandle = new Thread(() => WatchFiles(DirectoryName, IniData));
            if (StaticClass.InputFileWatcherThreadHandle == null)
            {
                StaticClass.Logger.LogError("InputFileWatcherThread thread failed to instantiate");
            }
            StaticClass.InputFileWatcherThreadHandle.Start();
        }

        /// <summary>
        /// The Add or Change of files callback
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void OnCreated(object source, FileSystemEventArgs e)
        {
            string fullDirectory = e.FullPath;
            string jobDirectory = fullDirectory.Replace(IniData.InputDir, "").Remove(0, 1);
            string jobFile = jobDirectory.Substring(jobDirectory.LastIndexOf('\\') + 1);
            string job = jobDirectory.Substring(0, jobDirectory.LastIndexOf('\\'));         

            // If Number of files is not complete
            if (StaticClass.NumberOfInputFilesFound[job] < StaticClass.NumberOfInputFilesNeeded[job])
            {
                // Increment the number of Input Buffer Job files found
                StaticClass.NumberOfInputFilesFound[job]++;

                StaticClass.Log(String.Format("\nInput File Watcher detected {0} for Job {1} file {2} of {3} at {4:HH:mm:ss.fff}",
                    jobFile, job, StaticClass.NumberOfInputFilesFound[job], StaticClass.NumberOfInputFilesNeeded[job], DateTime.Now));

                // If Number of Input files is complete
                if (StaticClass.NumberOfInputFilesFound[job] == StaticClass.NumberOfInputFilesNeeded[job])
                {
                    StaticClass.Log(String.Format("Input File Watcher detected Job {0} complete set of {1} files at {2:HH:mm:ss.fff}",
                    job, StaticClass.NumberOfInputFilesNeeded[job], DateTime.Now));

                    // Signal the Run thread that the Input Buffer files were found
                    StaticClass.InputFileScanComplete[job] = true;
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
        /// TCP/IP Scan Complete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void TcpIp_ScanCompleted(object sender, EventArgs e)
        {
            string job = e.ToString();

            StaticClass.Log(String.Format("InputFileWatcherThread received TCP/IP Scan Completed for Job {0} at {1:HH:mm:ss.fff}",
                job, DateTime.Now));

            // Signal that the TCP/IP scan for a job is complete
            StaticClass.TcpIpScanComplete[job] = true;
        }

        /// <summary>
        /// Monitor a Directory for a selected number of files with a timeout
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="iniData"></param>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void WatchFiles(string directory, IniFileData iniData)
        {
            // Get job name from directory name
            string job = directory.Replace(iniData.InputDir, "").Remove(0, 1);

            if (StaticClass.NumberOfInputFilesFound[job] == StaticClass.NumberOfInputFilesNeeded[job])
            {
                // Exiting thread message
                StaticClass.Log(String.Format("Input File Watcher thread completed the scan for Job {0} at {1:HH:mm:ss.fff}",
                    job, DateTime.Now));

                // Signal the Run thread that the Input files were found
                StaticClass.InputFileScanComplete[job] = true;
                return;
            }

            // Create a new FileSystemWatcher and set its properties
            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                if (watcher == null)
                {
                    StaticClass.Logger.LogError("InputFileWatcherThread watcher failed to instantiate");
                }

                // Watch for file changes in the watched directory
                watcher.NotifyFilter =
                    NotifyFilters.FileName |
                    NotifyFilters.CreationTime |
                    NotifyFilters.LastAccess;

                // Set the Path to scan for files
                watcher.Path = directory;

                // Watch for any file to get directory changes
                watcher.Filter = "*.*";

                // Add event handlers
                watcher.Created += OnCreated;
                watcher.Changed += OnChanged;

                // Begin watching for changes to Input directory
                watcher.EnableRaisingEvents = true;

                StaticClass.Log(String.Format("Input File Watcher watching {0} at {1:HH:mm:ss.fff}",
                    directory, DateTime.Now));

                // Wait for Input file scan to Complete with a full set of job output files
                do
                {
                    Thread.Yield();

                    if (StaticClass.ShutdownFlag == true)
                    {
                        StaticClass.Log(String.Format("\nShutdown InputFileWatcherThread WatchFiles watching {0} at {1:HH:mm:ss.fff}",
                            directory, DateTime.Now));
                        return;
                    }

                    // Check if the pause flag is set, then wait for reset
                    if (StaticClass.PauseFlag == true)
                    {
                        StaticClass.Log(String.Format("InputFileWatcherThread WatchFiles is in Pause mode at {0:HH:mm:ss.fff}", DateTime.Now));
                        do
                        {
                            Thread.Yield();
                        }
                        while (StaticClass.PauseFlag == true);
                    }
                }
                while (StaticClass.InputFileScanComplete[job] == false);

                // Signal the Input Job Complete flag for the Job
                StaticClass.InputJobScanComplete[job] = true;

                // Exiting thread message
                StaticClass.Log(String.Format("Input File Watcher thread completed the scan for Job {0} at {1:HH:mm:ss.fff}",
                    job, DateTime.Now));
            }
        }
    }
}
