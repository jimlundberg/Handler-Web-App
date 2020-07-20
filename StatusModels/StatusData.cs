using System;
using System.Collections.Generic;
using System.Text;

namespace StatusModels
{
    public class StatusData
    {
        public String Job { get; set; }
        public JobStatus? JobStatus { get; set; }
        public DateTime TimeReceived { get; set; }
        public DateTime TimeStarted { get; set; }
        public DateTime TimeCompleted { get; set; }
    }
}
