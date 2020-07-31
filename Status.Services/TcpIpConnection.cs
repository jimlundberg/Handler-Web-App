﻿using StatusModels;
using System;
using System.Net.Sockets;
using System.Threading;

namespace Status.Services
{
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
        public void Connect(String server, StatusMonitorData monitorData, String message)
        {
            try
            {
                // Set current port number
                PortNumber = monitorData.JobPortNumber;

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
                    Console.WriteLine("Querying Modeler for Job {0} on port {1} at {2:HH:mm:ss.fff}", monitorData.Job, PortNumber, DateTime.Now);
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
                            Console.WriteLine("Received: {0} from Job {1}", responseData, monitorData.Job);
                            break;

                        case "Step 5 in process.":
                        case "Step 6 in process.":
                            Console.WriteLine("Received: {0} from Job {1}", responseData, monitorData.Job);
                            sleepTime = 1000;
                            break;

                        case "Whole process done, socket closed.":
                            Console.WriteLine("Received: {0} from Job {1}", responseData, monitorData.Job);
                            jobComplete = true;
                            break;

                        default:
                            Console.WriteLine("$$$$$Received Weird Response: {0} from Job {1}", responseData, monitorData.Job);
                            break;
                    }

                    Thread.Sleep(sleepTime);
                }
                while (jobComplete == false);

                Console.WriteLine("Exiting TCP/IP Scan of Job {0}", monitorData.Job);

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
    }
}