using System;
using System.Collections.Generic;
using StatusModels;
using static StatusModels.StatusWrapper;

namespace Status.Services
{
    public interface IStatusRepository
    {
        void GetIniFileData();
        void CheckCsvFileHistory();
        IEnumerable<StatusData> GetHistoryData();
        IEnumerable<StatusData> GetJobStatus();
        void GetMonitorStatus();
        void StopMonitor();
    }
}
