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
        private readonly string DirectoryName;
        private readonly string Job;
        public event EventHandler ProcessCompleted;

        /// <summary>
        /// Current Processing File Watcher thread default constructor
        /// </summary>
        public ProcessingFileWatcherThread()
        {
            StaticClass.Logger.LogInformation("ProcessingFileWatcherThread default constructor called");
        }

        /// <summary>
        /// Current Processing File Watcher thread default destructor
        /// </summary>
        ~ProcessingFileWatcherThread()
        {
            StaticClass.Logger.LogInformation("ProcessingFileWatcherThread default destructor called");
        }

        /// <summary>
        /// Processing directory file watcher thread
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="numberOfFilesNeeded"></param>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></param>
        /// <param name="statusData"></param>
        public ProcessingFileWatcherThread(string directory, StatusMonitorData monitorData, IniFileData iniData)
        {
            DirectoryName = directory;
            IniData = iniData;
            Job = directory.Replace(iniData.ProcessingDir, "").Remove(0, 1);
            DirectoryInfo ProcessingJobInfo = new DirectoryInfo(directory);
            if (ProcessingJobInfo == null)
            {
                StaticClass.Logger.LogError("ProcessingFileWatcherThread ProcessingJobInfo failed to instantiate");
            }
            StaticClass.NumberOfProcessingFilesFound[Job] = ProcessingJobInfo.GetFiles().Length;
            StaticClass.NumberOfProcessingFilesNeeded[Job] = monitorData.NumFilesConsumed + monitorData.NumFilesProduced;
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

                StaticClass.Log(string.Format("\nProcessing File Watcher detected {0} for Job {1} file {2} of {3} at {4:HH:mm:ss.fff}\n",
                    jobFile, job, StaticClass.NumberOfProcessingFilesFound[job], StaticClass.NumberOfProcessingFilesNeeded[job], DateTime.Now));

                // If Number of Processing files is complete
                if (StaticClass.NumberOfProcessingFilesFound[job] == StaticClass.NumberOfProcessingFilesNeeded[job])
                {
                    StaticClass.Log(string.Format("Processing File Watcher detected Job {0} complete set of {1} files at {2:HH:mm:ss.fff}",
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
                string dataXmlFile = directory + @"\" + "Data.xml";

                // Wait for data.xml file to be ready
                do
                {
                    Thread.Sleep(StaticClass.FILE_WAIT_DELAY);
                }
                while (StaticClass.IsFileReady(dataXmlFile) == false);

                // Check if the OverallResult node exists
                XmlDocument dataXmlDoc = new XmlDocument();
                dataXmlDoc.Load(dataXmlFile);
                XmlNode OverallResult = dataXmlDoc.DocumentElement.SelectSingleNode("/Data/OverallResult/result");
                if (OverallResult != null)
                {
                    OverallResultEntryFound = true;
                }

                // Check for shutdown or pause
                if (StaticClass.ShutDownPauseCheck("Overall Result Entry Check") == true)
                {
                    StaticClass.Log(string.Format("\nShutdown ProcessingFileWatcherThread OverallResultEntryCheck for file {0} at {1:HH:mm:ss.fff}",
                        directory, DateTime.Now));
                    return false;
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
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void WatchFiles(string directory, IniFileData iniData)
        {
            // Get job name from directory name
            string job = directory.Replace(iniData.ProcessingDir, "").Remove(0, 1);

            // Quick check to see if the directory is already full
            if (StaticClass.NumberOfProcessingFilesFound[job] == StaticClass.NumberOfProcessingFilesNeeded[job])
            {
                StaticClass.Log(string.Format("Processing File Watcher thread completed the scan for Job {0} at {1:HH:mm:ss.fff}",
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

                StaticClass.Log(string.Format("Processing File Watcher watching {0} at {1:HH:mm:ss.fff}",
                    directory, DateTime.Now));

                // Wait for Processing file scan to Complete with a full set of job output files
                do
                {
                    // Check for shutdown or pause
                    if (StaticClass.ShutDownPauseCheck("Processing File Watcher Thread") == true)
                    {
                        StaticClass.Log(string.Format("\nShutdown ProcessingFileWatcherThread watching {0} at {1:HH:mm:ss.fff}",
                            directory, DateTime.Now));

                        return;
                    }

                    Thread.Yield();
                }
                while ((StaticClass.ProcessingFileScanComplete[job] == false) || (StaticClass.TcpIpScanComplete[job] == false));

                // Check if the Processing Job Complete flag is still false or you get a shutdown error
                if (StaticClass.ProcessingJobScanComplete[job] == false)
                {
                    // Wait to make sure the data.xml is done being handled
                    Thread.Sleep(StaticClass.POST_PROCESS_WAIT);

                    // Wait for the data.xml file to contain a result
                    if (OverallResultEntryCheck(directory))
                    {
                        // set the Processing of a Job scan complete flag
                        StaticClass.ProcessingJobScanComplete[job] = true;

                        // Processing Thread Complete
                        StaticClass.Log(string.Format("Processing File Watcher thread completed the scan for Job {0} at {1:HH:mm:ss.fff}",
                            directory, DateTime.Now));
                    }
                    else
                    {
                        // Show error and return
                        StaticClass.Log(string.Format("Processing File could not confirm the OverallResult Entry for Job {0} at {1:HH:mm:ss.fff}",
                            directory, DateTime.Now));
                    }
                }
            }
        }
    }
}
