﻿using System;

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
        COPYING_TO_ARCHIVE,
        COMPLETE
    }

    public enum JobType
    {
        NONE,
        TIME_START,
        TIME_RECIEVED,
        TIME_COMPLETE
    }
}
