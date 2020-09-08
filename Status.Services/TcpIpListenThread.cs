using Microsoft.Extensions.Logging;
using Status.Models;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;

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
        public static string Server = "127.0.0.1";
        private static string Job;
        private static int Port = 0;
        private static NetworkStream StreamHandle;
        private static readonly string Message = "status";
        private const int TIMEOUT = 30300; // 5 minutes

        /// <summary>
        /// Job Tcp/IP thread 
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></param>
        /// <param name="statusData"></param>
        public TcpIpListenThread(IniFileData iniData, StatusMonitorData monitorData, List<StatusData> statusData)
        {
            Port = monitorData.JobPortNumber;
            Job = monitorData.Job;
            IniData = iniData;
            MonitorData = monitorData;
            StatusData = statusData;
        }

        /// <summary>
        /// Start TCP/IP scan process
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></param>
        /// <param name="statusData"></param>
        public void StartTcpIpScanProcess(IniFileData iniData, StatusMonitorData monitorData, List<StatusData> statusData)
        {
            // Start TCP/IP thread
            TcpIpListenThread tcpIp = new TcpIpListenThread(iniData, monitorData, statusData);
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
            StaticClass.TcpListenerThreadHandle = new Thread(() => Connect(Port, Server, IniData, MonitorData, StatusData));
            if (StaticClass.TcpListenerThreadHandle == null)
            {
                StaticClass.Logger.LogError("TcpIpListenThread TcpListenerThreadHandle failed to instantiate");
            }
            StaticClass.TcpListenerThreadHandle.Start();
        }

        /// <summary>
        /// Retry Timer elapsed handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void retryTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            StaticClass.Log(String.Format("\nSending Retry msg {0} to Modeler for Job {1} on Port {2} at {3:HH:mm:ss.fff}",
                Message, Job, Port, DateTime.Now));

            // Send retry message to the Modeler
            Byte[] sendData = Encoding.ASCII.GetBytes(Message);
            StreamHandle.Write(sendData, 0, sendData.Length);
        }

        /// <summary>
        /// Connect to TCP/IP Port 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></param>
        /// <param name="statusData"></param>
        public static void Connect(int port, string server, IniFileData iniData, StatusMonitorData monitorData, List<StatusData> statusData)
        {
            // Wait about a minute for the Modeler to start execution
            Thread.Sleep(60000);

            string job = monitorData.Job;

            StaticClass.Log(String.Format("\nStarting TCP/IP Scan for Job {0} on Port {1} at {2:HH:mm:ss.fff}",
                job, port, DateTime.Now));

            // Log starting TCP/IP monitoring entry
            StaticClass.StatusDataEntry(statusData, job, iniData, JobStatus.MONITORING_TCPIP, JobType.TIME_START);

            try
            {
                // Create a TcpClient
                TcpClient client = new TcpClient(server, port);
                if (client == null)
                {
                    StaticClass.Logger.LogError("TcpIp Connectinon client failed to instantiate");
                }

                // Get a client stream for reading and writing
                NetworkStream stream = client.GetStream();
                if (stream == null)
                {
                    StaticClass.Logger.LogError("TcpIp Connection stream handle was not gotten from client");

                    StaticClass.TcpIpScanComplete[job] = true;
                    return;
                }

                // Receive the TcpServer.response
                //StaticClass.Log(String.Format("Starting timer for Job {0} on Port {1} at {2:HH:mm:ss.fff}",
                //    job, port, DateTime.Now));

                // Start 60 second resend timer that gets reset if we receive data
                //var resendTimer = new System.Timers.Timer(TIMEOUT);
                //resendTimer.Elapsed += new ElapsedEventHandler(retryTimer_Elapsed);
                //resendTimer.Enabled = true;
                //resendTimer.Start();

                StaticClass.Log(String.Format("\nConnected to TCP/IP for Job {0} on Port {1} at {2:HH:mm:ss.fff}",
                    job, port, DateTime.Now));

                bool jobComplete = false;
                do
                {
                    if (StaticClass.ShutdownFlag == true)
                    {
                        StaticClass.Log(String.Format("\nShutdown TcpIpListenThread prewrite for Job {0} on Port {1} at {2:HH:mm:ss.fff}",
                            job, port, DateTime.Now));

                        StaticClass.TcpIpScanComplete[job] = true;

                        // Make sure to close TCP/IP socket
                        jobComplete = true;
                    }

                    // Check if the pause flag is set, then wait for reset
                    if (StaticClass.PauseFlag == true)
                    {
                        StaticClass.Log(String.Format("TcpIpListenThread Connect1 is in Pause mode at {0:HH:mm:ss.fff}", DateTime.Now));
                        do
                        {
                            Thread.Yield();
                        }
                        while (StaticClass.PauseFlag == true);
                    }

                    // Translate the passed message into ASCII and store it as a Byte array.
                    Byte[] sendData = Encoding.ASCII.GetBytes(Message);

                    // Send the message to the Modeler
                    stream.Write(sendData, 0, sendData.Length);

                    // Set data for retries
                    StreamHandle = stream;
                    Port = port;
                    Job = job;

                    StaticClass.Log(String.Format("\nSending {0} msg to Modeler for Job {1} on Port {2} at {3:HH:mm:ss.fff}",
                        Message, job, port, DateTime.Now));

                    // Buffer to store the response bytes.
                    Byte[] recvData = new Byte[256];

                    // String to store the response in ASCII representation
                    string responseData = String.Empty;
                    int adjustableSleepTime = 15000;
                    int bytes = 0;
                    bytes = stream.Read(recvData, 0, recvData.Length);
                    responseData = Encoding.ASCII.GetString(recvData, 0, bytes);

                    StaticClass.Log(String.Format("Received: {0} from Job {1} on Port {2} at {3:HH:mm:ss.fff}",
                        responseData, job, port, DateTime.Now));

                    // Reset timer if data received
                    //if (responseData.Length > 0)
                    //{
                    // Receive the TcpServer.response
                    //StaticClass.Log(String.Format("Resetting TCP/IP Timout timer for Job {0} on Port {1} at {2:HH:mm:ss.fff}",
                    //    job, port, DateTime.Now));

                    //resendTimer.Stop();
                    //resendTimer.Start();
                    //}

                    // Readjust sleep time according to Step number
                    switch (responseData)
                    {
                        case "Step 1 in process.":
                            adjustableSleepTime = 15000;
                            break;

                        case "Step 2 in process.":
                            adjustableSleepTime = 10000;
                            break;

                        case "Step 3 in process.":
                            adjustableSleepTime = 7500;
                            break;

                        case "Step 4 in process.":
                            adjustableSleepTime = 5000;
                            break;

                        case "Step 5 in process.":
                            adjustableSleepTime = 2500;
                            break;

                        case "Step 6 in process.":
                            adjustableSleepTime = 250;
                            break;

                        case "Whole process done, socket closed.":
                            StaticClass.Log(String.Format("TCP/IP for Job {0} on Port {1} received Modeler process done at {2:HH:mm:ss.fff}",
                                job, port, DateTime.Now));

                            StaticClass.Log(String.Format("Closing TCP/IP Socket for Job {0} on Port {1} at {2:HH:mm:ss.fff}",
                                job, port, DateTime.Now));

                            // Set the TCP/IP Scan complete flag to signal the RunJob thread
                            StaticClass.TcpIpScanComplete[job] = true;

                            // Make sure to close TCP/IP socket
                            jobComplete = true;
                            break;

                        default:
                            StaticClass.Logger.LogWarning("Received Weird Response: {0} from Job {1} on Port {2} at {3:HH:mm:ss.fff}",
                                responseData, job, port, DateTime.Now);
                            break;
                    }

                    // Backup check of the process complete string, even if it is concatenated with another string
                    if (responseData.Contains("Whole process done, socket closed."))
                    {
                        StaticClass.Log(String.Format("TCP/IP for Job {0} on Port {1} received Modeler process complete at {2:HH:mm:ss.fff}",
                            job, port, DateTime.Now));

                        // Set the TCP/IP Scan complete flag to signal the RunJob thread
                        StaticClass.TcpIpScanComplete[job] = true;

                        // Make sure to close TCP/IP socket
                        jobComplete = true;
                    }

                    // Check for job timeout
                    if ((DateTime.Now - monitorData.StartTime).TotalSeconds > StaticClass.MaxJobTimeLimitSeconds)
                    {
                        StaticClass.Log(String.Format("Job Timeout for Job {0} at {1:HH:mm:ss.fff}", job, DateTime.Now));

                        // Create job Timeout status
                        StaticClass.StatusDataEntry(statusData, job, iniData, JobStatus.JOB_TIMEOUT, JobType.TIME_START);

                        // Set all flags to complete job Process
                        StaticClass.ProcessingJobScanComplete[job] = true;
                        StaticClass.TcpIpScanComplete[job] = true;
                        StaticClass.ProcessingFileScanComplete[job] = true;

                        // Wait a sec then shutdown the Modeler after completing job
                        Thread.Sleep(1000);
                        StaticClass.ProcessHandles[job].Kill();

                        // Make sure to close TCP/IP socket
                        jobComplete = true;
                    }

                    // Check if the shutdown flag is set, then exit method
                    if (StaticClass.ShutdownFlag == true)
                    {
                        StaticClass.Log(String.Format("\nShutdown TcpIpListenThread Connect for Job {0} on Port {1} at {2:HH:mm:ss.fff}",
                            job, port, DateTime.Now));

                        StaticClass.TcpIpScanComplete[job] = true;

                        // Make sure to close TCP/IP socket
                        jobComplete = true;
                    }

                    // Check if the pause flag is set, then wait for reset
                    if (StaticClass.PauseFlag == true)
                    {
                        StaticClass.Log(String.Format("TcpIpListenThread Connect3 is in Pause mode at {0:HH:mm:ss.fff}", DateTime.Now));
                        do
                        {
                            Thread.Yield();
                        }
                        while (StaticClass.PauseFlag == true);
                    }

                    // Wait for an adjustable time between TCP/IP status requests
                    Thread.Sleep(adjustableSleepTime);
                }
                while (jobComplete == false);

                // Close everything
                stream.Close();
                client.Close();

                StaticClass.Log(String.Format("Closed TCP/IP Socket for Job {0} on Port {1} at {2:HH:mm:ss.fff}",
                    job, port, DateTime.Now));
            }
            catch (SocketException e)
            {
                StaticClass.Log(String.Format("SocketException {0} for Job {1} on Port {2} at {3:HH:mm:ss.fff}",
                    e, job, port, DateTime.Now));

                StaticClass.Logger.LogError(String.Format("SocketException {0} for Job {1} on Port {2} at {3:HH:mm:ss.fff}",
                    e, job, port, DateTime.Now));
            }
        }
    }
}