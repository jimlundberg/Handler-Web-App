using System;

namespace Status.Services
{
    public static class StaticData
    {
        public static int NumberOfJobsExecuting = 0;
        public static int RunningJobsIndex = 0;
        public static volatile bool ShutdownFlag = false;
        public static volatile bool TcpIpScanComplete = true;
        public static volatile bool OldJobScanComplete = false;
        public static volatile bool ExitDirectoryScan = false;
        public static volatile bool FoundNewJobsReady = false;
        public static int sizeLimitInBytes = 130069; // 5 * 1024 * 1024; // 5 MB

        public static void Log(string logFile, string msg)
        {
            LoggingToFile log = new LoggingToFile(logFile);
            log.WriteToLogFile(msg);
            Console.WriteLine(msg);
        }

        public static void IncrementNumberOfJobsExecuting()
        {
            NumberOfJobsExecuting++;
        }

        public static void DecrementNumberOfJobsExecuting()
        {
            NumberOfJobsExecuting--;
        }
        public static string AddQuotesIfRequired(string path)
        {
            return !string.IsNullOrWhiteSpace(path) ?
                path.Contains(" ") && (!path.StartsWith("\"") && !path.EndsWith("\"")) ?
                    "\"" + path + "\"" : path :
                    string.Empty;
        }
    }
}
