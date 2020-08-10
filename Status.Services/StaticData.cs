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
