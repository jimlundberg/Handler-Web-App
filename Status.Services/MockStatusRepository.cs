using System;
using System.Collections.Generic;
using System.Text;
using StatusModels;

namespace Status.Services
{
    public class MockStatusRepository : IStatusRepository
    {
        private List<StatusData> _statusList;
        public MockStatusRepository()
        {
            _statusList = new List<StatusData>()
            {
                new StatusData() { Job = "1278061_202006181549", JobStatus = JobStatus.STARTED, TimeReceived = new DateTime(2020, 6, 18, 8, 15, 0),  TimeStarted = new DateTime(2020, 6, 18, 8, 15, 0),  TimeCompleted = new DateTime(2020, 6, 18, 9, 13, 0) },
                new StatusData() { Job = "1202740_202006171645", JobStatus = JobStatus.RUNNING, TimeReceived = new DateTime(2020, 6, 17, 9, 15, 0),  TimeStarted = new DateTime(2020, 6, 18, 9, 15, 0),  TimeCompleted = new DateTime(2020, 6, 18, 10, 4, 0) },
                new StatusData() { Job = "1278061_202006181549", JobStatus = JobStatus.MONITORING, TimeReceived = new DateTime(2020, 6, 18, 10, 14, 0), TimeStarted = new DateTime(2020, 6, 18, 10, 15, 0), TimeCompleted = new DateTime(2020, 6, 18, 10, 55, 0) },
                new StatusData() { Job = "1278061_202006181549", JobStatus = JobStatus.COMPLETED, TimeReceived = new DateTime(2020, 6, 17, 1, 40, 0),  TimeStarted = new DateTime(2020, 6, 18, 1, 41, 0),  TimeCompleted = new DateTime(2020, 6, 18, 4, 4, 0) },
                new StatusData() { Job = "1278061_202006177423", JobStatus = JobStatus.COPYING, TimeReceived = new DateTime(2020, 6, 16, 1, 40, 0),  TimeStarted = new DateTime(2020, 6, 16, 1, 22, 0),  TimeCompleted = new DateTime(2020, 6, 16, 4, 5, 0) }
            };
        }
        public IEnumerable<StatusData> GetAllStatus()
        {
            return _statusList;
        }
    }
}
