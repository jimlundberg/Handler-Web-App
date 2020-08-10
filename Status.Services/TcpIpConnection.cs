using Microsoft.Extensions.Logging;
using StatusModels;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace Status.Services
{
    /// <summary>
    /// Class to monitor and report status the TCP/IP connection to the Monitor application that is executing 
    /// </summary>
    public class TcpIpConnection
    {
        /// <summary>
        /// Status Data Entry
        /// </summary>
        /// <param name="statusList"></param>
        /// <param name="job"></param>
        /// <param name="status"></param>
        /// <param name="timeSlot"></param>
        /// <param name="logFileName"></param>
        /// <param name="logger"></param>
        public static void StatusDataEntry(List<StatusWrapper.StatusData> statusList, string job, JobStatus status, 
            JobType timeSlot, string logFileName, ILogger<StatusRepository> logger)
        {
            StatusEntry statusData = new StatusEntry(statusList, job, status, timeSlot, logFileName, logger);
            statusData.ListStatus(statusList, job, status, timeSlot);
            statusData.WriteToCsvFile(job, status, timeSlot, logFileName, logger);
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
        public void Connect(string server, IniFileData iniData, StatusMonitorData monitorData,
            List<StatusWrapper.StatusData> statusData, string message, ILogger<StatusRepository> logger)
        {
            // Wait a minute for Modeler to open
            Thread.Sleep(60000);
            Console.WriteLine("\nStarting Tcp/Ip Scan for job {0} on port {1} at {2:HH:mm:ss.fff}",
                monitorData.Job, monitorData.JobPortNumber, DateTime.Now);

            try
            {
                // Log Tcp/Ip monitoring entry
                StatusDataEntry(statusData, monitorData.Job, JobStatus.MONITORING_TCPIP, JobType.TIME_START, iniData.StatusLogFile, logger);

                // Create a TcpClient.
                // Note, for this client to work you need to have a TcpServer
                // connected to the same address as specified by the server, port combination.
                TcpClient client = new TcpClient(server, monitorData.JobPortNumber);
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
                int sleepTime = 15000;
                do
                {
                    // Send the message to the connected TcpServer.
                    stream.Write(data, 0, data.Length);

                    // Receive the TcpServer.response.
                    Console.WriteLine("Sending {0} msg to Modeler for Job {1} on port {2} at {3:HH:mm:ss.fff}",
                        message, monitorData.Job, monitorData.JobPortNumber, DateTime.Now);

                    // Buffer to store the response bytes.
                    data = new Byte[256];

                    // String to store the response ASCII representation.
                    string responseData = String.Empty;

                    // Read the first batch of the TcpServer response bytes.
                    if (stream.CanRead)
                    {
                        Int32 bytes = stream.Read(data, 0, data.Length);
                        responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);

                        // Send status for response received
                        switch (responseData)
                        {
                            case "Step 1 in process.":
                            case "Step 2 in process.":
                            case "Step 3 in process.":
                            case "Step 4 in process.":
                               Console.WriteLine("Received: {0} from Job {1} on port {2} at {3:HH:mm:ss.fff}",
                                    responseData, monitorData.Job, monitorData.JobPortNumber, DateTime.Now);
                                break;

                            case "Step 5 in process.":
                               Console.WriteLine("Received: {0} from Job {1} on port {2} at {3:HH:mm:ss.fff}",
                                    responseData, monitorData.Job, monitorData.JobPortNumber, DateTime.Now);
                                sleepTime = 1000;
                                break;

                            case "Step 6 in process.":
                               Console.WriteLine("Received: {0} from Job {1} on port {2} at {3:HH:mm:ss.fff}",
                                    responseData, monitorData.Job, monitorData.JobPortNumber, DateTime.Now);
                                sleepTime = 100;
                                break;

                            case "Whole process done, socket closed.":
                               Console.WriteLine("Received: {0} from Job {1} on port {2} at {3:HH:mm:ss.fff}",
                                    responseData, monitorData.Job, monitorData.JobPortNumber, DateTime.Now);
                                StaticData.TcpIpScanComplete = true;
                                jobComplete = true;
                                break;

                            default:
                                logger.LogWarning("Received Weird Response: {0} from Job {1} on port {2} at {3:HH:mm:ss.fff}",
                                    responseData, monitorData.Job, monitorData.JobPortNumber, DateTime.Now);
                                break;
                        }

                        // Check for job timeout
                        if ((DateTime.Now - monitorData.StartTime).TotalSeconds > iniData.MaxTimeLimit)
                        {
                            Console.WriteLine("Job Timeout for job {0} at {1:HH:mm:ss.fff}", monitorData.Job, DateTime.Now);

                            StatusDataEntry(statusData, monitorData.Job, JobStatus.JOB_TIMEOUT, JobType.TIME_COMPLETE, iniData.StatusLogFile, logger);
                            StaticData.TcpIpScanComplete = true;
                            jobComplete = true;
                        }

                        // Check if the shutdown flag is set, then exit method
                        if (StaticData.ShutdownFlag == true)
                        {
                            logger.LogInformation("Shutdown Connect job {0}", monitorData.Job);

                            jobComplete = true;
                        }

                        Thread.Sleep(sleepTime);
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

                Console.WriteLine("Completed TCP/IP Scan of Job {0} at {1:HH:mm:ss.fff}", monitorData.Job, DateTime.Now);
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
