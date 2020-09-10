﻿using Microsoft.Extensions.Logging;
using Status.Models;
using System;
using System.Collections.Generic;

namespace Status.Services
{
    /// <summary>
    /// Status Entry class
    /// </summary>
    public class StatusEntry
    {
        /// <summary>
        /// Status Entry default constructor
        /// </summary>
        public StatusEntry()
        {
            StaticClass.Logger.LogInformation("StatusEntry default constructor called");
        }

        /// <summary>
        /// Status Entry thread default destructor
        /// </summary>
        ~StatusEntry()
        {
            StaticClass.Logger.LogInformation("StatusEntry default destructor called");
        }

        /// <summary>
        /// Log a Status and write to csv file
        /// </summary>
        /// <param name="statusList"></param>
        /// <param name="job"></param>
        /// <param name="status"></param>
        /// <param name="timeSlot"></param>
        public void ListStatus(List<StatusData> statusList, string job, JobStatus status, JobType timeSlot)
        {
            StatusData entry = new StatusData();
            if (entry == null)
            {
                StaticClass.Logger.LogError("StatusEntry entry failed to instantiate");
            }

            entry.Job = job;
            entry.JobStatus = status;
            switch (timeSlot)
            {
                case JobType.TIME_RECEIVED:
                    entry.TimeReceived = DateTime.Now;
                    break;

                case JobType.TIME_START:
                    entry.TimeStarted = DateTime.Now;
                    break;

                case JobType.TIME_COMPLETE:
                    entry.TimeCompleted = DateTime.Now;
                    break;
            }

            // Add entry to status data list
            statusList.Add(entry);

            // Output to Console also
            StaticClass.Log(String.Format("Status: Job:{0} Job Status:{1} Time Type:{2}",
                job, status, timeSlot.ToString()));
        }
    }
}
