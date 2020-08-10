using StatusModels;
using System.Collections.Generic;

namespace Status.Services
{
    public interface IStatusRepository
    {
        void GetIniFileData();
        void CheckLogFileHistory();
        IEnumerable<StatusWrapper.StatusData> GetHistoryData();
        IEnumerable<StatusWrapper.StatusData> GetJobStatus();
        void StartMonitorProcess();
        void StopMonitor();
    }
}
