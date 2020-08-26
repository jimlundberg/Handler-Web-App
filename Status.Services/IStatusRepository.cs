using StatusModels;
using System.Collections.Generic;

namespace Status.Services
{
    /// <summary>
    /// Status Repository Interface Class
    /// </summary>
    public interface IStatusRepository
    {
        void GetIniFileData();
        void CheckLogFileHistory();
        IEnumerable<StatusData> GetHistoryData();
        IEnumerable<StatusData> GetJobStatus();
        void PauseMonitor();
        void StartMonitorProcess();
        void StopMonitor();
    }
}
