﻿using System;
using System.Collections.Generic;

namespace Status.Services
{
    /// <summary>
    /// Static Data with global access
    /// </summary>
    public static class StaticData
    {
        public static int NumberOfJobsExecuting = 0;
        public static int RunningJobsIndex = 0;
        public static int logFileSizeLimit = 0;
        public static volatile bool ShutdownFlag = false;
        public static volatile bool CurrentInputJobsScanComplete = false;
        public static volatile bool CurrentProcessingJobsScanComplete = false;
        public static volatile bool DirectoryScanComplete = false;
        public static volatile bool FoundNewJobReadyToRun = false;
        public static Dictionary<string, bool> InputFileScanComplete = new Dictionary<string, bool>();
        public static Dictionary<string, bool> ProcessingFileScanComplete = new Dictionary<string, bool>();
        public static Dictionary<string, bool> TcpIpScanComplete = new Dictionary<string, bool>();
        public static Dictionary<string, int> NumberOfInputFilesFound = new Dictionary<string, int>();
        public static Dictionary<string, int> NumberOfInputFilesNeeded = new Dictionary<string, int>();
        public static Dictionary<string, int> NumberOfProcessingFilesFound = new Dictionary<string, int>();
        public static Dictionary<string, int> NumberOfProcessingFilesNeeded = new Dictionary<string, int>();
        public static List<String> NewInputJobsToRun = new List<String>();

        /// <summary>
        /// Global log to file method
        /// </summary>
        /// <param name="logFile"></param>
        /// <param name="msg"></param>
        public static void Log(string logFile, string msg)
        {
            LoggingToFile log = new LoggingToFile(logFile);
            log.WriteToLogFile(msg);
            Console.WriteLine(msg);
        }
    }
}
