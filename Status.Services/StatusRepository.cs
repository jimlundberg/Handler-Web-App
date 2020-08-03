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
        private JobScanThread jobScanThread;
        private IniFileData iniFileData = new IniFileData();
        private List<StatusMonitorData> monitorData = new List<StatusMonitorData>();
        private List<StatusWrapper.StatusData> statusList = new List<StatusWrapper.StatusData>();
        private StatusWrapper.StatusData statusData = new StatusWrapper.StatusData();

        /// <summary>
        /// Scan for Unfinished jobs in the Processing Buffer
        /// </summary>
        public void ScanForUnfinishedJobs()
        {
            StatusModels.JobXmlData jobXmlData = new StatusModels.JobXmlData();
            DirectoryInfo directory = new DirectoryInfo(iniFileData.ProcessingDir);
            DirectoryInfo[] subdirs = directory.GetDirectories();
            if (subdirs.Length != 0)
            {
                Console.WriteLine("\nFound unfinished jobs...");
                for (int i = 0; i < subdirs.Length; i++)
                {
                    if (StaticData.NumberOfJobsExecuting < iniFileData.ExecutionLimit)
                    {
                        // Increment counts to track job execution and port id
                        StaticData.IncrementNumberOfJobsExecuting();

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
                        data.JobIndex = StaticData.RunningJobsIndex++;

                        // Display Monitor Data found
                        Console.WriteLine("");
                        Console.WriteLine("Found unfinished Job  = " + data.Job);
                        Console.WriteLine("New Job Directory     = " + data.JobDirectory);
                        Console.WriteLine("New Serial Number     = " + data.JobSerialNumber);
                        Console.WriteLine("New Time Stamp        = " + data.TimeStamp);
                        Console.WriteLine("New Job Xml File      = " + data.XmlFileName);

                        Console.WriteLine("+++++Job {0} Executing slot {1}", data.Job, StaticData.NumberOfJobsExecuting);

                        // Create a thread to execute the task, and then start the thread.
                        JobRunThread jobThread = new JobRunThread(iniFileData.ProcessingDir, iniFileData, data, statusList);
                        Console.WriteLine("Starting Job " + data.Job);
                        jobThread.ThreadProc();

                        // If the shutdown flag is set, exit method
                        if (StaticData.ShutdownFlag == true)
                        {
                            Console.WriteLine("Shutdown ScanForUnfinishedJobs job {0} time {1:HH:mm:ss.fff}", data.Job, DateTime.Now);
                            return;
                        }

                        // Delay to let Modeler startup
                        Thread.Sleep(15000);
                    }
                    else
                    {
                        Thread.Sleep(iniFileData.ScanTime);
                    }
                }
            }
            else
            {
                Console.WriteLine("\nNo Unfinished Jobs found");
            }
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
            String scanTime = IniParser.Read("Process", "ScanTime");
            iniFileData.ScanTime = Int32.Parse(scanTime.Substring(0, scanTime.IndexOf("#")));
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
            // Exit threads by setting shutdown flag
            StaticData.ShutdownFlag = true;
        }

        /// <summary>
        /// Get the Monitor Status Entry point
        /// </summary>
        /// <returns></returns>
        public void GetMonitorStatus()
        {
            StaticData.ShutdownFlag = false;

            // Scan for jobs not completed
            ScanForUnfinishedJobs();

            // Start thread to scan for new jobs
            jobScanThread = new JobScanThread(iniFileData, statusList);
            jobScanThread.ThreadProc();
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
