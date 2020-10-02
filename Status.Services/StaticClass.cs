using Microsoft.Extensions.Logging;
using Status.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Status.Services
{
    /// <summary>
    /// Static Data with global access
    /// </summary>
    public static class StaticClass
    {
        // Common definitions
        public const int TCP_IP_STARTUP_WAIT = 60000;
        public const int STARTING_TCP_IP_WAIT = 15000;
        public const int POST_PROCESS_WAIT = 10000;
        public const int DIRECTORY_RECEIVE_WAIT = 250;
        public const int FILE_RECEIVE_WAIT = 1000;
        public const int WAIT_FOR_FILES_TO_COMPLETE = 5000;
        public const int DISPLAY_PROCESS_DATA_WAIT = 45000;
        public const int DISPLAY_PROCESS_TITLE_WAIT = 1000;
        public const int SHUTDOWN_PROCESS_WAIT = 5000;
        public const int READ_AVAILABLE_RETRY_DELAY = 250;
        public const int FILE_WAIT_DELAY = 2500;
        public const int FILE_READY_WAIT = 250;
        public const int ADD_JOB_DELAY = 2000;
        public const int GET_TOTAL_NUM_OF_JOBS_DELAY = 1500;
        public const int READ_JOB_DELAY = 2000;
        public const int DELETE_JOB_DELAY = 2000;
        public const int NUM_JOB_CHECK_RETRIES = 10;
        public const int NUM_TCP_IP_RETRIES = 240;
        public const int NUM_XML_ACCESS_RETRIES = 100;
        public const int NUM_RESULTS_ENTRY_RETRIES = 100;
        public const int NUM_REQUESTS_TILL_TCPIP_SLOWDOWN = 5;
        public const int LIST_PAUSE = 10;

        // Common counters
        public static double MaxJobTimeLimitSeconds = 0.0;
        public static int ScanWaitTime = 0;
        public static int NumberOfJobsExecuting = 0;
        public static int JobPortIndex = 0;
        public static int LogFileSizeLimit = 0;
        public static int TotalNumberOfJobs = 0;
        public static int CurrentJobIndex = 1;

        // Thread handles
        public static Thread InputJobsScanThreadHandle;
        public static Thread ProcessingFileWatcherThreadHandle;
        public static Thread ProcessingJobsScanThreadHandle;
        public static Thread DirectoryWatcherThreadHandle;
        public static Thread InputFileWatcherThreadHandle;
        public static Thread JobRunThreadHandle;
        public static Thread TcpListenerThreadHandle;

        // Global flags
        public static volatile bool ShutdownFlag = false;
        public static volatile bool PauseFlag = false;
        public static volatile bool UnfinishedProcessingJobsScanComplete = false;

        // Job state tracking
        public static Dictionary<string, DateTime> JobStartTime = new Dictionary<string, DateTime>();
        public static Dictionary<string, bool> InputFileScanComplete = new Dictionary<string, bool>();
        public static Dictionary<string, bool> InputJobScanComplete = new Dictionary<string, bool>();
        public static Dictionary<string, bool> ProcessingFileScanComplete = new Dictionary<string, bool>();
        public static Dictionary<string, bool> ProcessingJobScanComplete = new Dictionary<string, bool>();
        public static Dictionary<string, bool> JobShutdownFlag = new Dictionary<string, bool>();
        public static Dictionary<string, bool> TcpIpScanComplete = new Dictionary<string, bool>();

        // Job number of files tracking
        public static Dictionary<string, int> NumberOfInputFilesFound = new Dictionary<string, int>();
        public static Dictionary<string, int> NumberOfInputFilesNeeded = new Dictionary<string, int>();
        public static Dictionary<string, int> NumberOfProcessingFilesFound = new Dictionary<string, int>();
        public static Dictionary<string, int> NumberOfProcessingFilesNeeded = new Dictionary<string, int>();

        // Modeler process handle list
        public static Dictionary<string, Process> ProcessHandles = new Dictionary<string, Process>();

        // Common objects
        internal static LoggingToFile FileLoggerObject;
        internal static Logger<IStatusRepository> Logger;
        internal static SynchronizedCache InputJobsToRun = new SynchronizedCache();
        internal static CsvFileHandler CsvFileHandlerHandle = new CsvFileHandler();
        internal static StatusEntry StatusEntryHandle = new StatusEntry();
        internal static List<StatusData> StatusDataList = new List<StatusData>();
        internal static IniFileData IniData = new IniFileData();

        /// <summary>
        /// Global log to file method
        /// </summary>
        /// <param name="msg"></param>
        public static void Log(string msg)
        {
            FileLoggerObject.WriteToLogFile(msg);
            Console.WriteLine(msg);
        }

        /// <summary>
        /// Status Data Entry Method
        /// </summary>
        /// <param name="job"></param>
        /// <param name="status"></param>
        /// <param name="timeSlot"></param>
        public static void StatusDataEntry(string job, JobStatus status, JobType timeSlot)
        {
            // Write to the Status accumulator
            StatusEntryHandle.ListStatus(job, status, timeSlot);

            // Write new status to the log file
            CsvFileHandlerHandle.WriteToCsvFile(job, status, timeSlot);
        }

        /// <summary>
        /// Get total number of Jobs in the Input Buffer Job list
        /// </summary>
        public static int GetTotalNumberOfJobs()
        {
            int jobCount = 0;
            Task AddTask = Task.Run(() =>
            {
                jobCount = InputJobsToRun.Count;
            });

            TimeSpan timeSpan = TimeSpan.FromMilliseconds(GET_TOTAL_NUM_OF_JOBS_DELAY);
            if (!AddTask.Wait(timeSpan))
            {
                Logger.LogError("InputJobScanThread get total number of Jobs timed out at {0} msec at {1:HH:mm:ss.fff}",
                    GET_TOTAL_NUM_OF_JOBS_DELAY, DateTime.Now);
            }

            return jobCount;
        }

        /// <summary>
        /// Add Job to Input Buffer Job List 
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        public static void AddJobToList(string job)
        {
            int jobIndex = 1;
            Task AddTask = Task.Run(() =>
            {
                try
                {
                    jobIndex = FindLastIndex() + 1;
                    InputJobsToRun.Add(jobIndex, job);
                    Log(string.Format("Input Jobs Scan added new Job {0} to Input Job List index {1} at {2:HH:mm:ss.fff}",
                        job, jobIndex, DateTime.Now));
                }
                catch (ArgumentException)
                {
                    jobIndex++;
                    Logger.LogWarning("Add Job to list retry to add {0} to Input Job List index {1} at {2:HH:mm:ss.fff}",
                        job, jobIndex, DateTime.Now);
                    InputJobsToRun.Add(jobIndex, job);
                }
            });

            TimeSpan timeSpan = TimeSpan.FromMilliseconds(ADD_JOB_DELAY);
            if (!AddTask.Wait(timeSpan))
            {
                Logger.LogError("InputJobScanThread Add Job {0} timed out at {1} msec at {2:HH:mm:ss.fff}",
                    job, ADD_JOB_DELAY, DateTime.Now);
            }
        }

        /// <summary>
        /// Get Job from the Input Buffer List
        /// </summary>
        /// <param name="jobIndex"></param>
        /// <returns>Job string</returns>
        public static string GetJobFromList(int jobIndex)
        {
            string job = string.Empty;
            int JobIndex = jobIndex;
            Task ReadJobTask = Task.Run(() =>
            {
                if (InputJobsToRun.Count > 0)
                {
                    try
                    {
                        job = InputJobsToRun.Read(jobIndex);
                        Log(string.Format("\nGot next Job {0} from Input Job list index {1} at {2:HH:mm:ss.fff}",
                            job, jobIndex, DateTime.Now));
                    }
                    catch (KeyNotFoundException)
                    {
                        CurrentJobIndex = 1;
                        Logger.LogWarning("Read Job from invalid list index {0} failed resetting Current Index to 1 at {1:HH:mm:ss.fff}",
                            jobIndex, DateTime.Now);
                    }
                }
            });

            TimeSpan readJobtimeSpan = TimeSpan.FromMilliseconds(READ_JOB_DELAY);
            if (!ReadJobTask.Wait(readJobtimeSpan))
            {
                Logger.LogError("InputJobScanThread Read Job {0} timed out adding as index {1} at {2} msec at {2:HH:mm:ss.fff}",
                    job, jobIndex, READ_JOB_DELAY, DateTime.Now);
            }

            return job;
        }

        /// <summary>
        /// Delete Job from Input Buffer Job List
        /// </summary>
        /// <param name="jobIndex"></param>
        public static void DeleteJobFromList(int jobIndex)
        {
            string job = InputJobsToRun.Read(jobIndex);
            Task deleteJobTask = Task.Run(() =>
            {
                // Delete job being run next from the Input Jobs List
                InputJobsToRun.Delete(jobIndex);
                Log(string.Format("Deleted Job {0} from Input Job list index {1} at {2:HH:mm:ss.fff}",
                    job, jobIndex, DateTime.Now));

                // If there are more jobs in the list, increment current Job index
                if (CurrentJobIndex < FindLastIndex())
                {
                    CurrentJobIndex++;
                    Log(string.Format("Incremented the Current Job index to {0} at {1:HH:mm:ss.fff}",
                        CurrentJobIndex, DateTime.Now));
                }
            });

            TimeSpan deleteTimeSpan = TimeSpan.FromMilliseconds(DELETE_JOB_DELAY);
            if (!deleteJobTask.Wait(deleteTimeSpan))
            {
                Logger.LogError("InputJobScanThread Delete Job {0} index {1} timed out at {2} msec at {3:HH:mm:ss.fff}",
                    job, jobIndex, DELETE_JOB_DELAY, DateTime.Now);
            }
        }

        /// <summary>
        /// Display the Input Buffer Job List
        /// </summary>
        public static void DisplayJobList()
        {
            Dictionary<int, string> jobList = new Dictionary<int, string>();
            string job;
            int index;
            Task AddTask = Task.Run(() =>
            {
                int lastIndex = CurrentJobIndex + InputJobsToRun.Count;
                for (index = CurrentJobIndex; index <= lastIndex; index++)
                {
                    try
                    {
                        job = InputJobsToRun.Read(index);
                        jobList.Add(index, job);
                    }
                    catch (KeyNotFoundException)
                    {
                    }
                }
            });

            TimeSpan timeSpan = TimeSpan.FromMilliseconds(GET_TOTAL_NUM_OF_JOBS_DELAY);
            if (!AddTask.Wait(timeSpan))
            {
                Logger.LogError("InputJobScanThread get total number of Jobs timed out at {0} msec at {1:HH:mm:ss.fff}",
                    GET_TOTAL_NUM_OF_JOBS_DELAY, DateTime.Now);
            }

            Log("\nCurrent Input Buffer Job List:");
            if (jobList.Count > 0)
            {
                foreach (var entry in jobList)
                {
                    Log(string.Format("{0}", entry));
                }
            }
            else
            {
                Log("Empty List");
            }
            Log("");
        }

        /// <summary>
        /// Find the last index in the Input Buffer Job list from inside a task only
        /// </summary>
        public static int FindLastIndex()
        {
            int index;
            int lastIndex = 0;
            for (index = 1; index < InputJobsToRun.Count; index++)
            {
                try
                {
                    string job = InputJobsToRun.Read(index);
                    if (job != string.Empty)
                    {
                        lastIndex = index;
                    }
                }
                catch (KeyNotFoundException)
                {
                }
            }

            if ((StaticClass.IniData.DebugMode & (byte)DebugModeState.JOB_LIST) != 0)
            {
                Log(string.Format("Current Index = {0} Last Index = {1} at {2:HH:mm:ss.fff}",
                    CurrentJobIndex, lastIndex, DateTime.Now));
            }

            return lastIndex;
        }

        /// <summary>
        /// Check if a Job directory is complete
        /// </summary>
        /// <param name="directory"></param>
        /// <returns>Job Files Complete or not</returns>
        public static bool CheckIfJobFilesComplete(string directory)
        {
            // Check if the Job Xml file exists
            string[] files = Directory.GetFiles(directory, "*.xml");
            if (files.Length > 0)
            {
                JobXmlData jobXmlData = GetJobXmlFileInfo(directory, DirectoryScanType.INPUT_BUFFER);
                string jobXmlFileName = jobXmlData.JobDirectory + @"\" + jobXmlData.XmlFileName;
                if (CheckFileReady(jobXmlFileName))
                {
                    // Read Job xml file and get the top node
                    XmlDocument jobXmlDoc = new XmlDocument();
                    jobXmlDoc.Load(jobXmlFileName);
                    XmlElement root = jobXmlDoc.DocumentElement;
                    string TopNode = root.LocalName;

                    // Get nodes for the number of files and names of files to transfer from Job .xml file
                    XmlNode ConsumedNode = jobXmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/FileConfiguration/Consumed");
                    int numberOfFilesNeeded = Convert.ToInt32(ConsumedNode.InnerText);

                    // Get the current number of files
                    DirectoryInfo InputJobInfo = new DirectoryInfo(directory);
                    if (InputJobInfo == null)
                    {
                        Logger.LogError("InputJobInfo failed to instantiate");
                    }
                    int numberOfFilesFound = InputJobInfo.GetFiles().Length;

                    // Return Job file start set complete or not
                    return (numberOfFilesNeeded == numberOfFilesFound);
                }
            }

            return false;
        }

        /// <summary>
        /// Get the Job XML data 
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="scanType"></param>
        /// <returns>Job Xml data</returns>
        public static JobXmlData GetJobXmlFileInfo(string directory, DirectoryScanType scanType)
        {
            JobXmlData jobScanXmlData = new JobXmlData();
            string baseDirectory = (scanType == DirectoryScanType.INPUT_BUFFER) ? IniData.InputDir : IniData.ProcessingDir;
            string job = directory.Replace(baseDirectory, "").Remove(0, 1);
            jobScanXmlData.Job = job;
            jobScanXmlData.JobDirectory = directory;
            jobScanXmlData.JobSerialNumber = job.Substring(0, job.IndexOf("_"));
            int start = job.IndexOf("_") + 1;
            jobScanXmlData.TimeStamp = job.Substring(start, job.Length - start);

            // Wait until the Xml file shows up
            bool xmlFileFound = false;
            do
            {
                string[] files = Directory.GetFiles(directory, "*.xml");
                if (files.Length > 0)
                {
                    jobScanXmlData.XmlFileName = Path.GetFileName(files[0]);
                    xmlFileFound = true;
                }

                Thread.Yield();
            }
            while (xmlFileFound == false);

            return jobScanXmlData;
        }

        /// <summary>
        /// Get the Status Monitor data using the Job xml file data
        /// </summary>
        /// <param name="jobXmlData"></param>
        /// <returns>Status Monitor Data</returns>
        public static StatusMonitorData GetJobMonitorData(JobXmlData jobXmlData)
        {
            // Create the Job Run common strings
            string job = jobXmlData.Job;
            string xmlJobDirectory = jobXmlData.JobDirectory;

            // Create new status monitor data and fill it in with the job xml data
            StatusMonitorData monitorData = new StatusMonitorData
            {
                Job = job,
                JobDirectory = xmlJobDirectory,
                StartTime = DateTime.Now,
                JobSerialNumber = jobXmlData.JobSerialNumber,
                TimeStamp = jobXmlData.TimeStamp,
                XmlFileName = jobXmlData.XmlFileName
            };

            // Check if Job xml file is ready
            string jobXmlFileName = jobXmlData.JobDirectory + @"\" + jobXmlData.XmlFileName;
            if (CheckFileReady(jobXmlFileName))
            {
                // Read Job xml file and get the top node
                XmlDocument jobXmlDoc = new XmlDocument();
                jobXmlDoc.Load(jobXmlFileName);

                XmlElement root = jobXmlDoc.DocumentElement;
                string TopNode = root.LocalName;

                // Get nodes for the number of files and names of files to transfer from Job .xml file
                XmlNode UnitNumberdNode = jobXmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/listitem/value");
                XmlNode ConsumedNode = jobXmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/FileConfiguration/Consumed");
                XmlNode ProducedNode = jobXmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/FileConfiguration/Produced");
                XmlNode TransferedNode = jobXmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/FileConfiguration/Transfered");
                XmlNode ModelerNode = jobXmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/FileConfiguration/Modeler");

                // Assign then increment port number for this Job
                monitorData.JobPortNumber = IniData.StartPort + JobPortIndex++;

                // Get the modeler and number of files to transfer
                int NumFilesToTransfer = 0;
                monitorData.UnitNumber = UnitNumberdNode.InnerText;
                monitorData.Modeler = ModelerNode.InnerText;
                monitorData.NumFilesConsumed = Convert.ToInt32(ConsumedNode.InnerText);
                monitorData.NumFilesProduced = Convert.ToInt32(ProducedNode.InnerText);
                if (TransferedNode != null)
                {
                    NumFilesToTransfer = Convert.ToInt32(TransferedNode.InnerText);
                }
                monitorData.NumFilesToTransfer = NumFilesToTransfer;

                // Get the modeler and number of files to transfer
                Log($"Unit Number                    : {monitorData.UnitNumber}");
                Log($"Modeler                        : {monitorData.Modeler}");
                Log($"Num Files Consumed             : {monitorData.NumFilesConsumed}");
                Log($"Num Files Produced             : {monitorData.NumFilesProduced}");
                Log($"Num Files To Transfer          : {monitorData.NumFilesToTransfer}");
                Log($"Job Port Number                : {monitorData.JobPortNumber}");

                // Create the Transfered file list from the Xml file entries
                monitorData.TransferedFileList = new List<string>(NumFilesToTransfer);
                for (int i = 1; i < NumFilesToTransfer + 1; i++)
                {
                    string transferFileNodeName = ("/" + TopNode + "/FileConfiguration/Transfered" + i.ToString());
                    XmlNode TransferedFileXml = jobXmlDoc.DocumentElement.SelectSingleNode(transferFileNodeName);
                    monitorData.TransferedFileList.Add(TransferedFileXml.InnerText);
                    Log(string.Format("Transfer File{0}                 : {1}", i, TransferedFileXml.InnerText));
                }
            }
            else
            {
                Logger.LogError("File {0} is not available at {1:HH:mm:ss.fff}\n", jobXmlFileName, DateTime.Now);
            }

            return monitorData;
        }

        /// <summary>
        /// Returns when a file is ready to access
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>Returns if file is ready to access</returns>
        public static bool CheckFileReady(string fileName)
        {
            int numOfRetries = 0;
            do
            {
                try
                {
                    // Check that a file is both readable and writeable
                    using (FileStream fileService = File.Open(fileName, FileMode.Open))
                    {
                        if (fileService.CanRead && fileService.CanWrite)
                        {
                            if ((StaticClass.IniData.DebugMode & (byte)DebugModeState.FILE) != 0)
                            {
                                Log(string.Format("File {0} ready at {1:HH:mm:ss.fff}", fileName, DateTime.Now));
                            }

                            return true;
                        }
                    }
                }
                catch (IOException)
                {
                    if ((StaticClass.IniData.DebugMode & (byte)DebugModeState.FILE) != 0)
                    {
                        Log(string.Format("File {0} check {1} shows not ready at {2:HH:mm:ss.fff}", fileName, numOfRetries, DateTime.Now));
                    }

                     Thread.Sleep(FILE_READY_WAIT);
                }

                // Check for shutdown or pause
                if (ShutDownPauseCheck("IsFileReady") == true)
                {
                    return false;
                }

                Thread.Yield();
            }
            while (numOfRetries++ < NUM_XML_ACCESS_RETRIES);

            return false;
        }

        /// <summary>
        /// Returns when a file is ready to access
        /// </summary>
        /// <param name="directory"></param>
        /// <returns>Returns if file is ready to access</returns>
        public static bool CheckDirectoryReady(string directory)
        {
            // Get directory info
            DirectoryInfo dirInfo = new DirectoryInfo(directory);
            try
            {
                // If GetDirectories works then it is accessible
                DirectoryInfo[] dirs = dirInfo.GetDirectories();
                if (dirs != null)
                {
                    Log(string.Format("Directory {0} accessibility check passed at {1:HH:mm:ss.fff}",
                        directory, DateTime.Now));

                    return true;
                }
            }
            catch (UnauthorizedAccessException e)
            {
                Logger.LogError("Directory {0} accessibility check failed with {1} at {2:HH:mm:ss.fff}",
                    directory, e, DateTime.Now);
            }

            return false;
        }

        /// <summary>
        /// Check the Input Buffer for directories that are older than the time limit
        /// </summary>
        public static void CheckForInputBufferTimeLimits()
        {
            string[] directories = Directory.GetDirectories(IniData.InputDir);
            foreach (string dir in directories)
            {
                // Get the current directory list and delete the ones beyond the time limit
                DirectoryInfo dirInfo = new DirectoryInfo(dir);
                if (dirInfo.LastWriteTime < DateTime.Now.AddDays(-IniData.InputBufferTimeLimit))
                {
                    FileHandling.DeleteDirectory(dir);
                }
            }
        }

        /// <summary>
        /// Shut Down and Pause Check
        /// </summary>
        /// <param name="location"></param>
        /// <returns>Shutdown or not</returns>
        public static bool ShutDownPauseCheck(string location)
        {
            // Output message of the shutdown flag is set
            if (ShutdownFlag == true)
            {
                Log(string.Format("\nShutdown {0} at {1:HH:mm:ss.fff}", location, DateTime.Now));

                // Shutdown confirmed
                return true;
            }

            // Check if the pause flag is set, then wait for reset
            if (PauseFlag == true)
            {
                Log(string.Format("Handler in Pause mode in {0} at {1:HH:mm:ss.fff}", location, DateTime.Now));
                do
                {
                    Thread.Yield();
                }
                while (PauseFlag == true);
            }

            // No shutdown
            return false;
        }
    }
}