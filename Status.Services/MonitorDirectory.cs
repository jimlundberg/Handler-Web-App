using StatusModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using static StatusModels.StatusWrapper;

namespace Status.Services
{
    /// <summary>
    /// Class to Monitor the number of files in a directory 
    /// </summary>
    public class MonitorDirectoryFiles
    {
        public static void TcpIp_ProcessCompleted(object sender, EventArgs e)
        {
            // Set Flag for ending directory scan loop
            Console.WriteLine("Monitor directory received Tcp/Ip Scan Completed!");
            StaticData.tcpIpScanComplete = true;
        }

        /// <summary>
        /// Monitor the Directory for a selected number of files with a timeout
        /// </summary>
        /// <param name="monitoredDir"></param>
        /// <param name="numberOfFilesNeeded"></param>
        /// <param name="timeout"></param>
        /// <param name="scanTime"></param>
        /// <returns></returns>
        public static bool MonitorDirectory(StatusModels.DirectoryScanType scanType, IniFileData iniData, StatusMonitorData monitorData,
             List<StatusWrapper.StatusData> statusData, String monitoredDir, int numberOfFilesNeeded, int timeout, int scanTime)
        {
            bool filesFound = false;
            int numberOfSeconds = 0;

            if (scanType == StatusModels.DirectoryScanType.PROCESSING_BUFFER)
            {
                // Register with the Tcp/Ip Event and start it's thread
                JobTcpIpThread tcpIp = new JobTcpIpThread(iniData, monitorData, statusData);
                tcpIp.ProcessCompleted += TcpIp_ProcessCompleted;
                tcpIp.StartTcpIpScanProcess(iniData, monitorData, statusData);
            }

            do
            {
                if (StaticData.tcpIpScanComplete == true)
                {
                    int numberOfFilesFound = Directory.GetFiles(monitoredDir, "*", SearchOption.TopDirectoryOnly).Length;
                    if (numberOfFilesFound >= numberOfFilesNeeded)
                    {
                        Console.WriteLine("Recieved all {0} files in {1}", numberOfFilesFound, monitoredDir);

                        Thread.Sleep(10000);
                        return true;
                    }

                    // If the shutdown flag is set, exit method
                    if (StaticData.ShutdownFlag == true)
                    {
                        Console.WriteLine("Shutdown ScanForNewJobs directory {0} time {1:HH:mm:ss.fff}", monitoredDir, DateTime.Now);
                        return false;
                    }

                    Thread.Sleep(scanTime);
                    numberOfSeconds++;
                }
            }
            while ((filesFound == false) && (numberOfSeconds < timeout));

            return false;
        }
    }
}
