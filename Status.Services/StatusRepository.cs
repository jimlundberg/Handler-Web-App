using StatusModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

/// <summary>
/// Status data services
/// </summary>
namespace Status.Services
{
    /// <summary>
    /// Status Data storage object
    /// </summary>
    public class StatusRepository : IStatusRepository
    {
        private ProcessThread processThread;
        private IniFileData iniFileData = new IniFileData();
        private List<StatusMonitorData> monitorData = new List<StatusMonitorData>();
        private List<StatusWrapper.StatusData> statusList = new List<StatusWrapper.StatusData>();
        private StatusWrapper.StatusData statusData = new StatusWrapper.StatusData();
        public int GlobalJobIndex = 0;
        private bool RunStop = true;

        /// <summary>
        /// Scan for Unfinished jobs in the Processing Buffer
        /// </summary>
        public void ScanForUnfinishedJobs()
        {
            StatusModels.JobXmlData jobXmlData = new StatusModels.JobXmlData();
            DirectoryInfo directory = new DirectoryInfo(iniFileData.ProcessingDir);
            DirectoryInfo[] subdirs = directory.GetDirectories();
            if ((subdirs.Length != 0) && (Counters.NumberOfJobsExecuting < iniFileData.ExecutionLimit))
            {
                Console.WriteLine("\nFound unfinished jobs...");
                for (int i = 0; i < subdirs.Length; i++)
                {
                    String job = subdirs[i].Name;

                    // Delete the data.xml file if present
                    String dataXmlFile = iniFileData.ProcessingDir + @"\" + job + @"\" + "data.xml";
                    if (File.Exists(dataXmlFile))
                    {
                        File.Delete(dataXmlFile);
                    }

                    // Start scan for job files in the Output Buffer
                    ScanDirectory scanDir = new ScanDirectory(iniFileData.ProcessingDir);
                    jobXmlData = scanDir.GetJobXmlData(iniFileData.ProcessingDir + @"\" + job);

                    // Get data found in Xml file into Monitor Data
                    StatusModels.StatusMonitorData data = new StatusModels.StatusMonitorData();
                    data.Job = jobXmlData.Job;
                    data.JobDirectory = jobXmlData.JobDirectory;
                    data.JobSerialNumber = jobXmlData.JobSerialNumber;
                    data.TimeStamp = jobXmlData.TimeStamp;
                    data.XmlFileName = jobXmlData.XmlFileName;
                    data.JobIndex = GlobalJobIndex++;

                    // Display Monitor Data found
                    Console.WriteLine("");
                    Console.WriteLine("Found unfinished Job  = " + data.Job);
                    Console.WriteLine("New Job Directory     = " + data.JobDirectory);
                    Console.WriteLine("New Serial Number     = " + data.JobSerialNumber);
                    Console.WriteLine("New Time Stamp        = " + data.TimeStamp);
                    Console.WriteLine("New Job Xml File      = " + data.XmlFileName);

                    if (Counters.NumberOfJobsExecuting < iniFileData.ExecutionLimit)
                    {
                        // Increment counts to track job execution and port id
                        Counters.IncrementNumberOfJobsExecuting();
                        data.ExecutionCount++;

                        Console.WriteLine("+++++Job {0} Executing {1}", data.Job, Counters.NumberOfJobsExecuting);

                        JobRunThread jobThread = new JobRunThread(iniFileData.ProcessingDir, iniFileData, data, statusList);

                        // Create a thread to execute the task, and then start the thread.
                        Thread t = new Thread(new ThreadStart(jobThread.ThreadProc));
                        Console.WriteLine("Starting Job " + data.Job);
                        t.Start();
                        Thread.Sleep(30000);
                    }
                    else
                    {
                        Console.WriteLine("Job {0} Executing {1} Exceeded Execution Limit of {2}",
                            data.Job, Counters.NumberOfJobsExecuting, iniFileData.ExecutionLimit);
                        Thread.Sleep(iniFileData.ScanTime);
                    }

                    // If Stop button pressed, set RunStop Flag to false to stop
                    if (RunStop == false)
                    {
                        return;
                    }
                }
            }
            else
            {
                Console.WriteLine("\nNo Unfinished Jobs found");
            }
        }

        /// <summary>
        /// Class to run the whole monitoring process as a thread
        /// </summary>
        public class ProcessThread
        {
            // State information used in the task.
            private IniFileData IniData;
            private List<StatusWrapper.StatusData> StatusData;
            public volatile bool endProcess = false;
            private int GlobalJobIndex = 0;

            // The constructor obtains the state information.
            /// <summary>
            /// Process Thread constructor receiving data buffers
            /// </summary>
            /// <param name="iniData"></param>
            /// <param name="statusData"></param>
            /// <param name="globalJobIndex"></param>
            /// <param name="numberOfJobsRunning"></param>
            public ProcessThread(IniFileData iniData, List<StatusWrapper.StatusData> statusData, int globalJobIndex)
            {
                IniData = iniData;
                StatusData = statusData;
                GlobalJobIndex = globalJobIndex;
            }

            /// <summary>
            /// Method to set flag to stop the monitoring process
            /// </summary>
            public void StopProcess()
            {
                endProcess = true;
            }

            /// <summary>
            /// Method to scan for new jobs in the Input Buffer
            /// </summary>
            public void ScanForNewJobs()
            {
                endProcess = false;
                StatusModels.JobXmlData jobXmlData = new StatusModels.JobXmlData();
                DirectoryInfo directory = new DirectoryInfo(IniData.InputDir);
                List<String> directoryList = new List<String>();

                Console.WriteLine("\nWaiting for new job(s)...\n");

                while (endProcess == false) // Loop until flag set
                {
                    // Check if there are any directories
                    DirectoryInfo[] subdirs = directory.GetDirectories();
                    if (subdirs.Length != 0)
                    {
                        for (int i = 0; i < subdirs.Length; i++)
                        {
                            String job = subdirs[i].Name;

                            // Start scan for new directory in the Input Buffer
                            ScanDirectory scanDir = new ScanDirectory(IniData.InputDir);
                            jobXmlData = scanDir.GetJobXmlData(IniData.InputDir + @"\" + job);

                            // Set data found
                            StatusModels.StatusMonitorData data = new StatusModels.StatusMonitorData();
                            data.Job = jobXmlData.Job;
                            data.JobDirectory = jobXmlData.JobDirectory;
                            data.JobSerialNumber = jobXmlData.JobSerialNumber;
                            data.TimeStamp = jobXmlData.TimeStamp;
                            data.XmlFileName = jobXmlData.XmlFileName;
                            data.JobIndex = GlobalJobIndex++;

                            // Display data found
                            Console.WriteLine("");
                            Console.WriteLine("Found new Job         = " + data.Job);
                            Console.WriteLine("New Job Directory     = " + data.JobDirectory);
                            Console.WriteLine("New Serial Number     = " + data.JobSerialNumber);
                            Console.WriteLine("New Time Stamp        = " + data.TimeStamp);
                            Console.WriteLine("New Job Xml File      = " + data.XmlFileName);

                            if (Counters.NumberOfJobsExecuting <= IniData.ExecutionLimit)
                            {
                                // Increment counters to track job execution and port id
                                Counters.IncrementNumberOfJobsExecuting();
                                data.ExecutionCount++;

                                Console.WriteLine("+++++Job {0} Executing slot {1}", data.Job, Counters.NumberOfJobsExecuting);

                                // Supply the state information required by the task.
                                JobRunThread jobThread = new JobRunThread(IniData.InputDir, IniData, data, StatusData);

                                // Create a thread to execute the task, and then start the thread.
                                Thread t = new Thread(new ThreadStart(jobThread.ThreadProc));
                                Console.WriteLine("Starting Job " + data.Job);
                                t.Start();
                                Thread.Sleep(30000);
                            }
                            else
                            {
                                i--; // Retry job
                                Console.WriteLine("Job {0} job count {1} trying to exceeded Execution Limit of {2}",
                                    data.Job, Counters.NumberOfJobsExecuting, IniData.ExecutionLimit);
                                Thread.Sleep(IniData.ScanTime);
                            }
                        }
                    }

                    // Sleep to allow job to finish before checking for more
                    Thread.Sleep(IniData.ScanTime);
                }
                Console.WriteLine("\nExiting job Scan...");
            }
        }

        /// <summary>
        /// Get the Monitor Status Entry point
        /// </summary>
        /// <returns></returns>
        public void GetMonitorStatus()
        {
            RunStop = true;
            GlobalJobIndex = 0;

            // Scan for jobs not completed
            ScanForUnfinishedJobs();

            // Start scan for new jobs on it's own thread
            processThread = new ProcessThread(iniFileData, statusList, GlobalJobIndex);
            processThread.ScanForNewJobs();
        }

        /// <summary>
        /// Get the Monitor Status Entry point
        /// </summary>
        /// <returns></returns>
        public void GetIniFileData()
        {
            // Check that Config.ini file exists
            String IniFileName = "Config.ini";
            if (File.Exists(IniFileName) == false)
            {
                throw new System.InvalidOperationException("Config.ini file does not exist in the Handler directory");
            }

            // Get information from the Config.ini file
            var IniParser = new IniFileHandler(IniFileName);
            iniFileData.IniFileName = IniFileName;
            iniFileData.InputDir = IniParser.Read("Paths", "Input");
            iniFileData.ProcessingDir = IniParser.Read("Paths", "Processing");
            iniFileData.RepositoryDir = IniParser.Read("Paths", "Repository");
            iniFileData.FinishedDir = IniParser.Read("Paths", "Finished");
            iniFileData.ErrorDir = IniParser.Read("Paths", "Error");
            iniFileData.ModelerRootDir = IniParser.Read("Paths", "ModelerRootDir");
            iniFileData.CPUCores = Int32.Parse(IniParser.Read("Process", "CPUCores"));
            iniFileData.ExecutionLimit = Int32.Parse(IniParser.Read("Process", "ExecutionLimit"));
            iniFileData.StartPort = Int32.Parse(IniParser.Read("Process", "StartPort"));
            iniFileData.LogFile = IniParser.Read("Process", "LogFile");
            iniFileData.ScanTime = Int32.Parse(IniParser.Read("Process", "ScanTime"));
            String timeLimitString = IniParser.Read("Process", "MaxTimeLimit");
            iniFileData.MaxTimeLimit = Int32.Parse(timeLimitString.Substring(0, timeLimitString.IndexOf("#")));
            String logFileHistory = IniParser.Read("Process", "LogFileHistory");
            iniFileData.LogFileHistory = Int32.Parse(logFileHistory.Substring(0, logFileHistory.IndexOf("#")));

            Console.WriteLine("\nConfig.ini data found:");
            Console.WriteLine("Input Dir             = " + iniFileData.InputDir);
            Console.WriteLine("Processing Dir        = " + iniFileData.ProcessingDir);
            Console.WriteLine("Repository Dir        = " + iniFileData.RepositoryDir);
            Console.WriteLine("Finished Dir          = " + iniFileData.FinishedDir);
            Console.WriteLine("Error Dir             = " + iniFileData.ErrorDir);
            Console.WriteLine("Modeler Root Dir      = " + iniFileData.ModelerRootDir);
            Console.WriteLine("Log File              = " + iniFileData.LogFile);
            Console.WriteLine("CPU Cores             = " + iniFileData.CPUCores + " Cores");
            Console.WriteLine("Execution Limit       = " + iniFileData.ExecutionLimit + " Jobs");
            Console.WriteLine("Start Port            = " + iniFileData.StartPort);
            Console.WriteLine("Scan Time             = " + iniFileData.ScanTime + " Miliseconds");
            Console.WriteLine("Max Time Limit        = " + iniFileData.MaxTimeLimit + " Seconds");
            Console.WriteLine("Log File History      = " + iniFileData.LogFileHistory + " Days");
        }

        /// <summary>
        /// Method to Check the History of the log file
        /// </summary>
        public void CheckLogFileHistory()
        {
            StatusEntry status = new StatusEntry();
            status.CheckLogFileHistory(iniFileData.LogFile, iniFileData.LogFileHistory);
        }

        /// <summary>
        /// Method to stop the Monitor process
        /// </summary>
        public void StopMonitor()
        {
            if (processThread != null)
            {
                processThread.StopProcess();
            }
        }

        /// <summary>
        /// Method to return the status data to the requestor
        /// </summary>
        /// <returns></returns>
        public IEnumerable<StatusWrapper.StatusData> GetJobStatus()
        {
            return statusList;
        }

        /// <summary>
        /// Get csV history data
        /// </summary>
        /// <returns></returns>
        public IEnumerable<StatusWrapper.StatusData> GetHistoryData()
        {
            List<StatusWrapper.StatusData> statusList = new List<StatusWrapper.StatusData>();
            StatusEntry status = new StatusEntry();
            statusList = status.ReadFromCsvFile(iniFileData.LogFile);
            return statusList;
        }
    }
}
