﻿using Microsoft.Extensions.Logging;
using Status.Models;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace Status.Services
{
    /// <summary>
    /// TCP/IP Thread Class
    /// </summary>
    public class TcpIpListenThread
    {
        private static IniFileData IniData;
        private static StatusMonitorData MonitorData;
        private static List<StatusData> StatusData;
        public event EventHandler ProcessCompleted;
        public static ILogger<StatusRepository> Logger;
        public const string Host = "127.0.0.1";
        private readonly int Port = 0;

        /// <summary>
        /// Job Tcp/IP thread 
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public TcpIpListenThread(IniFileData iniData, StatusMonitorData monitorData,
            List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            Port = monitorData.JobPortNumber;
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
            TcpIpListenThread tcpIp = new TcpIpListenThread(iniData, monitorData, statusData, Logger);
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
            StaticClass.TcpIpListenThreadHandle = new Thread(() => Connect(Port, IniData, MonitorData, StatusData, "status", Logger));
            if (StaticClass.TcpIpListenThreadHandle == null)
            {
                Logger.LogError("TcpIpListenThread thread failed to instantiate");
            }
            StaticClass.TcpIpListenThreadHandle.Start();
        }

        /// <summary>
        /// Connect to TCP/IP Port 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></param>
        /// <param name="statusData"></param>
        /// <param name="message"></param>
        /// <param name="logger"></param>
        public void Connect(int port, IniFileData iniData, StatusMonitorData monitorData,
            List<StatusData> statusData, string message, ILogger<StatusRepository> logger)
        {
            // Wait about a minute for the Modeler to start execution
            Thread.Sleep(StaticClass.ScanWaitTime * 12);

            try
            {
                string job = monitorData.Job;

                StaticClass.Log(String.Format("\nStarting Tcp/Ip Scan for job {0} on port {1} at {2:HH:mm:ss.fff}",
                    job, port, DateTime.Now));

                // Log Tcp/Ip monitoring entry
                StaticClass.StatusDataEntry(statusData, job, iniData, JobStatus.MONITORING_TCPIP, JobType.TIME_START, logger);

                // Create a TcpClient.
                // Note, for this client to work you need to have a TcpServer
                // connected to the same address as specified by the server, port combination.
                TcpClient client = new TcpClient(Host, port);
                if (client == null)
                {
                    logger.LogError("TcpIp Connectinon client failed to instantiate");
                }

                // Translate the passed message into ASCII and store it as a Byte array.
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);

                // Get a client stream for reading and writing.
                // Stream stream = client.GetStream();
                NetworkStream stream = client.GetStream();
                if (stream == null)
                {
                    logger.LogError("TcpIp Connection stream handle was not gotten from client");
                    client.Close();
                    StaticClass.TcpIpScanComplete[job] = true;
                    return;
                }

                StaticClass.Log(String.Format("Opening TCP/IP socket for Job {0} on port {1} at {2:HH:mm:ss.fff}", job, port, DateTime.Now));

                bool jobComplete = false;
                do
                {
                    if (StaticClass.ShutdownFlag == true)
                    {
                        StaticClass.Log(String.Format("\nShutdown TcpIpListenThread prewrite for Job {0} on port {1} at {2:HH:mm:ss.fff}",
                            job, port, DateTime.Now));

                        StaticClass.TcpIpScanComplete[job] = true;

                        // Make sure to close TCP/IP socket
                        jobComplete = true;
                    }

                    // Check if the pause flag is set, then wait for reset
                    if (StaticClass.PauseFlag == true)
                    {
                        do
                        {
                            Thread.Yield();
                        }
                        while (StaticClass.PauseFlag == true);
                    }

                    // Send the message to the Modeler
                    stream.Write(data, 0, data.Length);

                    // Receive the TcpServer.response.
                    StaticClass.Log(String.Format("\nSending {0} msg to Modeler for Job {1} on port {2} at {3:HH:mm:ss.fff}",
                        message, job, port, DateTime.Now));

                    // Buffer to store the response bytes.
                    data = new Byte[256];

                    // String to store the response ASCII representation.
                    string responseData = String.Empty;
                    int adjustableSleepTime = 5000;

                    // Try to read the Modeler response at least 5 times
                    if (stream.CanRead)
                    {
                        int bytes = 0;
                        for (int i = 0; i < 5; i++)
                        {
                            try
                            {
                                // Check if the shutdown flag is set
                                if (StaticClass.ShutdownFlag == true)
                                {
                                    StaticClass.Log(String.Format("\nShutdown TcpIpListenThread preread for Job {0} on port {1} at {2:HH:mm:ss.fff}",
                                        job, port, DateTime.Now));

                                    StaticClass.TcpIpScanComplete[job] = true;

                                    // Make sure to close TCP/IP socket
                                    jobComplete = true;
                                }

                                // Check if the pause flag is set, then wait for reset
                                if (StaticClass.PauseFlag == true)
                                {
                                    do
                                    {
                                        Thread.Yield();
                                    }
                                    while (StaticClass.PauseFlag == true);
                                }

                                bytes = stream.Read(data, 0, data.Length);
                                if (bytes > 0)
                                {
                                    break;
                                }
                            }
                            catch (Exception e)
                            {
                                logger.LogWarning(String.Format("Tcp/Ip Read for job {0} port {1} failed with error {2}",
                                    job, port, e));

                                if (i == 4)
                                {
                                    logger.LogError(String.Format("Tcp/Ip Connection Timeout for job {0} port {1} after 5 tries with error {2}",
                                        job, port, e));

                                    StaticClass.TcpIpScanComplete[job] = true;

                                    // Make sure to close TCP/IP socket
                                    jobComplete = true;
                                }
                            }

                            // Wait between TCP/IP Connection tries
                            Thread.Sleep(StaticClass.ScanWaitTime * 3);
                        }

                        // Get the Modeler response and display it
                        responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                        StaticClass.Log(String.Format("Received: {0} from Job {1} on port {2} at {3:HH:mm:ss.fff}",
                            responseData, job, port, DateTime.Now));

                        // Send status for response received
                        switch (responseData)
                        {
                            case "Step 1 in process.":
                                adjustableSleepTime = 15000;
                                break;

                            case "Step 2 in process.":
                                adjustableSleepTime = 15000;
                                break;

                            case "Step 3 in process.":
                                adjustableSleepTime = 15000;
                                break;

                            case "Step 4 in process.":
                                adjustableSleepTime = 10000;
                                break;

                            case "Step 5 in process.":
                                adjustableSleepTime = 5000;
                                break;

                            case "Step 6 in process.":
                                adjustableSleepTime = 1000;
                                break;

                            case "Whole process done, socket closed.":
                                StaticClass.Log(String.Format("TCP/IP for Job {0} on port {1} received Modeler process complete at {2:HH:mm:ss.fff}",
                                    job, port, DateTime.Now));

                                StaticClass.TcpIpScanComplete[job] = true;
                                jobComplete = true;
                                return;

                            default:
                                logger.LogWarning("Received Weird Response: {0} from Job {1} on port {2} at {3:HH:mm:ss.fff}",
                                    responseData, job, port, DateTime.Now);
                                break;
                        }

                        // Backup check of the process complete string, even if it is concatenated with another string
                        if (responseData.Contains("Whole process done, socket closed."))
                        {
                            StaticClass.Log(String.Format("TCP/IP for Job {0} on port {1} received Modeler socket complete at {2:HH:mm:ss.fff}",
                                job, port, DateTime.Now));

                            StaticClass.TcpIpScanComplete[job] = true;
                            jobComplete = true;
                        }

                        // Check for job timeout
                        if ((DateTime.Now - monitorData.StartTime).TotalSeconds > StaticClass.MaxJobTimeLimitSeconds)
                        {
                            StaticClass.Log(String.Format("Job Timeout for job {0} at {1:HH:mm:ss.fff}", job, DateTime.Now));

                            // Create job Timeout status
                            StaticClass.StatusDataEntry(statusData, job, iniData, JobStatus.JOB_TIMEOUT, JobType.TIME_COMPLETE, logger);

                            // Set all flags to complete job Process
                            StaticClass.ProcessingJobScanComplete[job] = true;
                            StaticClass.TcpIpScanComplete[job] = true;
                            StaticClass.ProcessingFileScanComplete[job] = true;

                            // Shut down the Modeler after shutting down the job
                            StaticClass.ProcessHandles[job].Kill();

                            // Make sure to close TCP/IP socket
                            jobComplete = true;
                        }

                        // Check if the shutdown flag is set, then exit method
                        if (StaticClass.ShutdownFlag == true)
                        {
                            StaticClass.Log(String.Format("\nShutdown TcpIpListenThread Connect for Job {0} on port {1} at {2:HH:mm:ss.fff}",
                                job, port, DateTime.Now));

                            StaticClass.TcpIpScanComplete[job] = true;

                            // Make sure to close TCP/IP socket
                            jobComplete = true;
                        }

                        // Check if the pause flag is set, then wait for reset
                        if (StaticClass.PauseFlag == true)
                        {
                            do
                            {
                                Thread.Yield();
                            }
                            while (StaticClass.PauseFlag == true);
                        }

                        // Wait for an adjustable time between TCP/IP status requests
                        Thread.Sleep(adjustableSleepTime);
                    }
                    else
                    {
                        logger.LogError(String.Format("Can not read TCP/IP Stream for job {0} at {1:HH:mm:ss.fff}",
                            job, DateTime.Now));

                        // Make sure to close TCP/IP socket
                        jobComplete = true;
                    }
                }
                while (jobComplete == false);

                // Close everything
                stream.Close();
                client.Close();

                StaticClass.Log(String.Format("Closing TCP/IP socket for Job {0} on port {1} at {2:HH:mm:ss.fff}",
                    job, port, DateTime.Now));
            }
            catch (ArgumentNullException e)
            {
                logger.LogError("ArgumentNullException: {0}", e);
            }
            catch (SocketException e)
            {
                logger.LogError("SocketException: {0}", e);
            }
        }
    }
}
