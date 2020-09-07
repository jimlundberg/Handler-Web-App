using Microsoft.Extensions.Logging;
using Status.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;
using System.Threading;
using System.Xml;

namespace Status.Services
{
    /// <summary>
    /// Class to Monitor the number of files in a directory
    /// </summary>
    public class ProcessingFileWatcherThread
    {
        private readonly IniFileData IniData;
        private readonly StatusMonitorData MonitorData;
        private readonly List<StatusData> StatusDataList;
        private readonly string DirectoryName;
        private readonly string Job;
        public event EventHandler ProcessCompleted;

        /// <summary>
        /// Processing directory file watcher thread
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="numberOfFilesNeeded"></param>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></param>
        /// <param name="statusData"></param>
        public ProcessingFileWatcherThread(string directory, int numberOfFilesNeeded, IniFileData iniData,
            StatusMonitorData monitorData, List<StatusData> statusData)
        {
            DirectoryName = directory;
            IniData = iniData;
            MonitorData = monitorData;
            StatusDataList = statusData;
            Job = monitorData.Job;
            DirectoryInfo ProcessingJobInfo = new DirectoryInfo(directory);
            StaticClass.NumberOfProcessingFilesFound[Job] = ProcessingJobInfo.GetFiles().Length;
            StaticClass.NumberOfProcessingFilesNeeded[Job] = numberOfFilesNeeded;
            StaticClass.TcpIpScanComplete[Job] = false;
            StaticClass.ProcessingFileScanComplete[Job] = false;
            StaticClass.ProcessingJobScanComplete[Job] = false;
        }

        /// <summary>
        /// Process complete callback
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnProcessCompleted(EventArgs e)
        {
            ProcessCompleted?.Invoke(this, e);
        }

        /// <summary>
        /// Thread procedure to start the Processing job file watching
        /// </summary>
        public void ThreadProc()
        {
            StaticClass.ProcessingFileWatcherThreadHandle = new Thread(() => WatchFiles(DirectoryName, IniData));
            if (StaticClass.ProcessingFileWatcherThreadHandle == null)
            {
                StaticClass.Logger.LogError("ProcessingFileWatcherThread thread failed to instantiate");
            }
            StaticClass.ProcessingFileWatcherThreadHandle.Start();
        }

