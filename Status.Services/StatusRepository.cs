﻿using Microsoft.Extensions.Logging;
using StatusModels;
using System.Collections.Generic;
using System.Diagnostics;
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
        private CurrentInputJobsScanThread newJobsScanThread;
        public IniFileData IniData = new IniFileData();
        public List<StatusData> StatusDataList = new List<StatusData>();
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
            IniData.IniFileName = IniFileName;
            IniData.InputDir = IniParser.Read("Paths", "Input");
            IniData.ProcessingDir = IniParser.Read("Paths", "Processing");
            IniData.RepositoryDir = IniParser.Read("Paths", "Repository");
            IniData.FinishedDir = IniParser.Read("Paths", "Finished");
            IniData.ErrorDir = IniParser.Read("Paths", "Error");
            IniData.ModelerRootDir = IniParser.Read("Paths", "ModelerRootDir");
            IniData.CPUCores = int.Parse(IniParser.Read("Process", "CPUCores"));
            IniData.ExecutionLimit = int.Parse(IniParser.Read("Process", "ExecutionLimit"));
            IniData.StartPort = int.Parse(IniParser.Read("Process", "StartPort"));
            IniData.StatusLogFile = IniParser.Read("Process", "StatusLogFile");
            IniData.ProcessLogFile = IniParser.Read("Process", "ProcessLogFile");
            string scanTime = IniParser.Read("Process", "ScanTime");
            IniData.ScanTime = int.Parse(scanTime.Substring(0, scanTime.IndexOf("#")));
            string timeLimitString = IniParser.Read("Process", "MaxTimeLimit");
            IniData.MaxTimeLimit = int.Parse(timeLimitString.Substring(0, timeLimitString.IndexOf("#")));
            string logFileHistory = IniParser.Read("Process", "LogFileHistory");
            IniData.LogFileHistory = int.Parse(logFileHistory.Substring(0, logFileHistory.IndexOf("#")));
            string logFileMaxSize = IniParser.Read("Process", "logFileMaxSize");
            IniData.LogFileMaxSize = int.Parse(logFileMaxSize.Substring(0, logFileMaxSize.IndexOf("#")));

            // Set the log file max size
            StaticClass.logFileSizeLimit = IniData.LogFileMaxSize;

            string logFile = IniData.ProcessLogFile;
            StaticClass.Log(logFile, "\nConfig.ini data found:\n");
            StaticClass.Log(logFile, "Input Dir             = " + IniData.InputDir);
            StaticClass.Log(logFile, "Processing Dir        = " + IniData.ProcessingDir);
            StaticClass.Log(logFile, "Repository Dir        = " + IniData.RepositoryDir);
            StaticClass.Log(logFile, "Finished Dir          = " + IniData.FinishedDir);
            StaticClass.Log(logFile, "Error Dir             = " + IniData.ErrorDir);
            StaticClass.Log(logFile, "Modeler Root Dir      = " + IniData.ModelerRootDir);
            StaticClass.Log(logFile, "Status Log File       = " + IniData.StatusLogFile);
            StaticClass.Log(logFile, "Process Log File      = " + IniData.ProcessLogFile);
            StaticClass.Log(logFile, "CPU Cores             = " + IniData.CPUCores + " Cores");
            StaticClass.Log(logFile, "Execution Limit       = " + IniData.ExecutionLimit + " Jobs");
            StaticClass.Log(logFile, "Start Port            = " + IniData.StartPort);
            StaticClass.Log(logFile, "Scan Time             = " + IniData.ScanTime + " Miliseconds");
            StaticClass.Log(logFile, "Max Time Limit        = " + IniData.MaxTimeLimit + " Seconds");
            StaticClass.Log(logFile, "Log File History      = " + IniData.LogFileHistory + " Days");
            StaticClass.Log(logFile, "Log File Max Size     = " + IniData.LogFileMaxSize + " MegaBytes");
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
            status.CheckLogFileHistory(IniData.StatusLogFile, IniData.LogFileHistory, Logger);
        }

        /// <summary>
        /// Get the Monitor Status Entry point
        /// </summary>
        public void StartMonitorProcess()
        {
            StaticClass.ShutdownFlag = false;

            // Start thread to scan for old then new jobs
            newJobsScanThread = new CurrentInputJobsScanThread(IniData, StatusDataList, Logger);
            if (newJobsScanThread == null)
            {
                Logger.LogError("StartMonitorProcess newJobsScanThread failed to instantiate");
            }
            newJobsScanThread.ThreadProc();
        }

        /// <summary>
        /// Method to stop the Monitor process
        /// </summary>
        public void StopMonitor()
        {
            // Exit Handler threads by setting shutdown flag
            StaticClass.ShutdownFlag = true;

            // Shutdown Modeler executables
            foreach (KeyValuePair<string, Process> process in StaticClass.ProcessHandles)
            {
                process.Value.Kill();
            }

            // Clear the Dictionary after shutdowns complete
            StaticClass.ProcessHandles.Clear();
        }

        /// <summary>
        /// Method to return the status data to the requestor
        /// </summary>
        /// <returns>Status Data List</returns>
        public IEnumerable<StatusData> GetJobStatus()
        {
            return StatusDataList;
        }

        /// <summary>
        /// Get CSV file history data
        /// </summary>
        /// <returns>History Status Data List</returns>
        public IEnumerable<StatusData> GetHistoryData()
        {
            List<StatusData> StatusDataList = new List<StatusData>();
            if (StatusDataList == null)
            {
                Logger.LogError("StatusRepository StatusList failed to instantiate");
            }

            StatusEntry status = new StatusEntry();
            if (status == null)
            {
                Logger.LogError("StatusRepository status failed to instantiate");
            }

            StatusDataList = status.ReadFromCsvFile(IniData.StatusLogFile, IniData, Logger);
            
            return StatusDataList;
        }
    }
}
