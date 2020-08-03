using StatusModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Status.Services
{
    /// <summary>
    /// Class to run the whole monitoring process as a thread
    /// </summary>
    public class JobScanThread
    {
        // State information used in the task.
        private IniFileData IniData;
        private List<StatusWrapper.StatusData> StatusData;
        public volatile bool endProcess = false;
        private static Thread thread;

        // The constructor obtains the state information.
        /// <summary>
        /// Process Thread constructor receiving data buffers
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="statusData"></param>
        /// <param name="globalJobIndex"></param>
        /// <param name="numberOfJobsRunning"></param>
        public JobScanThread(IniFileData iniData, List<StatusWrapper.StatusData> statusData)
        {
            IniData = iniData;
            StatusData = statusData;
        }

        /// <summary>
        /// A Thread procedure that scans for new jobs
        /// </summary>
        public void ThreadProc()
        {
            thread = new Thread(() => ScanForNewJobs(IniData, StatusData));
            thread.Start();
        }

        /// <summary>
        /// Method to set flag to stop the monitoring process
        /// </summary>
        public void StopProcess()
        {
            thread.Abort();
        }

        /// <summary>
        /// Method to scan for new jobs in the Input Buffer
        /// </summary>
        public static void ScanForNewJobs(IniFileData iniFileData, List<StatusWrapper.StatusData> statusData)
        {
            StatusModels.JobXmlData jobXmlData = new StatusModels.JobXmlData();
            DirectoryInfo directory = new DirectoryInfo(iniFileData.InputDir);
            List<String> directoryList = new List<String>();

            Console.WriteLine("\nWaiting for new job(s)...\n");

            while (true)
            {
                // Check if there are any directories
                DirectoryInfo[] subdirs = directory.GetDirectories();
                if (subdirs.Length != 0)
                {
                    for (int i = 0; i < subdirs.Length; i++)
                    {
                        if (StaticData.NumberOfJobsExecuting < iniFileData.ExecutionLimit)
                        {
                            // Increment counters to track job execution and port id
                            StaticData.IncrementNumberOfJobsExecuting();

                            String job = subdirs[i].Name;

                            // Start scan for new directory in the Input Buffer
                            ScanDirectory scanDir = new ScanDirectory(iniFileData.InputDir);
                            jobXmlData = scanDir.GetJobXmlData(iniFileData.InputDir + @"\" + job);

                            // Set data found
                            StatusModels.StatusMonitorData data = new StatusModels.StatusMonitorData();
                            data.Job = jobXmlData.Job;
                            data.JobDirectory = jobXmlData.JobDirectory;
                            data.JobSerialNumber = jobXmlData.JobSerialNumber;
                            data.TimeStamp = jobXmlData.TimeStamp;
                            data.XmlFileName = jobXmlData.XmlFileName;
                            data.JobIndex = StaticData.RunningJobsIndex++;

                            // Display data found
                            Console.WriteLine("");
                            Console.WriteLine("Found new Job         = " + data.Job);
                            Console.WriteLine("New Job Directory     = " + data.JobDirectory);
                            Console.WriteLine("New Serial Number     = " + data.JobSerialNumber);
                            Console.WriteLine("New Time Stamp        = " + data.TimeStamp);
                            Console.WriteLine("New Job Xml File      = " + data.XmlFileName);

                            Console.WriteLine("+++++Job {0} Executing slot {1}", data.Job, StaticData.NumberOfJobsExecuting);

                            // If the shutdown flag is set, exit method
                            if (StaticData.ShutdownFlag == true)
                            {
                                return;
                            }

                            // Supply the state information required by the task.
                            JobRunThread jobThread = new JobRunThread(iniFileData.InputDir, iniFileData, data, statusData);
                            Console.WriteLine("Starting Job " + data.Job);
                            jobThread.ThreadProc();

                            // Delay to let Modeler startup
                            Thread.Sleep(15000);
                        }
                        else
                        {
                            Thread.Sleep(iniFileData.ScanTime);
                        }
                    }
                }

                // Sleep to allow job to finish before checking for more
                Thread.Sleep(iniFileData.ScanTime);
            }
        }
    }
}
