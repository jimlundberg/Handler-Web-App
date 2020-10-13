using Microsoft.Extensions.Logging;
using Status.Models;
using System;
using System.IO;
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
                    StaticClass.JobShutdownFlag[job] = true;
                    return;
                }

                // Show connection and start sending messages
                StaticClass.Log(string.Format("Connected to Modeler TCP/IP for Job {0} on Port {1} at {2:HH:mm:ss.fff}", job, port, DateTime.Now));
                bool jobComplete = false;
                int numRequestSent = 0;
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

                        // Set the shutdown flag to signal the RunJob thread
                        StaticClass.JobShutdownFlag[job] = true;
                        return;
                    }

                    // Check if the stream is writable, if not, shutdown and exit
                    if (stream.CanWrite)
                    {
                        if (StaticClass.ShutDownPauseCheck("TCP/IP before write") == false)
                        {
                            try
                            {
                                // Translate the passed message into ASCII and send it as a byte array
                                byte[] sendData = Encoding.ASCII.GetBytes(StatusMessage);
                                stream.Write(sendData, 0, sendData.Length);
                                numRequestSent++;
                            }
                            catch (IOException e)
                            {
                                // Make sure to close TCP/IP socket
                                stream.Close();
                                client.Close();

                                StaticClass.Log(string.Format("Closed TCP/IP Socket for Job {0} on Port {1} because of Exception {2} at {3:HH:mm:ss.fff}",
                                    job, port, e, DateTime.Now));

                                // Signal job complete if exception happened in Step 6
                                if (ModelerCurrentStepState == ModelerStepState.STEP_6)
                                {
                                    StaticClass.Log(string.Format("No Job Complete for Job {0} but it is in Step 6 on Port {1} at {2:HH:mm:ss.fff}",
                                        job, port, DateTime.Now));

                                    // Set the TCP/IP Scan complete flag to signal the RunJob thread
                                    StaticClass.TcpIpScanComplete[job] = true;
                                }
                                else
                                {
                                    // Set the Shutdown flag to signal the RunJob thread
                                    StaticClass.JobShutdownFlag[job] = true;
                                }

                                return;
                            }

                            StaticClass.Log(string.Format("\nSending {0} msg to Modeler for Job {1} on Port {2} at {3:HH:mm:ss.fff}",
                                StatusMessage, job, port, DateTime.Now));
                        }
                    }
                    else // Can not write to stream
                    {
                        // Make sure to close TCP/IP socket
                        stream.Close();
                        client.Close();

                        StaticClass.Log(string.Format("\nTCP/IP stream can not be written to for Modeler Job {0} on Port {1} at {2:HH:mm:ss.fff}",
                            job, port, DateTime.Now));

                        // Set the Shutdown flag to signal the RunJob thread
                        StaticClass.JobShutdownFlag[job] = true;
                        return;
                    }

                    // Check if TCP/IP stream is readable and data is available
                    int adjustableSleepTime = StaticClass.STARTING_TCP_IP_WAIT;
                    int numOfRetries = 1;
                    bool messageReceived = false;
                    do
                    {
                        if (stream.CanRead && stream.DataAvailable)
                        {
                            // Buffers to store the response
                            byte[] recvData = new byte[256];
                            string responseData = string.Empty;
                            int bytes = stream.Read(recvData, 0, recvData.Length);
                            responseData = Encoding.ASCII.GetString(recvData, 0, bytes);

                            StaticClass.Log(string.Format("Received: {0} from Job {1} on Port {2} at {3:HH:mm:ss.fff}",
                                responseData, job, port, DateTime.Now));

                            // Readjust sleep time according to Step number
                            switch (responseData)
                            {
                                case "Step 1 in process.":
                                    ModelerCurrentStepState = ModelerStepState.STEP_1;
                                    if (numRequestSent < StaticClass.NUM_REQUESTS_TILL_TCPIP_SLOWDOWN)
                                    {
                                        adjustableSleepTime = 15000;
                                    }
                                    else
                                    {
                                        adjustableSleepTime = 60000;
                                    }
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
                                    // Make sure to close TCP/IP socket
                                    stream.Close();
                                    client.Close();

                                    StaticClass.Log(string.Format("TCP/IP for Job {0} on Port {1} received Modeler process done at {2:HH:mm:ss.fff}",
                                        job, port, DateTime.Now));

                                    ModelerCurrentStepState = ModelerStepState.STEP_COMPLETE;

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
                                // Make sure to close TCP/IP socket
                                stream.Close();
                                client.Close();

                                StaticClass.Log(string.Format("TCP/IP for Job {0} on Port {1} received Modeler process complete at {2:HH:mm:ss.fff}",
                                    job, port, DateTime.Now));

                                ModelerCurrentStepState = ModelerStepState.STEP_COMPLETE;

                                StaticClass.Log(string.Format("Closed TCP/IP Socket for Job {0} on Port {1} at {2:HH:mm:ss.fff}",
                                    job, port, DateTime.Now));

                                // Set the TCP/IP Scan complete flag to signal the RunJob thread
                                StaticClass.TcpIpScanComplete[job] = true;
                                return;
                            }

                            // Check for Job timeout
                            if ((DateTime.Now - monitorData.StartTime).TotalSeconds > StaticClass.MaxJobTimeLimitSeconds)
                            {
                                StaticClass.Log(string.Format("Job Execution Timeout for Job {0} in state {1} at {2:HH:mm:ss.fff}",
                                    job, ModelerCurrentStepState, DateTime.Now));

                                // Make sure to close TCP/IP socket
                                stream.Close();
                                client.Close();

                                StaticClass.Log(string.Format("Closed TCP/IP Socket for Job {0} on Port {1} at {2:HH:mm:ss.fff}",
                                    job, port, DateTime.Now));

                                // Create job Timeout status
                                StaticClass.StatusDataEntry(job, JobStatus.JOB_TIMEOUT, JobType.TIME_START);

                                // Set the Shutdown flag to signal the RunJob thread
                                StaticClass.JobShutdownFlag[job] = true;
                                return;
                            }

                            // Check for shutdown or pause
                            if (StaticClass.ShutDownPauseCheck("TCP/IP Receive") == true)
                            {
                                // Make sure to close TCP/IP socket
                                stream.Close();
                                client.Close();

                                // Set the Shutdown flag to signal the RunJob thread
                                StaticClass.JobShutdownFlag[job] = true;
                                return;
                            }

                            // Set the messge received flag to exit receive loop
                            messageReceived = true;
                        }
                        else
                        {
                            // Wait 250 msec between 480 read retry checks for about 2 min CanRead is set for session
                            Thread.Sleep(StaticClass.READ_AVAILABLE_RETRY_DELAY);

                            // Display TCP/IP read retries if TCP_IP debug flag set
                            if ((StaticClass.IniData.DebugMode & (byte)DebugModeState.TCP_IP) != 0)
                            {
                                  StaticClass.Log(string.Format("TCP/IP read retry {0} in {1} for Job {2} on Port {3} at {4:HH:mm:ss.fff}",
                                    numOfRetries, ModelerCurrentStepState, job, port, DateTime.Now));
                            }
                        }
                    }
                    while ((numOfRetries++ < StaticClass.NUM_TCP_IP_RETRIES) && (messageReceived == false));

                    // Check if retry count exceeded in STEP 5 or 6, if so, force TCP/IP Complete
                    if ((numOfRetries >= StaticClass.NUM_TCP_IP_RETRIES) &&
                       ((ModelerCurrentStepState == ModelerStepState.STEP_5) || (ModelerCurrentStepState == ModelerStepState.STEP_6)))
                    {
                        // Make sure to close TCP/IP socket
                        stream.Close();
                        client.Close();

                        StaticClass.Log(string.Format("Closed TCP/IP Socket for Job {0} on Port {1} because Retry count exceeded {2} at {3:HH:mm:ss.fff}",
                            job, port, StaticClass.NUM_TCP_IP_RETRIES, DateTime.Now));

                        StaticClass.Log(string.Format("No Job Complete received for Job {0} but it is in Step 6 so forcing complete on Port {1} at {2:HH:mm:ss.fff}",
                            job, port, DateTime.Now));

                        // Set the TCP/IP Scan complete flag to signal the RunJob thread
                        StaticClass.TcpIpScanComplete[job] = true;
                        return;
                    }

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
