﻿using StatusModels;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Status.Services
{
    /// <summary>
    /// TCP/IP Thread Class
    /// </summary>
    public class JobTcpIpThread
    {
        // State information used in the task.
        public static IniFileData IniData;
        public static StatusMonitorData MonitorData;
        public static List<StatusWrapper.StatusData> StatusData;
        private static Thread thread;

        // The constructor obtains the state information.
        public JobTcpIpThread(IniFileData iniData, StatusMonitorData monitorData, List<StatusWrapper.StatusData> statusData)
        {
            IniData = iniData;
            MonitorData = monitorData;
            StatusData = statusData;
        }

        // The thread procedure performs the task
        public void ThreadProc()
        {
            thread = new Thread(() => TcpIpMonitor(MonitorData.JobPortNumber));
            thread.Start();          
        }

        public static void StatusEntry(List<StatusWrapper.StatusData> statusList, String job, JobStatus status, JobType timeSlot)
        {
            StatusWrapper.StatusData entry = new StatusWrapper.StatusData();
            entry.Job = job;
            entry.JobStatus = status;
            switch (timeSlot)
            {
                case JobType.TIME_START:
                    entry.TimeStarted = DateTime.Now;
                    break;

                case JobType.TIME_RECEIVED:
                    entry.TimeReceived = DateTime.Now;
                    break;

                case JobType.TIME_COMPLETE:
                    entry.TimeCompleted = DateTime.Now;
                    break;
            }

            statusList.Add(entry);
            Console.WriteLine("Status: Job:{0} Job Status:{1} Time Type:{2}", job, status, timeSlot.ToString());
        }

        public static void TcpIpMonitor(int TcpIpPortNumber)
        {
            TcpIpConnection tcpIpConnection = new TcpIpConnection();
            tcpIpConnection.Connect("127.0.0.1", MonitorData, "status");
        }
    }
}