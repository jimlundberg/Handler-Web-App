using StatusModels;
using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Status.Services
{
    /// <summary>
    /// TCP/IP Thread Class
    /// </summary>
    public class JobTcpIpThread
    {
        public static IniFileData IniData;
        public static StatusMonitorData MonitorData;
        public static List<StatusData> StatusData;
        private static Thread tcpIpthread;
        public event EventHandler ProcessCompleted;
        public static ILogger<StatusRepository> Logger;

        /// <summary>
        /// Job Tcp/IP thread 
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public JobTcpIpThread(IniFileData iniData, StatusMonitorData monitorData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            IniData = iniData;
            MonitorData = monitorData;
            StatusData = statusData;
            Logger = logger;
        }

        /// <summary>
        /// Start Tcp/IP scan process
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></param>
        /// <param name="statusData"></param>
        public void StartTcpIpScanProcess(IniFileData iniData, StatusMonitorData monitorData, List<StatusData> statusData)
        {
            // Start Tcp/Ip thread
            JobTcpIpThread tcpIp = new JobTcpIpThread(iniData, monitorData, statusData, Logger);
            tcpIp.ThreadProc();
        }

        /// <summary>
        /// TCP/IP process complete callback
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnProcessCompleted(EventArgs e)
        {
            ProcessCompleted?.Invoke(this, e);
        }

        /// <summary>
        /// Thread procedure to run the TCP/IP communications with the Modeler
        /// </summary>
        public void ThreadProc()
        {
            tcpIpthread = new Thread(() => TcpIpMonitor());
            tcpIpthread.Start();          
        }

        /// <summary>
        /// Status data entry
        /// </summary>
        /// <param name="statusList"></param>
        /// <param name="job"></param>
        /// <param name="status"></param>
        /// <param name="timeSlot"></param>
        public static void StatusEntry(List<StatusData> statusList, string job, JobStatus status, JobType timeSlot)
        {
            StatusData entry = new StatusData();
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
            StaticData.Log(IniData.ProcessLogFile, 
                String.Format("Status: Job:{0} Job Status:{1}", job, status));
        }

        /// <summary>
        /// Start Tcp/Ip communications monitor
        /// </summary>
        public void TcpIpMonitor()
        {
            TcpIpConnection tcpIpConnection = new TcpIpConnection();
            tcpIpConnection.Connect("127.0.0.1", IniData, MonitorData, StatusData, "status", Logger);
        }
    }
}
