using System;

namespace StatusModels
{
    public enum JobStatus
    {
        NONE,
        STARTED,
        RUNNING,
        EXECUTING,
        MONITORING,
        COPYING,
        COMPLETED
    }
}
