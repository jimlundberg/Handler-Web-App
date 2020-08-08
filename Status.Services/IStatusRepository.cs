using StatusModels;
using System.Collections.Generic;

namespace Status.Services
{
    public interface IStatusRepository
    {
        void GetIniFileData();
        void CheckLogFileHistory();
        IEnumerable<StatusData> GetHistoryData();
        IEnumerable<StatusData> GetJobStatus();
        void StartMonitorProcess();
        void StopMonitor();
    }
}