        /// <summary>
        /// The Add of files callback
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void OnCreated(object source, FileSystemEventArgs e)
        {
            string fullDirectory = e.FullPath;
            string jobDirectory = fullDirectory.Replace(IniData.ProcessingDir, "").Remove(0, 1);
            string jobFile = jobDirectory.Substring(jobDirectory.LastIndexOf('\\') + 1);
            string job = jobDirectory.Substring(0, jobDirectory.LastIndexOf('\\'));

            // If Number of files is not complete
            if (StaticClass.NumberOfProcessingFilesFound[job] < StaticClass.NumberOfProcessingFilesNeeded[job])
            {
                // Increment the number of Processing Buffer Job files found
                StaticClass.NumberOfProcessingFilesFound[job]++;

                StaticClass.Log(String.Format("Processing File Watcher detected {0} for Job {1} file {2} of {3} at {4:HH:mm:ss.fff}",
                    jobFile, job, StaticClass.NumberOfProcessingFilesFound[job], StaticClass.NumberOfProcessingFilesNeeded[job], DateTime.Now));

                // If Number of Processing files is complete
                if (StaticClass.NumberOfProcessingFilesFound[job] == StaticClass.NumberOfProcessingFilesNeeded[job])
                {
                    StaticClass.Log(String.Format("Processing File Watcher detected Job {0} complete set of {1} files at {2:HH:mm:ss.fff}",
                        job, StaticClass.NumberOfProcessingFilesNeeded[job], DateTime.Now));

                    // Signal the Run thread that the Processing Buffer files were found
                    StaticClass.ProcessingFileScanComplete[job] = true;
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
        /// Check if the Modeler has deposited the OverallResult entry in the job data.xml file
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public bool OverallResultEntryCheck(string directory)
        {
            bool OverallResultEntryFound = false;
            do
            {
                string xmlFileName = directory + @"\" + "Data.xml";

                // Read output Xml file data
                XmlDocument XmlDoc = new XmlDocument();

                // Wait for xml file to be ready
                if (StaticClass.IsFileReady(xmlFileName) == true)
                {
                    // Load the xml file
                    XmlDoc.Load(xmlFileName);
                }

                // Check if the OverallResult node exists
                XmlNode OverallResult = XmlDoc.DocumentElement.SelectSingleNode("/Data/OverallResult/result");
                if (OverallResult != null)
                {
                    OverallResultEntryFound = true;
                }

                if (StaticClass.ShutdownFlag == true)
                {
                    StaticClass.Log(String.Format("\nShutdown ProcessingFileWatcherThread OverallResultEntryCheck for file {0} at {1:HH:mm:ss.fff}",
                        directory, DateTime.Now));
                    return false;
                }

                // Check if the pause flag is set, then wait for reset
                if (StaticClass.PauseFlag == true)
                {
                    StaticClass.Log(String.Format("ProcessingFileWatcherThread OverallResultEntryCheck is in Pause mode at {0:HH:mm:ss.fff}", DateTime.Now));
                    do
                    {
                        Thread.Yield();
                    }
                    while (StaticClass.PauseFlag == true);
                }

                Thread.Yield();
            }
            while (OverallResultEntryFound == false);

            return OverallResultEntryFound;
        }

        /// <summary>
        /// Monitor a directory for a complete set of Processing files for a job 
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></param>
        /// <param name="statusData"></param>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void WatchFiles(string directory, IniFileData iniData)
        {
            // Get job name from directory name
            string job = directory.Replace(iniData.ProcessingDir, "").Remove(0, 1);

            // Quick check to see if the directory is already full
            if (StaticClass.NumberOfProcessingFilesFound[job] == StaticClass.NumberOfProcessingFilesNeeded[job])
            {
                StaticClass.Log(String.Format("Processing File Watcher thread completed the scan for Job {0} at {1:HH:mm:ss.fff}",
                    job, DateTime.Now));

                // Signal the Run thread that the Processing files were found
                StaticClass.ProcessingFileScanComplete[job] = true;
                return;
            }

            // Create a new FileSystemWatcher and set its properties
            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                if (watcher == null)
                {
                    StaticClass.Logger.LogError("ProcessingFileWatcherThread watcher failed to instantiate");
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

                // Begin watching for file changes to Processing job directory
                watcher.EnableRaisingEvents = true;

                StaticClass.Log(String.Format("Processing File Watcher watching {0} at {1:HH:mm:ss.fff}",
                    directory, DateTime.Now));

                // Wait for Processing file scan to Complete with a full set of job output files
                do
                {
                    if (StaticClass.ShutdownFlag == true)
                    {
                        StaticClass.Log(String.Format("\nShutdown ProcessingFileWatcherThread watching {0} at {1:HH:mm:ss.fff}",
                            directory, DateTime.Now));

                        return;
                    }

                    // Check if the pause flag is set, then wait for reset
                    if (StaticClass.PauseFlag == true)
                    {
                        StaticClass.Log(String.Format("ProcessingFileWatcherThread WatchFiles is in Pause mode at {0:HH:mm:ss.fff}", DateTime.Now));
                        do
                        {
                            Thread.Yield();
                        }
                        while (StaticClass.PauseFlag == true);
                    }

                    Thread.Yield();
                }
                while ((StaticClass.ProcessingFileScanComplete[job] == false) || (StaticClass.TcpIpScanComplete[job] == false));

                // Check if the Processing Job Complete flag is still false or you get a shutdown error
                if (StaticClass.ProcessingJobScanComplete[job] == false)
                {
                    // Wait for the data.xml file to contain a result
                    if (OverallResultEntryCheck(directory))
                    {
                        StaticClass.ProcessingJobScanComplete[job] = true;
                    }
                }

                // Exiting thread message
                StaticClass.Log(String.Format("Processing File Watcher thread completed the scan for Job {0} at {1:HH:mm:ss.fff}",
                    directory, DateTime.Now));
            }
        }
    }
}
