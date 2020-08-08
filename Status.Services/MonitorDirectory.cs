using StatusModels;
using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Status.Services
{
    /// <summary>
    /// Class to Monitor the number of files in a directory 
    /// </summary>
    public class MonitorDirectoryFiles
    {
        public static ILogger<StatusRepository> Logger;

        public static void TcpIp_ProcessCompleted(object sender, EventArgs e)
        {
            // Set Flag for ending directory scan loop
            Logger.LogInformation("Monitor directory received Tcp/Ip Scan Completed!");
            StaticData.tcpIpScanComplete = true;
        }

        /// <summary>
        /// Monitor the Directory for a selected number of files with a timeout
        /// </summary>
        /// <param name="monitoredDir"></param>
        /// <param name="numberOfFilesNeeded"></param>
        /// <param name="timeout"></param>
        /// <param name="scanTime"></param>
        /// <param name="logger"></param>
        /// <returns>Pass/Fail</returns>
        public static bool MonitorDirectory(StatusModels.DirectoryScanType scanType, IniFileData iniData, StatusMonitorData monitorData,
             List<StatusData> statusData, String monitoredDir, int numberOfFilesNeeded, ILogger<StatusRepository> logger)
        {
            bool filesFound = false;

            Logger = logger;

            if (scanType == StatusModels.DirectoryScanType.PROCESSING_BUFFER)
            {
                // Register with the Tcp/Ip Event and start it's thread
                JobTcpIpThread tcpIp = new JobTcpIpThread(iniData, monitorData, statusData, logger);
                if (tcpIp == null)
                {
                    logger.LogError("MonitorDirectory tcpIp thread failed to instantiate");
                }
                tcpIp.ProcessCompleted += TcpIp_ProcessCompleted;
                tcpIp.StartTcpIpScanProcess(iniData, monitorData, statusData, logger);
            }

            // Scan directory until files found or timeout
            do
            {
                if (StaticData.tcpIpScanComplete == true)
                {
                    int numberOfFilesFound = Directory.GetFiles(monitoredDir, "*", SearchOption.TopDirectoryOnly).Length;
                    if (numberOfFilesFound >= numberOfFilesNeeded)
                    {
                        logger.LogInformation("Recieved {0} of {1} files in {2}",
                            numberOfFilesFound, numberOfFilesNeeded, monitoredDir);
                        return true;
                    }

                    // If the shutdown flag is set, exit method
                    if (StaticData.ShutdownFlag == true)
                    {
                        logger.LogInformation("Shutdown MonitorDirectory {0}", monitoredDir);
                        return false;
                    }

                    Thread.Sleep(iniData.ScanTime);
                }
            }
            while ((filesFound == false) && ((DateTime.Now - monitorData.StartTime).TotalSeconds < iniData.MaxTimeLimit));

            return false;
        }
    }
}
