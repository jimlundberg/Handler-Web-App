using System;
using System.Collections.Generic;
using StatusModels;
using static StatusModels.StatusWrapper;

namespace Status.Services
{
    public interface IStatusRepository
    {
        IEnumerable<StatusData> GetJobStatus();
        IniFileData GetMonitorStatus();
        void StopMonitor();
        IEnumerable<StatusData> GetHistoryData();
    }
}
