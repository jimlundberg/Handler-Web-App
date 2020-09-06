using Microsoft.Extensions.Logging;
using Status.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Timers;

namespace Status.Services
{
    /// <summary>
    /// TCP/IP Thread Class
    /// </summary>
    public class TcpIpListenThread
    {
        private readonly IniFileData IniData;
        private readonly StatusMonitorData MonitorData;
        private readonly List<StatusData> StatusData;
        public event EventHandler ProcessCompleted;
        public const string Server = "127.0.0.1";
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
            StaticClass.TcpIpListenThreadHandle = new Thread(() =>
                Connect(Port, Server, IniData, MonitorData, StatusData));

            if (StaticClass.TcpIpListenThreadHandle == null)
            {
                StaticClass.Logger.LogError("TcpIpListenThread thread failed to instantiate");
            }
            StaticClass.TcpIpListenThreadHandle.Start();
        }

        static void retryTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            StaticClass.Log(String.Format("\nSending Retry msg {0} to Modeler for Job {1} on Port {2} at {3:HH:mm:ss.fff}",
                Message, Job, Port, DateTime.Now));

            // Send retry message to the Modeler
            Byte[] data;
            data = System.Text.Encoding.ASCII.GetBytes(Message);
            StreamHandle.Write(data, 0, data.Length);
        }

        /// <summary>
        /// Connect to TCP/IP Port 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></param>
        /// <param name="statusData"></param>
        public void Connect(int port, string server, IniFileData iniData, StatusMonitorData monitorData, List<StatusData> statusData)
        {
            // Wait about a minute for the Modeler to start execution
            Thread.Sleep(60000);

            string job = monitorData.Job;

            StaticClass.Log(String.Format("\nStarting TCP/IP Scan for Job {0} on Port {1} at {2:HH:mm:ss.fff}",
                job, port, DateTime.Now));

            // Log starting TCP/IP monitoring entry
            StaticClass.StatusDataEntry(statusData, job, iniData, JobStatus.MONITORING_TCPIP, JobType.TIME_START);

            TcpListener tcpListener = null;
            try
            {
                tcpListener = new TcpListener(IPAddress.Parse(Server), Port);

                // Start listening for client requests
                tcpListener.Start();

                // Buffer for reading data
                Byte[] bytes = new Byte[256];
                String messageReceived = null;

                // Enter the listening loop
                Console.Write("\nWaiting for a connection...\n");

                // Perform a blocking call to accept requests
                TcpClient client = tcpListener.AcceptTcpClient();
                Console.WriteLine("Connected!");

                // Get a stream object for reading and writing
                NetworkStream stream = client.GetStream();

                // Store for retries
                StreamHandle = stream;
                Port = port;
                Job = job;

                StaticClass.Log(String.Format("\nSending {0} msg to Modeler for Job {1} on Port {2} at {3:HH:mm:ss.fff}",
                    Message, job, port, DateTime.Now));

                // Send the message to the Modeler
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(Message);
                stream.Write(data, 0, data.Length);

                // Loop to receive all the data sent by the client
                int i = 0;
                bool connectionComplete = false;
                int adjustableSleepTime = 15000;
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    // Translate data bytes to a ASCII string
                    messageReceived = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                    Console.WriteLine("Received: {0}", messageReceived);

                    // Display message received and set the adjustable sleep time
                    if (messageReceived == "status")
                    {
                        // Readjust sleep time according to Step number
                        switch (messageReceived)
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
                                adjustableSleepTime = 2500;
                                break;

                            case "Whole process done, socket closed.":
                                // Received process complete
                                StaticClass.Log(String.Format("TCP/IP for Job {0} on Port {1} received Modeler process done at {2:HH:mm:ss.fff}",
                                    job, port, DateTime.Now));

                                StaticClass.Log(String.Format("Closing TCP/IP Socket for Job {0} on Port {1} at {2:HH:mm:ss.fff}",
                                    job, port, DateTime.Now));

                                // Flag TCP/IP scan complete
                                StaticClass.TcpIpScanComplete[job] = true;

                                // Make sure to close TCP/IP socket
                                connectionComplete = true;
                                break;

                            default:
                                StaticClass.Logger.LogWarning("Received Weird Response: {0} from Job {1} on Port {2} at {3:HH:mm:ss.fff}",
                                    messageReceived, job, port, DateTime.Now);
                                break;
                        }

                        // Backup check of the process complete string, even if it is concatenated with another string
                        if (messageReceived.Contains("Whole process done, socket closed."))
                        {
                            // Received process complete
                            StaticClass.Log(String.Format("TCP/IP for Job {0} on Port {1} received Modeler process complete at {2:HH:mm:ss.fff}",
                                job, port, DateTime.Now));

                            StaticClass.Log(String.Format("Closing TCP/IP Socket for Job {0} on Port {1} at {2:HH:mm:ss.fff}",
                                job, port, DateTime.Now));

                            // Flag TCP/IP scan complete
                            StaticClass.TcpIpScanComplete[job] = true;

                            // Make sure to close TCP/IP socket
                            connectionComplete = true;
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

                            // Wait a bit then shutdown the Modeler after completing job
                            Thread.Sleep(1000);
                            StaticClass.ProcessHandles[job].Kill();

                            // Make sure to close TCP/IP socket
                            connectionComplete = true;
                        }

                        // Check if the shutdown flag is set, then exit method
                        if (StaticClass.ShutdownFlag == true)
                        {
                            StaticClass.Log(String.Format("\nShutdown TcpIpListenThread Connect for Job {0} on Port {1} at {2:HH:mm:ss.fff}",
                                job, port, DateTime.Now));

                            StaticClass.TcpIpScanComplete[job] = true;

                            // Make sure to close TCP/IP socket
                            connectionComplete = true;
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
                    while (connectionComplete == false);

                    // Shutdown and end connection
                    client.Close();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                // Stop listening for new clients
                tcpListener.Stop();
            }

            Console.WriteLine("\nHit enter to continue...");
            Console.Read();
        }
    }
}

