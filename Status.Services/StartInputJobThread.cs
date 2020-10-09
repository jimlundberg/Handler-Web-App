using Microsoft.Extensions.Logging;
using Status.Models;
using Status.Services;
using System;
using System.Threading;

namespace Handler.Services
{
    /// <summary>
    /// Start Input Job Thread
    /// </summary>
    public class StartInputJobThread
    {
        private static string DirectoryName;

        public StartInputJobThread(string directory)
        {
            DirectoryName = directory;
        }

        /// <summary>
        /// Thread procedure to run Input Job files watcher
        /// </summary>
        public void ThreadProc()
        {
            Thread inputFileWatcherThread = new Thread(() => StartInputJob(DirectoryName));
            if (inputFileWatcherThread == null)
            {
                StaticClass.Logger.LogError("InputFileWatcherThread inputFileWatcherThread thread failed to instantiate");
            }
            inputFileWatcherThread.Start();
        }

        /// <summary>
        /// Start Input Job
        /// </summary>
        /// <param name="directory"></param>
        void StartInputJob(string directory)
        {
            // Reset Input job file scan flag
            string job = directory.Replace(StaticClass.IniData.InputDir, "").Remove(0, 1);

            StaticClass.InputFileScanComplete[job] = false;

            // Get data found in Job xml file
            JobXmlData jobXmlData = StaticClass.GetJobXmlFileInfo(directory, DirectoryScanType.INPUT_BUFFER);
            if (jobXmlData == null)
            {
                StaticClass.Logger.LogError("InputJobsScanThread GetJobXmlData failed");
            }

            // Display job xml data found
            StaticClass.Log("Input Job                      : " + jobXmlData.Job);
            StaticClass.Log("Input Job Directory            : " + jobXmlData.JobDirectory);
            StaticClass.Log("Input Job Serial Number        : " + jobXmlData.JobSerialNumber);
            StaticClass.Log("Input Job Time Stamp           : " + jobXmlData.TimeStamp);
            StaticClass.Log("Input Job Xml File             : " + jobXmlData.XmlFileName);

            StaticClass.Log(string.Format("Started Input Job {0} executing Slot {1} at {2:HH:mm:ss.fff}",
                jobXmlData.Job, StaticClass.NumberOfJobsExecuting + 1, DateTime.Now));

            // Create a thread to run the job, and then start the thread
            JobRunThread jobRunThread = new JobRunThread(jobXmlData, DirectoryScanType.INPUT_BUFFER);
            if (jobRunThread == null)
            {
                StaticClass.Logger.LogError("InputJobsScanThread jobRunThread failed to instantiate");
            }
            jobRunThread.ThreadProc();
        }
    }
}
