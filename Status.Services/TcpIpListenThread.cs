using Microsoft.Extensions.Logging;
using Status.Models;
using System;
using System.IO;
using System.Net;
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
        private readonly StatusMonitorData MonitorData;
        public event EventHandler ProcessCompleted;
        private static readonly string Server = "127.0.0.1";
        private static readonly string StatusMessage = "status";
        private readonly int Port = 0;

        /// <summary>
        /// Job Tcp/IP thread 
        /// </summary>
        /// <param name="monitorData"></param>
        public TcpIpListenThread(StatusMonitorData monitorData)
        {
            Port = monitorData.JobPortNumber;
            MonitorData = monitorData;
        }

        /// <summary>
        /// TCP/IP Listen thread default destructor
        /// </summary>
        ~TcpIpListenThread()
        {
            StaticClass.Logger.LogInformation("TcpIpListenThread default destructor called");
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
            StaticClass.TcpListenerThreadHandle = new Thread(() => Connect(Port, Server, MonitorData));
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
        /// <param name="server"></param>
        /// <param name="monitorData"></pram>
        public void Connect(int port, string server, StatusMonitorData monitorData)
        {
            ModelerStepState ModelerCurrentStepState = ModelerStepState.NONE;
            string job = monitorData.Job;

            // Wait about a minute for the Modeler to start execution
            Thread.Sleep(StaticClass.TCP_IP_STARTUP_WAIT);

            StaticClass.Log(string.Format("\nStarting TCP/IP Scan for Job {0} on Port {1} at {2:HH:mm:ss.fff}",
                job, port, DateTime.Now));

            // Log starting TCP/IP monitoring entry
            StaticClass.StatusDataEntry(job, JobStatus.MONITORING_TCPIP, JobType.TIME_START);

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

                // Set the Security protocol
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

                // Show connection and start sending messages
                StaticClass.Log(string.Format("Connected to Modeler TCP/IP for Job {0} on Port {1} at {2:HH:mm:ss.fff}", job, port, DateTime.Now));
                bool jobComplete = false;
                do
                {
                    // Loop shutdown/Pause check
                    if (StaticClass.ShutDownPauseCheck("TCP/IP Connect") == true)
                    {
                        // Make sure to close TCP/IP socket
                        stream.Close();
                        client.Close();

                        StaticClass.Log(string.Format("Closed TCP/IP Socket for Job {0} on Port {1} at {2:HH:mm:ss.fff}",
                            job, port, DateTime.Now));

                        // Set the TCP/IP Scan complete flag to signal the RunJob thread
                        StaticClass.TcpIpScanComplete[job] = true;
                        return;
                    }

                    // Check if the stream is writable, if not, exit
                    if (stream.CanWrite)
                    {
                        if (StaticClass.ShutDownPauseCheck(job) == false)
                        {
                            try
                            {
                                // Translate the passed message into ASCII and store it as a Byte array.
                                Byte[] sendData = Encoding.ASCII.GetBytes(StatusMessage);
                                stream.Write(sendData, 0, sendData.Length);
                            }
                            catch (IOException e)
                            {
                                // Make sure to close TCP/IP socket
                                stream.Close();
                                client.Close();

                                StaticClass.Log(string.Format("Closed TCP/IP Socket for Job {0} on Port {1} because of Exception {2} at {3:HH:mm:ss.fff}",
                                    job, port, e, DateTime.Now));

                                // Signal job complete if exception happend in Step 5 or 6
                                if ((ModelerCurrentStepState == ModelerStepState.STEP_5) ||
                                    (ModelerCurrentStepState == ModelerStepState.STEP_6))
                                {
                                    // Set the TCP/IP Scan complete flag to signal the RunJob thread
                                    StaticClass.TcpIpScanComplete[job] = true;
                                }

                                return;
                            }

                            StaticClass.Log(string.Format("\nSending {0} msg to Modeler for Job {1} on Port {2} at {3:HH:mm:ss.fff}",
                                StatusMessage, job, port, DateTime.Now));
                        }
                    }
                    else
                    {
                        StaticClass.Log(string.Format("\nTCP/IP stream closed for Modeler Job {0} on Port {1} at {2:HH:mm:ss.fff}",
                            job, port, DateTime.Now));
                      
                        // Make sure to close TCP/IP socket
                        stream.Close();
                        client.Close();

                        StaticClass.Log(string.Format("Closed TCP/IP Socket for Job {0} on Port {1} at {2:HH:mm:ss.fff}",
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

                            StaticClass.Log(string.Format("Received: {0} from Job {1} on Port {2} at {3:HH:mm:ss.fff}",
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

                                    StaticClass.Log(string.Format("TCP/IP for Job {0} on Port {1} received Modeler process done at {2:HH:mm:ss.fff}",
                                        job, port, DateTime.Now));

                                    // Make sure to close TCP/IP socket
                                    stream.Close();
                                    client.Close();

                                    StaticClass.Log(string.Format("Closed TCP/IP Socket for Job {0} on Port {1} at {2:HH:mm:ss.fff}",
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

                                StaticClass.Log(string.Format("TCP/IP for Job {0} on Port {1} received Modeler process complete at {2:HH:mm:ss.fff}",
                                    job, port, DateTime.Now));

                                // Make sure to close TCP/IP socket
                                stream.Close();
                                client.Close();

                                StaticClass.Log(string.Format("Closed TCP/IP Socket for Job {0} on Port {1} at {2:HH:mm:ss.fff}",
                                    job, port, DateTime.Now));

                                // Set the TCP/IP Scan complete flag to signal the RunJob thread
                                StaticClass.TcpIpScanComplete[job] = true;
                                return;
                            }

                            // Check for Job timeout
                            if ((DateTime.Now - monitorData.StartTime).TotalSeconds > StaticClass.MaxJobTimeLimitSeconds)
                            {
                                StaticClass.Log(string.Format("Job Timeout for Job {0} in state {1} at {2:HH:mm:ss.fff}",
                                    ModelerCurrentStepState, job, DateTime.Now));

                                // Create job Timeout status
                                StaticClass.StatusDataEntry(job, JobStatus.JOB_TIMEOUT, JobType.TIME_START);

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

                            // Check for shutdown or pause
                            if (StaticClass.ShutDownPauseCheck("TCP/IP Receive") == true)
                            {
                                StaticClass.Log(string.Format("\nShutdown TcpIpListenThread for Job {0} in state {1} on Port {2} at {3:HH:mm:ss.fff}",
                                    job, ModelerCurrentStepState, port, DateTime.Now));

                                // Make sure to close TCP/IP socket
                                stream.Close();
                                client.Close();

                                StaticClass.TcpIpScanComplete[job] = true;

                                // Make sure to close TCP/IP socket
                                jobComplete = true;
                            }

                            // Set the messge received flag to exit receive loop
                            messageReceived = true;
                        }
                        else
                        {
                            // Wait 250 msec between 480 Data Available checks (2 min) CanRead is set for session
                            Thread.Sleep(StaticClass.READ_AVAILABLE_RETRY_DELAY);
                            tcpIpRetryCount++;
                        }

                        if (StaticClass.ShutDownPauseCheck("TCP/IP Receive") == true)
                        {
                            StaticClass.Log(string.Format("\nShutdown TcpIpListenThread for Job {0} in state {1} on Port {2} at {3:HH:mm:ss.fff}",
                                job, ModelerCurrentStepState, port, DateTime.Now));

                            // Make sure to close TCP/IP socket
                            stream.Close();
                            client.Close();

                            StaticClass.TcpIpScanComplete[job] = true;

                            // Make sure to close TCP/IP socket
                            jobComplete = true;
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

                StaticClass.Log(string.Format("Closed TCP/IP Socket for Job {0} on Port {1} in state {2} at {3:HH:mm:ss.fff}",
                    job, port, ModelerCurrentStepState, DateTime.Now));
            }
            catch (SocketException e)
            {
                StaticClass.Logger.LogError(string.Format("SocketException {0} for Job {1} on Port {2} in state {3} at {4:HH:mm:ss.fff}",
                    e, job, port, ModelerCurrentStepState, DateTime.Now));
            }
        }
    }
}
