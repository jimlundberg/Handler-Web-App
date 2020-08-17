using System;
using System.Collections.Generic;

namespace Status.Services
{
    public delegate void TimeoutCallbackType(string job);
    public delegate void jobCompleteCallbackType(string job);

    /// <summary>
    /// Static Data with global access
    /// </summary>
    public static class StaticData
    {
        public static int NumberOfJobsExecuting = 0;
        public static int RunningJobsIndex = 0;
        public static int logFileSizeLimit = 0;
        public static volatile bool ShutdownFlag = false;
        public static bool OldJobScanComplete = false;
        public static bool ExitDirectoryScan = false;
        public static bool ExitInputFileScan = false;
        public static Dictionary<string, bool> TcpIpScanComplete = new Dictionary<string, bool>();
        public static Dictionary<string, bool> ExitProcessingFileScan = new Dictionary<string, bool>();
        public static volatile bool FoundNewJobReadyToRun = false;
        public static List<String> newJobsToRun = new List<String>();

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
