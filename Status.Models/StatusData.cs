using System;

namespace StatusModels
{
    /// <summary>
    /// Job Status Data
    /// </summary>
    public class StatusData
    {
        public string Job { get; set; }

        public JobStatus? JobStatus { get; set; }

        public DateTime TimeReceived { get; set; }

        public DateTime TimeStarted { get; set; }

        public DateTime TimeCompleted { get; set; }
    }
}