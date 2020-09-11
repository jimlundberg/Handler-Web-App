using Microsoft.Extensions.Logging;
using Status.Models;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
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
        private static readonly string Server = "127.0.0.1";
        private static readonly string StatusMessage = "status";
        private static int Port = 0;

        /// <summary>
        /// TCP/IP Listen thread default constructor
        /// </summary>
        public TcpIpListenThread()
        {
            StaticClass.Logger.LogInformation("TcpIpListenThread default constructor called");
        }

        /// <summary>
        /// TCP/IP Listen thread default destructor
        /// </summary>
        ~TcpIpListenThread()
        {
            StaticClass.Logger.LogInformation("TcpIpListenThread default destructor called");
        }

        /// <summary>
        /// Job Tcp/IP thread 
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></param>
        /// <param name="statusData"></param>
        public TcpIpListenThread(IniFileData iniData, StatusMonitorData monitorData, List<StatusData> statusData)
        {
            Port = monitorData.JobPortNumber;
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
        /// Connect to TCP/IP Port 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></pram>
        /// <param name="statusData"></param>
        public static void Connect(int port, string server, IniFileData iniData, StatusMonitorData monitorData, List<StatusData> statusData)
        {
            ModelerStepState ModelerCurrentStepState = ModelerStepState.NONE;
            string job = monitorData.Job;

            // Wait about a minute for the Modeler to start execution
            Thread.Sleep(StaticClass.TCP_IP_STARTUP_WAIT);

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

                StaticClass.Log(String.Format("\nConnected to TCP/IP for Job {0} on Port {1} at {2:HH:mm:ss.fff}", job, port, DateTime.Now));
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

                    // Check if the stream is writable, if not, exit
                    if (stream.CanWrite)
                    {
                        // Translate the passed message into ASCII and store it as a Byte array.
                        Byte[] sendData = Encoding.ASCII.GetBytes(StatusMessage);
                        stream.Write(sendData, 0, sendData.Length);

                        StaticClass.Log(String.Format("\nSending {0} msg to Modeler for Job {1} on Port {2} at {3:HH:mm:ss.fff}",
                            StatusMessage, job, port, DateTime.Now));
                    }
                    else
                    {
                        StaticClass.Log(String.Format("\nTCP/IP stream closed for Modeler Job {0} on Port {1} at {2:HH:mm:ss.fff}",
                            job, port, DateTime.Now));
                      
                        // Make sure to close TCP/IP socket
                        stream.Close();
                        client.Close();

                        StaticClass.Log(String.Format("Closed TCP/IP Socket for Job {0} on Port {1} at {2:HH:mm:ss.fff}",
                            job, port, DateTime.Now));

                        // Set the TCP/IP Scan complete flag to signal the RunJob thread
                        StaticClass.TcpIpScanComplete[job] = true;
                        return;
                    }

                    // Check if TCP/IP stream is readable and data is available
                    int adjustableSleepTime = StaticClass.STARTING_TCP_IP_WAIT;
                    int tcpIpRetryCount = 0;
                    bool messageReceived = false;
                    do
                    {
                        if (stream.CanRead && stream.DataAvailable)
                        {
                            // Buffers to store the response
                            Byte[] recvData = new Byte[256];
                            string responseData = String.Empty;
                            int bytes = stream.Read(recvData, 0, recvData.Length);
                            responseData = Encoding.ASCII.GetString(recvData, 0, bytes);

                            StaticClass.Log(String.Format("Received: {0} from Job {1} on Port {2} at {3:HH:mm:ss.fff}",
                                responseData, job, port, DateTime.Now));

                            // Readjust sleep time according to Step number
                            switch (responseData)
                            {
                                case "Step 1 in process.":
                                    ModelerCurrentStepState = ModelerStepState.STEP_1;
                                    adjustableSleepTime = 15000;
                                    break;

                                case "Step 2 in process.":
                                    ModelerCurrentStepState = ModelerStepState.STEP_2;
                                    adjustableSleepTime = 10000;
                                    break;

                                case "Step 3 in process.":
                                    ModelerCurrentStepState = ModelerStepState.STEP_3;
                                    adjustableSleepTime = 7500;
                                    break;

                                case "Step 4 in process.":
                                    ModelerCurrentStepState = ModelerStepState.STEP_4;
                                    adjustableSleepTime = 5000;
                                    break;

                                case "Step 5 in process.":
                                    ModelerCurrentStepState = ModelerStepState.STEP_5;
                                    adjustableSleepTime = 2500;
                                    break;

                                case "Step 6 in process.":
                                    ModelerCurrentStepState = ModelerStepState.STEP_6;
                                    adjustableSleepTime = 250;
                                    break;

                                case "Whole process done, socket closed.":
                                    ModelerCurrentStepState = ModelerStepState.STEP_COMPLETE;

                                    StaticClass.Log(String.Format("TCP/IP for Job {0} on Port {1} received Modeler process done at {2:HH:mm:ss.fff}",
                                        job, port, DateTime.Now));

                                    // Make sure to close TCP/IP socket
                                    stream.Close();
                                    client.Close();

                                    StaticClass.Log(String.Format("Closed TCP/IP Socket for Job {0} on Port {1} at {2:HH:mm:ss.fff}",
                                        job, port, DateTime.Now));

                                    // Set the TCP/IP Scan complete flag to signal the RunJob thread
                                    StaticClass.TcpIpScanComplete[job] = true;
                                    return;

                                default:
                                    StaticClass.Logger.LogWarning("Received Weird Response: {0} from Job {1} on Port {2} at {3:HH:mm:ss.fff}",
                                        responseData, job, port, DateTime.Now);
                                    break;
                            }

                            // Backup check of the process complete string, even if it is concatenated with another string
                            if (responseData.Contains("Whole process done, socket closed."))
                            {
                                ModelerCurrentStepState = ModelerStepState.STEP_COMPLETE;

                                StaticClass.Log(String.Format("TCP/IP for Job {0} on Port {1} received Modeler process complete at {2:HH:mm:ss.fff}",
                                    job, port, DateTime.Now));

                                // Make sure to close TCP/IP socket
                                stream.Close();
                                client.Close();

                                StaticClass.Log(String.Format("Closed TCP/IP Socket for Job {0} on Port {1} at {2:HH:mm:ss.fff}",
                                    job, port, DateTime.Now));

                                // Set the TCP/IP Scan complete flag to signal the RunJob thread
                                StaticClass.TcpIpScanComplete[job] = true;
                                return;
                            }

                            // Check for Job timeout
                            if ((DateTime.Now - monitorData.StartTime).TotalSeconds > StaticClass.MaxJobTimeLimitSeconds)
                            {
                                StaticClass.Log(String.Format("Job Timeout for Job {0} in state {1} at {2:HH:mm:ss.fff}",
                                    ModelerCurrentStepState, job, DateTime.Now));

                                // Create job Timeout status
                                StaticClass.StatusDataEntry(statusData, job, iniData, JobStatus.JOB_TIMEOUT, JobType.TIME_START);

                                // Set all flags to complete job Process
                                StaticClass.ProcessingJobScanComplete[job] = true;
                                StaticClass.TcpIpScanComplete[job] = true;
                                StaticClass.ProcessingFileScanComplete[job] = true;

                                // Wait a sec then shutdown the Modeler after job timeout
                                StaticClass.ProcessHandles[job].Kill();
                                Thread.Sleep(StaticClass.KILL_PROCESS_WAIT);

                                // Make sure to close TCP/IP socket
                                jobComplete = true;
                            }

                            // Check if the shutdown flag is set, then exit method
                            if (StaticClass.ShutdownFlag == true)
                            {
                                StaticClass.Log(String.Format("\nShutdown TcpIpListenThread Connect for Job {0} in state {1} on Port {2} at {3:HH:mm:ss.fff}",
                                    job, ModelerCurrentStepState, port, DateTime.Now));

                                StaticClass.TcpIpScanComplete[job] = true;

                                // Make sure to close TCP/IP socket
                                jobComplete = true;
                            }

                            // Check if the pause flag is set, then wait for reset
                            if (StaticClass.PauseFlag == true)
                            {
                                StaticClass.Log(String.Format("TcpIpListenThread Connect3 is in Pause mode in state {0} at {1:HH:mm:ss.fff}",
                                    ModelerCurrentStepState, DateTime.Now));
                                do
                                {
                                    Thread.Yield();
                                }
                                while (StaticClass.PauseFlag == true);
                            }

                            messageReceived = true;
                        }
                        else
                        {
                            Thread.Sleep(StaticClass.READ_AVAILABLE_RETRY_DELAY);
                            tcpIpRetryCount++;
                        }
                    }
                    while ((tcpIpRetryCount < StaticClass.NUM_TCP_IP_RETRIES) && (messageReceived == false));

                    // Wait for an adjustable time between TCP/IP status requests
                    Thread.Sleep(adjustableSleepTime);
                }
                while (jobComplete == false);

                // Close everything
                stream.Close();
                client.Close();

                StaticClass.Log(String.Format("Closed TCP/IP Socket for Job {0} on Port {1} in state {2} at {3:HH:mm:ss.fff}",
                    job, port, ModelerCurrentStepState, DateTime.Now));
            }
            catch (SocketException e)
            {
                StaticClass.Log(String.Format("SocketException {0} for Job {1} on Port {2} in state {3} at {4:HH:mm:ss.fff}",
                    e, job, port, ModelerCurrentStepState, DateTime.Now));

                StaticClass.Logger.LogError(String.Format("SocketException {0} for Job {1} on Port {2} in state {3} at {4:HH:mm:ss.fff}",
                    e, job, port, ModelerCurrentStepState, DateTime.Now));
            }
        }
    }
}