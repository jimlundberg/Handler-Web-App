namespace Status.Services
{
    public static class Counters
    {
        public static int NumberOfJobsExecuting = 0;
        public static int RunningJobsIndex = 0;

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
