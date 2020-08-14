using Microsoft.Extensions.Logging;
using StatusModels;
using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Status data repository services
/// </summary>
namespace Status.Services
{
    /// <summary>
    /// Status Data storage object repository
    /// </summary>
    public class StatusRepository : IStatusRepository
    {
        private NewJobsScanThread newJobsScanThread;
        public IniFileData iniFileData = new IniFileData();
        public List<StatusMonitorData> monitorData = new List<StatusMonitorData>();
        public List<StatusData> statusList = new List<StatusData>();
        private StatusData statusData = new StatusData();
        public readonly ILogger<StatusRepository> Logger;

        /// <summary>
        /// StatusRepository contructor
        /// </summary>
        /// <param name="_logger"></param>
        public StatusRepository(ILogger<StatusRepository> logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Get the local Config.ini file data in the working directory
        /// </summary>
        public void GetIniFileData()
        {
            // Check that Config.ini file exists
            string IniFileName = "Config.ini";
            if (File.Exists(IniFileName) == false)
            {
                Logger.LogCritical("Missing Config.ini file");
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
            iniFileData.StatusLogFile = IniParser.Read("Process", "StatusLogFile");
            iniFileData.ProcessLogFile = IniParser.Read("Process", "ProcessLogFile");
            string scanTime = IniParser.Read("Process", "ScanTime");
            iniFileData.ScanTime = Int32.Parse(scanTime.Substring(0, scanTime.IndexOf("#")));
            string timeLimitString = IniParser.Read("Process", "MaxTimeLimit");
            iniFileData.MaxTimeLimit = Int32.Parse(timeLimitString.Substring(0, timeLimitString.IndexOf("#")));
            string logFileHistory = IniParser.Read("Process", "LogFileHistory");
            iniFileData.LogFileHistory = Int32.Parse(logFileHistory.Substring(0, logFileHistory.IndexOf("#")));
            string logFileMaxSize = IniParser.Read("Process", "logFileMaxSize");
            iniFileData.LogFileMaxSize = Int32.Parse(logFileMaxSize.Substring(0, logFileMaxSize.IndexOf("#")));

            // Set the log file max size
            StaticData.logFileSizeLimit = iniFileData.LogFileMaxSize;

            string logFile = iniFileData.ProcessLogFile;
            StaticData.Log(logFile, "\nConfig.ini data found:\n");
            StaticData.Log(logFile, "Input Dir             = " + iniFileData.InputDir);
            StaticData.Log(logFile, "Processing Dir        = " + iniFileData.ProcessingDir);
            StaticData.Log(logFile, "Repository Dir        = " + iniFileData.RepositoryDir);
            StaticData.Log(logFile, "Finished Dir          = " + iniFileData.FinishedDir);
            StaticData.Log(logFile, "Error Dir             = " + iniFileData.ErrorDir);
            StaticData.Log(logFile, "Modeler Root Dir      = " + iniFileData.ModelerRootDir);
            StaticData.Log(logFile, "Status Log File       = " + iniFileData.StatusLogFile);
            StaticData.Log(logFile, "Process Log File      = " + iniFileData.ProcessLogFile);
            StaticData.Log(logFile, "CPU Cores             = " + iniFileData.CPUCores + " Cores");
            StaticData.Log(logFile, "Execution Limit       = " + iniFileData.ExecutionLimit + " Jobs");
            StaticData.Log(logFile, "Start Port            = " + iniFileData.StartPort);
            StaticData.Log(logFile, "Scan Time             = " + iniFileData.ScanTime + " Miliseconds");
            StaticData.Log(logFile, "Max Time Limit        = " + iniFileData.MaxTimeLimit + " Seconds");
            StaticData.Log(logFile, "Log File History      = " + iniFileData.LogFileHistory + " Days");
            StaticData.Log(logFile, "Log File Max Size     = " + iniFileData.LogFileMaxSize + " MegaBytes");
        }

        /// <summary>
        /// Method to Check the History of the log file
        /// </summary>
        public void CheckLogFileHistory()
        {
            StatusEntry status = new StatusEntry();
            if (status == null)
            {
                Logger.LogError("Log File History status failed to instantiate");
            }
            status.CheckLogFileHistory(iniFileData.StatusLogFile, iniFileData.LogFileHistory, logger);
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
            newJobsScanThread = new NewJobsScanThread(iniFileData, statusList, logger);
            if (newJobsScanThread == null)
            {
                Logger.LogError("StartMonitorProcess newJobsScanThread failed to instantiate");
            }
            newJobsScanThread.ThreadProc();
        }

        /// <summary>
        /// Method to return the status data to the requestor
        /// </summary>
        /// <returns>Status Data List</returns>
        public IEnumerable<StatusData> GetJobStatus()
        {
            return statusList;
        }

        /// <summary>
        /// Get CSV file history data
        /// </summary>
        /// <returns>History Status Data List</returns>
        public IEnumerable<StatusData> GetHistoryData()
        {
            List<StatusData> statusList = new List<StatusData>();
            StatusEntry status = new StatusEntry();
            statusList = status.ReadFromCsvFile(iniFileData.StatusLogFile, iniFileData, logger);
            if (statusList == null)
            {
                Logger.LogError("GetHistoryData statusList failed to instantiate");
            }
            return statusList;
        }
    }
}
