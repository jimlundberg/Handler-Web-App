namespace Status.Models
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

    /// <summary>
    /// Modeler Step State enum
    /// </summary>
    public enum ModelerStepState
    {
        NONE,
        STEP_1,
        STEP_2,
        STEP_3,
        STEP_4,
        STEP_5,
        STEP_6,
        STEP_COMPLETE
    };

    public enum DebugModeState : byte
    {
        NONE = 0,
        JOB_LIST = 1,
        FILE = 2,
        TCP_IP = 4,
        MODELER = 8
    }
}
