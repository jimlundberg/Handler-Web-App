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
        MONITORING_TCPIP,
        COPYING_TO_ARCHIVE,
        COMPLETE
    }

    public enum JobType
    {
        NONE,
        TIME_START,
        TIME_RECEIVED,
        TIME_COMPLETE
    }
}