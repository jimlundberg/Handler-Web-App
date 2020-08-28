namespace StatusModels
{
    /// <summary>
    /// Job Status types
    /// </summary>
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

    /// <summary>
    /// Job Types
    /// </summary>
    public enum JobType
    {
        NONE,
        TIME_START,
        TIME_RECEIVED,
        TIME_COMPLETE
    }

    /// <summary>
    /// Directory Scan Types
    /// </summary>
    public enum DirectoryScanType
    {
        NONE,
        INPUT_BUFFER,
        PROCESSING_BUFFER
    }
}
