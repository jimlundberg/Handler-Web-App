using System;
using System.Collections.Generic;
using StatusModels;

namespace Status.Services
{
    public interface IStatusRepository
    {
        IEnumerable<StatusData> GetJobStatus();
        IniFileData GetMonitorStatus();
        void StopMonitor();
    }
}
