using Microsoft.Extensions.Logging;
using StatusModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;

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
        public static string DirectoryName;
        private static string Job;
        public event EventHandler ProcessCompleted;
        public static ILogger<StatusRepository> Logger;

        /// <summary>
        /// Input directory file watcher thread
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="numberOfFilesNeeded"></param>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public InputFileWatcherThread(string directory, int numberOfFilesNeeded, IniFileData iniData,
            StatusMonitorData monitorData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            DirectoryName = directory;
            IniData = iniData;
            MonitorData = monitorData;
            StatusData = statusData;
            Logger = logger;
            Job = monitorData.Job;
            DirectoryInfo InputJobInfo = new DirectoryInfo(DirectoryName);
            StaticClass.NumberOfInputFilesFound[Job] = InputJobInfo.GetFiles().Length;
            StaticClass.NumberOfInputFilesNeeded[Job] = numberOfFilesNeeded;
            StaticClass.InputFileScanComplete[Job] = false;
            StaticClass.InputJobScanComplete[Job] = false;

            // Check for current unfinished job(s) in the Input Buffer
            InputJobsReadyCheck(Job, iniData, statusData, logger);
        }

        /// <summary>
        /// Check if unfinished Input Jobs jobs are currently waiting to run
        /// </summary>
        /// <param name="job"></param>
        /// <param name="iniData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public void InputJobsReadyCheck(string job, IniFileData iniData, 
            List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            if (StaticClass.InputFileScanComplete[job] == true)
            {
                if (StaticClass.NumberOfJobsExecuting < iniData.ExecutionLimit)
                {
                    // Strt Input jobs currently waiting
                    for (int i = 0; i < StaticClass.NewInputJobsToRun.Count; i++)
                    {
                        string directory = iniData.InputDir + @"\" + StaticClass.NewInputJobsToRun[i];
                        CurrentInputJobsScanThread currentInputJobsScan = new CurrentInputJobsScanThread();
                        currentInputJobsScan.StartInputJob(directory, iniData, statusData, logger);

                        // Throttle the Job startups
                        var jobWaitTask = Task.Run(async delegate
                        {
                            await Task.Delay(StaticClass.ScanWaitTime);
                            return;
                        });
                        jobWaitTask.Wait();
                    }
                }
                else
                {
                    // Add currently unfinished job to Input Jobs run list
                    StaticClass.NewInputJobsToRun.Add(job);

                    StaticClass.Log(IniData.ProcessLogFile,
                        String.Format("\nInput file watcher added waiting job {0} to Input Job List at {0:HH:mm:ss.fff}",
                        job, DateTime.Now));
                }
            }
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
            StaticClass.InputFileWatcherThreadHandle = new Thread(() => WatchFiles(DirectoryName));
            if (StaticClass.InputFileWatcherThreadHandle == null)
            {
                Logger.LogError("InputFileWatcherThread thread failed to instantiate");
            }
            StaticClass.InputFileWatcherThreadHandle.Start();
        }

        /// <summary>
        /// The Add or Change of files callback
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public static void OnCreated(object source, FileSystemEventArgs e)
        {
            // Get job name from directory name
            string jobDirectory = e.FullPath;
            string jobFile = jobDirectory.Replace(IniData.InputDir, "").Remove(0, 1);
            string job = jobFile.Substring(0, jobFile.IndexOf(@"\"));

            StaticClass.NumberOfInputFilesFound[job]++;

            // Input job file added
            StaticClass.Log(IniData.ProcessLogFile,
                String.Format("\nInput File Watcher detected: {0} file {1} of {2} at {3:HH:mm:ss.fff}",
                jobDirectory, StaticClass.NumberOfInputFilesFound[job], StaticClass.NumberOfInputFilesNeeded[job], DateTime.Now));

            if (StaticClass.NumberOfInputFilesFound[job] == StaticClass.NumberOfInputFilesNeeded[job])
            {
                // All files needed dected
                StaticClass.Log(IniData.ProcessLogFile,
                    String.Format("\nInput File Watcher detected the complete {0} of {1} Input job {2} files at {3:HH:mm:ss.fff}",
                    StaticClass.NumberOfInputFilesFound[job], StaticClass.NumberOfInputFilesNeeded[job], job, DateTime.Now));

                // Signal the Run thread that the Input files were found
                StaticClass.InputFileScanComplete[job] = true;
            }
        }

        /// <summary>
        /// TCP/IP Scan Complete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void TcpIp_ScanCompleted(object sender, EventArgs e)
        {
            string job = e.ToString();

            StaticClass.Log(IniData.ProcessLogFile,
                String.Format("InputFileWatcherThread received Tcp/Ip Scan Completed for job {0} at {1:HH:mm:ss.fff}",
                job, DateTime.Now));
        }

        /// <summary>
        /// Monitor a Directory for a selected number of files with a timeout
        /// </summary>
        /// <param name="directory"></param>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void WatchFiles(string directory)
        {
            // Get job name from directory name
            string job = directory.Replace(IniData.InputDir, "").Remove(0, 1);

            if (StaticClass.NumberOfInputFilesFound[job] == StaticClass.NumberOfInputFilesNeeded[job])
            {
                // Signal the Run thread that the Input files were found
                StaticClass.InputFileScanComplete[job] = true;
                return;
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
                watcher.Created += OnCreated;

                // Begin watching for changes to Input directory
                watcher.EnableRaisingEvents = true;

                // Exiting thread message
                StaticClass.Log(IniData.ProcessLogFile,
                    String.Format("Input File Watcher watching {0} at {1:HH:mm:ss.fff}",
                    directory, DateTime.Now));

                // Wait for Input file Scan to Complete with enough files to start job
                do
                {
                    Thread.Yield();

                    if (StaticClass.ShutdownFlag == true)
                    {
                        StaticClass.Log(IniData.ProcessLogFile,
                            String.Format("\nShutdown InputFileWatcherThread WatchFiles watching {0} at {1:HH:mm:ss.fff}",
                            directory, DateTime.Now));
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
                }
                while (StaticClass.InputFileScanComplete[job] == false);

                // Remove job started from the Input job list
                StaticClass.InputJobScanComplete[job] = true;

                // Exiting thread message
                StaticClass.Log(IniData.ProcessLogFile,
                    String.Format("Input File Watcher exit of scan of {0} with InputFileScanComplete={1} and ShutdownFlag={2} at at {3:HH:mm:ss.fff}",
                    directory, StaticClass.InputFileScanComplete[job], StaticClass.ShutdownFlag, DateTime.Now));
            }
        }
    }
}
