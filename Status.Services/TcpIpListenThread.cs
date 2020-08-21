using Microsoft.Extensions.Logging;
using StatusModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Status.Services
{
    /// <summary>
    /// TCP/IP Thread Class
    /// </summary>
    public class TcpIpListenThread
    {
        public static IniFileData IniData;
        public static StatusMonitorData MonitorData;
        public static List<StatusData> StatusData;
        private static Thread thread;
        public event EventHandler ProcessCompleted;
        public static ILogger<StatusRepository> Logger;
        public const string Host = "127.0.0.1";
        public const int Port = 3000;

        /// <summary>
        /// Job Tcp/IP thread 
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public TcpIpListenThread(IniFileData iniData, StatusMonitorData monitorData, List<StatusData> statusData, ILogger<StatusRepository> logger)
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
            thread = new Thread(() => Connect(Host, IniData, MonitorData, StatusData, "status", Logger));
            thread.Start();
        }

        // Status Data Entry
        /// <summary>
        /// Status Data Entry Method
        /// </summary>
        /// <param name="statusList"></param>
        /// <param name="job"></param>
        /// <param name="iniData"></param>
        /// <param name="status"></param>
        /// <param name="timeSlot"></param>
        /// <param name="logFileName"></param>
        /// <param name="logger"></param>
        public static void StatusDataEntry(List<StatusData> statusList, string job, IniFileData iniData, JobStatus status,
            JobType timeSlot, string logFileName, ILogger<StatusRepository> logger)
        {
            StatusEntry statusData = new StatusEntry(statusList, job, status, timeSlot, logFileName, logger);
            statusData.ListStatus(iniData, statusList, job, status, timeSlot);
            statusData.WriteToCsvFile(job, iniData, status, timeSlot, logFileName, logger);
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
            StaticClass.Log(IniData.ProcessLogFile,
                String.Format("Status: Job:{0} Job Status:{1}", job, status));
        }

        /// <summary>
        /// Job timeout handler
        /// </summary>
        /// <param name="job"></param>
        /// <param name="monitorData"></param>
        /// <param name="iniData"></param>
        /// <param name="logger"></param>
        public static void TimeoutHandler(string job, IniFileData iniData, ILogger<StatusRepository> logger)
        {
            Console.WriteLine(String.Format("Timeout Handler for job {0}", job));

            // Get job name from directory name
            string processingBufferDirectory = iniData.ProcessingDir + @"\" + job;
            string repositoryDirectory = iniData.RepositoryDir + @"\" + job;

            // If the repository directory does not exist, create it
            if (!Directory.Exists(repositoryDirectory))
            {
                Directory.CreateDirectory(repositoryDirectory);
            }

            // Move Processing Buffer Files to the Repository directory when failed
            FileHandling.CopyFolderContents(processingBufferDirectory, repositoryDirectory, logger, true, true);

            // Remove job from Input jobs to run list and decrement execution count
            StaticClass.NewInputJobsToRun.Remove(job);
        }

        /// <summary>
        /// Connect to TCP/IP Port 
        /// </summary>
        /// <param name="server"></param>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></param>
        /// <param name="statusData"></param>
        /// <param name="message"></param>
        /// <param name="logger"></param>
        public void Connect(string port, IniFileData iniData, StatusMonitorData monitorData,
            List<StatusData> statusData, string message, ILogger<StatusRepository> logger)
        {
            // Wait about a minute for the Modeler to start execution
            Thread.Sleep(iniData.ScanTime * 12);

            Console.WriteLine("\nStarting Tcp/Ip Scan for job {0} on port {1} at {2:HH:mm:ss.fff}",
                monitorData.Job, monitorData.JobPortNumber, DateTime.Now);

            try
            {
                string job = monitorData.Job;

                // Log Tcp/Ip monitoring entry
                StatusDataEntry(statusData, job, iniData, JobStatus.MONITORING_TCPIP, JobType.TIME_START, iniData.StatusLogFile, logger);

                // Create a TcpClient.
                // Note, for this client to work you need to have a TcpServer
                // connected to the same address as specified by the server, port combination.
                TcpClient client = new TcpClient(port, monitorData.JobPortNumber);
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
                    logger.LogError("TcpIp Connectinon stream handle was not gotten from client");
                }

                bool jobComplete = false;
                int adjustableSleepTime = iniData.ScanTime * 3;
                do
                {
                    // Send the message to the Modeler
                    stream.Write(data, 0, data.Length);

                    // Receive the TcpServer.response.
                    StaticClass.Log(iniData.ProcessLogFile,
                        String.Format("\nSending {0} msg to Modeler for Job {1} on port {2} at {3:HH:mm:ss.fff}",
                        message, job, monitorData.JobPortNumber, DateTime.Now));

                    // Buffer to store the response bytes.
                    data = new Byte[256];

                    // String to store the response ASCII representation.
                    string responseData = String.Empty;

                    // Try to read the Modeler response at least 5 times
                    if (stream.CanRead)
                    {
                        int bytes = 0;
                        for (int i = 0; i < 5; i++)
                        {
                            try
                            {
                                bytes = stream.Read(data, 0, data.Length);
                                if (bytes > 0)
                                {
                                    break;
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(String.Format("Tcp/Ip Read failed with error {0}", e.ToString()));
                            }

                            if (i == 4)
                            {
                                Console.WriteLine("Tcp/Ip Connection Timeout after 5 tries");
                                return;
                            }

                            Thread.Sleep(IniData.ScanTime);
                        }

                        responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);

                        if (responseData.Contains("Whole process done, socket closed."))
                        {
                            StaticClass.Log(iniData.ProcessLogFile,
                                String.Format("Received: {0} from Job {1} on port {2} at {3:HH:mm:ss.fff}",
                                responseData, monitorData.Job, monitorData.JobPortNumber, DateTime.Now));
                            StaticClass.TcpIpScanComplete[job] = true;
                            jobComplete = true;
                            return;
                        }

                        // Send status for response received
                        switch (responseData)
                        {
                            case "Step 1 in process.":
                            case "Step 2 in process.":
                            case "Step 3 in process.":
                            case "Step 4 in process.":
                                StaticClass.Log(iniData.ProcessLogFile,
                                    String.Format("Received: {0} from Job {1} on port {2} at {3:HH:mm:ss.fff}",
                                     responseData, monitorData.Job, monitorData.JobPortNumber, DateTime.Now));
                                break;

                            case "Step 5 in process.":
                                StaticClass.Log(iniData.ProcessLogFile,
                                    String.Format("Received: {0} from Job {1} on port {2} at {3:HH:mm:ss.fff}",
                                    responseData, monitorData.Job, monitorData.JobPortNumber, DateTime.Now));
                                adjustableSleepTime = 1000;
                                break;

                            case "Step 6 in process.":
                                StaticClass.Log(iniData.ProcessLogFile,
                                    String.Format("Received: {0} from Job {1} on port {2} at {3:HH:mm:ss.fff}",
                                    responseData, monitorData.Job, monitorData.JobPortNumber, DateTime.Now));
                                adjustableSleepTime = 100;
                                break;

                            default:
                                logger.LogWarning("Received Weird Response: {0} from Job {1} on port {2} at {32qw111:mm:ss.fff}",
                                    responseData, monitorData.Job, monitorData.JobPortNumber, DateTime.Now);
                                break;
                        }

                        // Check for job timeout
                        if ((DateTime.Now - monitorData.StartTime).TotalSeconds > iniData.MaxTimeLimit)
                        {
                            StaticClass.Log(iniData.ProcessLogFile, String.Format("Job Timeout for job {0} at {1:HH:mm:ss.fff}", job, DateTime.Now));

                            // Handle job timeout
                            TimeoutHandler(job, iniData, logger);

                            // Create job Timeout status
                            StatusDataEntry(statusData, monitorData.Job, iniData, JobStatus.JOB_TIMEOUT, JobType.TIME_COMPLETE, iniData.StatusLogFile, logger);
                            jobComplete = true;
                        }

                        // Check if the shutdown flag is set, then exit method
                        if (StaticClass.ShutdownFlag == true)
                        {
                            logger.LogInformation("Shutdown Connect job {0}", monitorData.Job);
                            jobComplete = true;
                        }

                        Thread.Sleep(adjustableSleepTime);
                    }
                    else
                    {
                        return;
                    }
                }
                while (jobComplete == false);

                // Close everything
                stream.Close();
                client.Close();

                StaticClass.Log(iniData.ProcessLogFile,
                    String.Format("Completed TCP/IP Scan of Job {0} at {1:HH:mm:ss.fff}",
                        monitorData.Job, DateTime.Now));
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
