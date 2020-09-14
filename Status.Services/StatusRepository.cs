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
        /// <summary>
        /// StatusRepository contructor
        /// </summary>
        public StatusRepository(ILogger<IStatusRepository> logger)
        {
            StaticClass.Logger = (Logger<IStatusRepository>)logger;
        }

        /// <summary>
        /// Ini configuration method
        /// </summary>
        /// <param name="IniFileName"></param>
        /// <returns></returns>
        private static IniFileHandler IniConfigMethod(string IniFileName)
        {
            // Get information from the Config.ini file
            return new IniFileHandler(IniFileName);
        }

        /// <summary>
        /// Get the Config.ini file data from the working directory
        /// </summary>
        public void GetIniFileData()
        {
            // Check that Config.ini file exists
            string IniFileName = "Config.ini";
            if (File.Exists(IniFileName) == false)
            {
                StaticClass.Logger.LogCritical("Missing Config.ini file");
                throw new InvalidOperationException("Config.ini file does not exist in the Handler directory");
            }

            IniFileHandler IniParser = IniConfigMethod(IniFileName);
            StaticClass.IniData.IniFileName = IniFileName;
            StaticClass.IniData.InputDir = IniParser.Read("Paths", "Input");
            StaticClass.IniData.ProcessingDir = IniParser.Read("Paths", "Processing");
            StaticClass.IniData.RepositoryDir = IniParser.Read("Paths", "Repository");
            StaticClass.IniData.FinishedDir = IniParser.Read("Paths", "Finished");
            StaticClass.IniData.ErrorDir = IniParser.Read("Paths", "Error");
            StaticClass.IniData.ModelerRootDir = IniParser.Read("Paths", "ModelerRootDir");
            StaticClass.IniData.CPUCores = int.Parse(IniParser.Read("Process", "CPUCores"));
            StaticClass.IniData.ExecutionLimit = int.Parse(IniParser.Read("Process", "ExecutionLimit"));
            StaticClass.IniData.StartPort = int.Parse(IniParser.Read("Process", "StartPort"));
            StaticClass.IniData.StatusLogFile = IniParser.Read("Process", "StatusLogFile");
            StaticClass.IniData.ProcessLogFile = IniParser.Read("Process", "ProcessLogFile");

            string scanWaitTime = IniParser.Read("Process", "ScanWaitTime");
            StaticClass.IniData.ScanWaitTime = int.Parse(scanWaitTime.Substring(0, scanWaitTime.IndexOf("#")));

            string timeLimitString = IniParser.Read("Process", "MaxJobTimeLimit");
            StaticClass.IniData.MaxJobTimeLimit = double.Parse(timeLimitString.Substring(0, timeLimitString.IndexOf("#")));

            string logFileHistoryLimit = IniParser.Read("Process", "LogFileHistoryLimit");
            StaticClass.IniData.LogFileHistoryLimit = int.Parse(logFileHistoryLimit.Substring(0, logFileHistoryLimit.IndexOf("#")));

            string inputBufferTimeLimit = IniParser.Read("Process", "InputbufferTimeLimit");
            StaticClass.IniData.InputBufferTimeLimit = int.Parse(inputBufferTimeLimit.Substring(0, inputBufferTimeLimit.IndexOf("#")));

            string logFileMaxSize = IniParser.Read("Process", "logFileMaxSize");
            StaticClass.IniData.LogFileMaxSize = int.Parse(logFileMaxSize.Substring(0, logFileMaxSize.IndexOf("#")));

            // Set the static class data needed for global use
            StaticClass.ScanWaitTime = StaticClass.IniData.ScanWaitTime;
            StaticClass.LogFileSizeLimit = StaticClass.IniData.LogFileMaxSize;
            StaticClass.MaxJobTimeLimitSeconds = StaticClass.IniData.MaxJobTimeLimit * 60.0 * 60.0;

            // Set the file logging object handle only once here
            LoggingToFile loggingToFile = new LoggingToFile(StaticClass.IniData.ProcessLogFile);
            StaticClass.FileLoggerObject = loggingToFile;

            // Output the Data.ini informatino found
            StaticClass.Log($"\nConfig.ini data found:\n");
            StaticClass.Log($"Input Dir                      : {StaticClass.IniData.InputDir}");
            StaticClass.Log($"Processing Dir                 : {StaticClass.IniData.ProcessingDir}");
            StaticClass.Log($"Repository Dir                 : {StaticClass.IniData.RepositoryDir}");
            StaticClass.Log($"Finished Dir                   : {StaticClass.IniData.FinishedDir}");
            StaticClass.Log($"Error Dir                      : {StaticClass.IniData.ErrorDir}");
            StaticClass.Log($"Modeler Root Dir               : {StaticClass.IniData.ModelerRootDir}");
            StaticClass.Log($"Status Log File                : {StaticClass.IniData.StatusLogFile}");
            StaticClass.Log($"Process Log File               : {StaticClass.IniData.ProcessLogFile}");
            StaticClass.Log($"CPU Cores                      : {StaticClass.IniData.CPUCores} Cores");
            StaticClass.Log($"Execution Limit                : {StaticClass.IniData.ExecutionLimit} Jobs");
            StaticClass.Log($"Start Port                     : {StaticClass.IniData.StartPort}");
            StaticClass.Log($"Scan Wait Time                 : {StaticClass.IniData.ScanWaitTime} Miliseconds");
            StaticClass.Log($"Max Job Time Limit             : {StaticClass.IniData.MaxJobTimeLimit} Hours");
            StaticClass.Log($"Log File History Limit         : {StaticClass.IniData.LogFileHistoryLimit} Days");
            StaticClass.Log($"Log File Max Size              : {StaticClass.IniData.LogFileMaxSize} Megabytes");
        }

        /// <summary>
        /// Method to Check the History of the log file
        /// </summary>
        public void CheckLogFileHistory()
        {
            StaticClass.CsvFileHandlerHandle.CheckLogFileHistory();
        }

        /// <summary>
        /// Start the Monitor process by starting the new jobs scan thread
        /// </summary>
        public void StartMonitorProcess()
        {
            StaticClass.ShutdownFlag = false;

            // Check for pause state and reset it if the Start button is pressed when in Pause mode
            if (StaticClass.PauseFlag == true)
            {
                StaticClass.Log(string.Format("Taking the system out of Pause Mode at {0:HH:mm:ss.fff}", DateTime.Now));
                StaticClass.PauseFlag = false;
            }
            else
            {
                // Start Modeler job Processing thread
                InputJobsScanThread newJobsScanThread = new InputJobsScanThread();
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
            StaticClass.Log(string.Format("Putting the system into Pause Mode at {0:HH:mm:ss.fff}", DateTime.Now));
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

            // Make sure threads are shut down
            StaticClass.InputJobsScanThreadHandle.Join();
            StaticClass.ProcessingFileWatcherThreadHandle.Join();
            StaticClass.ProcessingJobsScanThreadHandle.Join();
            StaticClass.DirectoryWatcherThreadHandle.Join();
            StaticClass.InputFileWatcherThreadHandle.Join();
            StaticClass.JobRunThreadHandle.Join();
            StaticClass.TcpListenerThreadHandle.Join();

            // Set all thread handles to null
            StaticClass.InputJobsScanThreadHandle= null;
            StaticClass.ProcessingFileWatcherThreadHandle= null;
            StaticClass.ProcessingJobsScanThreadHandle= null;
            StaticClass.DirectoryWatcherThreadHandle= null;
            StaticClass.InputFileWatcherThreadHandle= null;
            StaticClass.JobRunThreadHandle= null;
            StaticClass.TcpListenerThreadHandle= null;

            // Clear the Dictionaries after Modeler shutdowns complete
            StaticClass.ProcessHandles.Clear();
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
            return StaticClass.StatusDataList;
        }

        /// <summary>
        /// Get CSV file history data
        /// </summary>
        /// <returns>History Status Data List</returns>
        public IEnumerable<StatusData> GetHistoryData()
        {
            // Read data from the CSV file
            return (StaticClass.CsvFileHandlerHandle.ReadFromCsvFile());
        }
    }
}
