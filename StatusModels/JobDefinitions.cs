namespace StatusModels
{
    public enum JobStatus
    {
        NONE,
        JOB_STARTED,
        EXECUTING,
        MONITORING_INPUT,
        COPYING_TO_PROCESSING,
        MONITORING_PROCESSING,
        MONITORING_TCPIP,
        COPYING_TO_ARCHIVE,
        JOB_TIMEOUT,
        COMPLETE
    }

    public enum JobType
    {
        NONE,
        TIME_START,
        TIME_RECEIVED,
        TIME_COMPLETE
    }

    public enum DirectoryScanType
    {
        NONE,
        INPUT_BUFFER,
        PROCESSING_BUFFER
    }
}
