using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.Extensions.Logging;
using StatusModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private NewJobsScanThread newJobsScanThread;
        private IniFileData iniFileData = new IniFileData();
        private List<StatusMonitorData> monitorData = new List<StatusMonitorData>();
        private List<StatusWrapper.StatusData> statusList = new List<StatusWrapper.StatusData>();
        private StatusWrapper.StatusData statusData = new StatusWrapper.StatusData();
        public readonly ILogger<StatusRepository> logger;

        public StatusRepository(ILogger<StatusRepository> _logger)
        {
            logger = (ILogger<StatusRepository>)_logger;
        }

        /// <summary>
        /// Get the Monitor Status Entry point
        /// </summary>
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
        public void StartMonitorProcess()
        {
            StaticData.ShutdownFlag = false;

            // Start thread to scan for old then new jobs
            newJobsScanThread = new NewJobsScanThread(iniFileData, statusList);
            newJobsScanThread.ThreadProc();
        }

        /// <summary>
        /// Method to return the status data to the requestor
        /// </summary>
        /// <returns>Status Data List</returns>
        public IEnumerable<StatusWrapper.StatusData> GetJobStatus()
        {
            return statusList;
        }

        /// <summary>
        /// Get csV history data
        /// </summary>
        /// <returns>History Status Data List</returns>
        public IEnumerable<StatusWrapper.StatusData> GetHistoryData()
        {
            List<StatusWrapper.StatusData> statusList = new List<StatusWrapper.StatusData>();
            StatusEntry status = new StatusEntry();
            statusList = status.ReadFromCsvFile(iniFileData.LogFile);
            return statusList;
        }
    }
}
