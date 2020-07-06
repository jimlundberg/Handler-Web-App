using System;
using System.Collections.Generic;
using StatusModels;

namespace Status.Services
{
    public interface IStatusRepository
    {
        IEnumerable<StatusData> GetAllStatus();
        IEnumerable<StatusMonitorData> GetMonitorStatus();
    }
}
