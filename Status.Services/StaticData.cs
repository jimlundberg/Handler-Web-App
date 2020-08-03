namespace Status.Services
{
    public static class StaticData
    {
        public static int NumberOfJobsExecuting = 0;
        public static int RunningJobsIndex = 0;
        public static volatile bool ShutdownFlag = false;

        public static void IncrementNumberOfJobsExecuting()
        {
            NumberOfJobsExecuting++;
        }

        public static void DecrementNumberOfJobsExecuting()
        {
            NumberOfJobsExecuting--;
        }
    }
}
