using StatusModels;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Timers;

namespace Status.Services
{
    /// <summary>
    /// TCP/IP Thread Class
    /// </summary>
    public class TcpIpThread
    {
        // State information used in the task.
        static public IniFileData IniData;
        static public StatusMonitorData MonitorData;
        static public List<StatusWrapper.StatusData> StatusData;

        // The constructor obtains the state information.
        public TcpIpThread(IniFileData iniData, StatusMonitorData monitorData, List<StatusWrapper.StatusData> statusData)
        {
            IniData = iniData;
            MonitorData = monitorData;
            StatusData = statusData;
        }

        // The thread procedure performs the task
        public void ThreadProc()
        {
            TcpIpMonitor(MonitorData.JobPortNumber);
        }

        public void StatusEntry(List<StatusWrapper.StatusData> statusList, String job, JobStatus status, JobType timeSlot)
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

        public void TcpIpMonitor(int TcpIpPortNumber)
        {
            TcpIpConnection tcpIpConnection = new TcpIpConnection();
            TcpIpConnection.PortNumber = MonitorData.JobPortNumber;
            TcpIpConnection.SetTimer();
            tcpIpConnection.Connect("127.0.0.1", TcpIpPortNumber, "status");
            TcpIpConnection.aTimer.Stop();
            TcpIpConnection.aTimer.Dispose();
        }

        /// <summary>
        /// Class to monitor and report status the TCP/IP connection to the Monitor application that is executing 
        /// </summary>
        public class TcpIpConnection
        {
            public static System.Timers.Timer aTimer;
            static public int PortNumber;

            /// <summary>
            /// Connect to TCP/IP Port
            /// </summary>
            /// <param name="server"></param>
            /// <param name="port"></param>
            /// <param name="message"></param>
            public void Connect(String server, Int32 port, String message)
            {
                try
                {
                    // Set current port number
                    PortNumber = port;

                    // Create a TcpClient.
                    // Note, for this client to work you need to have a TcpServer
                    // connected to the same address as specified by the server, port combination.
                    TcpClient client = new TcpClient(server, PortNumber);

                    // Translate the passed message into ASCII and store it as a Byte array.
                    Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);

                    // Get a client stream for reading and writing.
                    // Stream stream = client.GetStream();
                    NetworkStream stream = client.GetStream();

                    bool jobComplete = false;
                    int sleepTime = 15000;
                    do
                    {
                        // Send the message to the connected TcpServer.
                        stream.Write(data, 0, data.Length);

                        // Receive the TcpServer.response.
                        Console.WriteLine("Querying Modeler for Job {0} on port {1} at {2:HH:mm:ss.fff}", MonitorData.Job, PortNumber, DateTime.Now);
                        Console.WriteLine("Senting: {0}", message);

                        // Buffer to store the response bytes.
                        data = new Byte[256];

                        // String to store the response ASCII representation.
                        String responseData = String.Empty;

                        // Read the first batch of the TcpServer response bytes.
                        Int32 bytes = stream.Read(data, 0, data.Length);
                        responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);

                        // Send status for response received
                        switch (responseData)
                        {
                            case "Step 1 in process.":
                            case "Step 2 in process.":
                            case "Step 3 in process.":
                            case "Step 4 in process.":
                                Console.WriteLine("Received: {0} from Job {1}", responseData, MonitorData.Job);
                                break;

                            case "Step 5 in process.":
                            case "Step 6 in process.":
                                Console.WriteLine("Received: {0} from Job {1}", responseData, MonitorData.Job);
                                sleepTime = 1000;
                                break;

                            case "Whole process done, socket closed.":
                                Console.WriteLine("Received: {0} from Job {1}", responseData, MonitorData.Job);
                                jobComplete = true;
                                break;

                            default:
                                Console.WriteLine("$$$$$Received Weird Response: {0} from Job {1}", responseData, MonitorData.Job);
                                break;
                        }

                        Thread.Sleep(sleepTime);
                    }
                    while (jobComplete == false);

                    Console.WriteLine("Exiting TCP/IP Scan of Job {0}", MonitorData.Job);

                    // Close everything.
                    stream.Close();
                    client.Close();
                }
                catch (ArgumentNullException e)
                {
                    Console.WriteLine("ArgumentNullException: {0}", e);
                }
                catch (SocketException e)
                {
                    Console.WriteLine("SocketException: {0}", e);
                }

                Console.WriteLine("\n Press Enter to exit...");
                Console.Read();
            }

            public static void SetTimer()
            {
                // Create a timer with a five second interval.
                aTimer = new System.Timers.Timer(15000);

                // Hook up the Elapsed event for the timer. 
                aTimer.Elapsed += OnTimedEvent;
                aTimer.AutoReset = true;
                aTimer.Enabled = true;
            }

            public static void OnTimedEvent(Object source, ElapsedEventArgs e)
            {
                // Check modeler status
                Console.ReadLine();
            }
        }
    }
}
