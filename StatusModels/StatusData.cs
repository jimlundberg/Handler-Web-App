using System;

namespace StatusModels
{
    // Wrap the statusdata class within a wrapper so we can pass lastupdate

    public class StatusWrapper
    {
        public DateTime LastUpdate { get; set; }

        public class StatusData
        {
            public String Job { get; set; }

            public JobStatus? JobStatus { get; set; }

            public DateTime TimeReceived { get; set; }

            public DateTime TimeStarted { get; set; }

            public DateTime TimeCompleted { get; set; }
        }
    }
}