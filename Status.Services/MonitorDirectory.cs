using System;
using System.IO;
using System.Threading;

namespace Status.Services
{
    /// <summary>
    /// Class to Monitor the number of files in a directory 
    /// </summary>
    public class MonitorDirectoryFiles
    {
        /// <summary>
        /// Monitor the Directory for a selected number of files with a timeout
        /// </summary>
        /// <param name="monitoredDir"></param>
        /// <param name="numberOfFilesNeeded"></param>
        /// <param name="timeout"></param>
        /// <param name="scanTime"></param>
        /// <returns></returns>
        public static bool MonitorDirectory(String monitoredDir, int numberOfFilesNeeded, int timeout, int scanTime)
        {
            bool filesFound = false;
            int numberOfSeconds = 0;

            do
            {
                int numberOfFilesFound = Directory.GetFiles(monitoredDir, "*", SearchOption.TopDirectoryOnly).Length;
                if (numberOfFilesFound >= numberOfFilesNeeded)
                {
                    Console.WriteLine("Recieved all {0} files in {1}", numberOfFilesFound, monitoredDir);
                    return true;
                }

                Thread.Sleep(scanTime);
                numberOfSeconds++;
            }
            while ((filesFound == false) && (numberOfSeconds < timeout));

            return false;
        }
    }
}
