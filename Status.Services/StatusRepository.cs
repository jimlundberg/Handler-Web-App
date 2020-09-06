using Microsoft.Extensions.Logging;
using Status.Models;
using System;
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
        private readonly IniFileData IniData = new IniFileData();
        private readonly List<StatusData> StatusDataList = new List<StatusData>();

        /// <summary>
        /// StatusRepository contructor
        /// </summary>
        public StatusRepository() { }

        /// <summary>
        /// Get the local Config.ini file data in the working directory
        /// </summary>
        public void GetIniFileData()
        {
            // Create the logger object handle straightaway
            StaticClass.Logger = new LoggerFactory().CreateLogger<StatusRepository>();

            // Check that Config.ini file exists
            string IniFileName = "Config.ini";
            if (File.Exists(IniFileName) == false)
            {
                StaticClass.Logger.LogCritical("Missing Config.ini file");
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
            
            string scanWaitTime = IniParser.Read("Process", "ScanWaitTime");
            IniData.ScanWaitTime = int.Parse(scanWaitTime.Substring(0, scanWaitTime.IndexOf("#")));
            
            string timeLimitString = IniParser.Read("Process", "MaxJobTimeLimit");
            IniData.MaxJobTimeLimit = double.Parse(timeLimitString.Substring(0, timeLimitString.IndexOf("#")));
            
            string logFileHistoryLimit = IniParser.Read("Process", "LogFileHistoryLimit");
            IniData.LogFileHistoryLimit = int.Parse(logFileHistoryLimit.Substring(0, logFileHistoryLimit.IndexOf("#")));
            
            string inputBufferTimeLimit = IniParser.Read("Process", "InputbufferTimeLimit");
            IniData.InputBufferTimeLimit = int.Parse(inputBufferTimeLimit.Substring(0, inputBufferTimeLimit.IndexOf("#")));
            
            string logFileMaxSize = IniParser.Read("Process", "logFileMaxSize");
            IniData.LogFileMaxSize = int.Parse(logFileMaxSize.Substring(0, logFileMaxSize.IndexOf("#")));

            // Set the static class data needed for global use
            StaticClass.ScanWaitTime = IniData.ScanWaitTime;
            StaticClass.LogFileSizeLimit = IniData.LogFileMaxSize;
            StaticClass.MaxJobTimeLimitSeconds = IniData.MaxJobTimeLimit * 60.0 * 60.0;

            // Set the file logging object handle only once here
            LoggingToFile loggingToFile = new LoggingToFile(IniData.ProcessLogFile);
            StaticClass.FileLoggerObject = loggingToFile;

            // Output the Data.ini informatino found
            StaticClass.Log($"\nConfig.ini data found:\n");
            StaticClass.Log($"Input Dir                      : {IniData.InputDir}");
            StaticClass.Log($"Processing Dir                 : {IniData.ProcessingDir}");
            StaticClass.Log($"Repository Dir                 : {IniData.RepositoryDir}");
            StaticClass.Log($"Finished Dir                   : {IniData.FinishedDir}");
            StaticClass.Log($"Error Dir                      : {IniData.ErrorDir}");
            StaticClass.Log($"Modeler Root Dir               : {IniData.ModelerRootDir}");
            StaticClass.Log($"Status Log File                : {IniData.StatusLogFile}");
            StaticClass.Log($"Process Log File               : {IniData.ProcessLogFile}");
            StaticClass.Log($"CPU Cores                      : {IniData.CPUCores} Cores");
            StaticClass.Log($"Execution Limit                : {IniData.ExecutionLimit} Jobs");
            StaticClass.Log($"Start Port                     : {IniData.StartPort}");
            StaticClass.Log($"Scan Wait Time                 : {IniData.ScanWaitTime} Miliseconds");
            StaticClass.Log($"Max Job Time Limit             : {IniData.MaxJobTimeLimit} Hours");
            StaticClass.Log($"Log File History Limit         : {IniData.LogFileHistoryLimit} Days");
            StaticClass.Log($"Log File Max Size              : {IniData.LogFileMaxSize} Megabytes");
        }

        /// <summary>
        /// Method to Check the History of the log file
        /// </summary>
        public void CheckLogFileHistory()
        {
            CsvFileHandler csvFileHandler = new CsvFileHandler();
            if (csvFileHandler == null)
            {
                StaticClass.Logger.LogError("StatusRepository csvFileHandler failed to instantiate");
            }
            csvFileHandler.CheckLogFileHistory(IniData);
        }

        /// <summary>
        /// Start the Monitor process by strting the new jobs scan thread
        /// </summary>
        public void StartMonitorProcess()
        {
            StaticClass.ShutdownFlag = false;

            // Check for pause state and reset it if the Start button is pressed when in Pause mode
            if (StaticClass.PauseFlag == true)
            {
                StaticClass.Log(String.Format("Taking the system out of Pause Mode at {0:HH:mm:ss.fff}", DateTime.Now));
                StaticClass.PauseFlag = false;
            }
            else
            {
                // Start Modeler job Processing thread
                InputJobsScanThread newJobsScanThread = new InputJobsScanThread(IniData, StatusDataList);
                if (newJobsScanThread == null)
                {
                    StaticClass.Logger.LogError("StartMonitorProcess newJobsScanThread failed to instantiate");
                }
                newJobsScanThread.ThreadProc();
            }
        }

        /// <summary>
        /// Pause the Monitor System
        /// </summary>
        public void PauseMonitor()
        {
            StaticClass.Log(String.Format("Putting the system into Pause Mode at {0:HH:mm:ss.fff}", DateTime.Now));
            StaticClass.PauseFlag = true;
        }

        /// <summary>
        /// Stop the Monitor process
        /// </summary>
        public void StopMonitor()
        {
            // Soft exit Handler threads by just setting volitale Shutdown flag
            StaticClass.ShutdownFlag = true;

            // Shutdown all active Modeler executables
            foreach (KeyValuePair<string, Process> process in StaticClass.ProcessHandles)
            {
                process.Value.Kill();
            }

            // Clear the Dictionaries after Modeler shutdowns complete
            StaticClass.ProcessHandles.Clear();
            StaticClass.InputJobsToRun.Clear();
            StaticClass.ProcessingJobsToRun.Clear();
            StaticClass.ProcessHandles.Clear();
            StaticClass.InputFileScanComplete.Clear();
            StaticClass.InputJobScanComplete.Clear();
            StaticClass.ProcessingFileScanComplete.Clear();
            StaticClass.ProcessingJobScanComplete.Clear();
            StaticClass.TcpIpScanComplete.Clear();
            StaticClass.NumberOfInputFilesFound.Clear();
            StaticClass.NumberOfInputFilesNeeded.Clear();
            StaticClass.NumberOfProcessingFilesFound.Clear();
            StaticClass.NumberOfProcessingFilesNeeded.Clear();
            StaticClass.NumberOfJobsExecuting = 0;
            StaticClass.JobPortIndex = 0;
        }

        /// <summary>
        /// Return the status data to the requestor
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
                StaticClass.Logger.LogError("StatusRepository StatusList failed to instantiate");
            }

            CsvFileHandler csvFileHandler = new CsvFileHandler();
            if (csvFileHandler == null)
            {
                StaticClass.Logger.LogError("StatusRepository csvFileHandler failed to instantiate");
            }
            StatusDataList = csvFileHandler.ReadFromCsvFile(IniData);
            
            return StatusDataList;
        }
    }
}
