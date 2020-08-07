using StatusModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Status.Services
{
    /// <summary>
    /// Class to run the whole monitoring process as a thread
    /// </summary>
    public class NewJobsScanThread
    {
        // State information used in the task.
        private static IniFileData IniData;
        private static List<StatusWrapper.StatusData> StatusData;
        public volatile bool endProcess = false;
        private static Thread thread;

        public static void oldJob_ProcessCompleted(object sender, EventArgs e)
        {
            // Set Flag for ending directory scan loop
            Console.WriteLine("Old Job Scan Completed!");
            StaticData.oldJobScanComplete = true;
            ScanForNewJobs(IniData, StatusData);
        }

        // The constructor obtains the state information.
        /// <summary>
        /// Process Thread constructor receiving data buffers
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="statusData"></param>
        /// <param name="globalJobIndex"></param>
        /// <param name="numberOfJobsRunning"></param>
        public NewJobsScanThread(IniFileData iniData, List<StatusWrapper.StatusData> statusData)
        {
            IniData = iniData;
            StatusData = statusData;

            // Register with the Old Jobs Event and start its thread
            OldJobsScanThread oldJobs = new OldJobsScanThread(iniData, statusData);
            oldJobs.ProcessCompleted += oldJob_ProcessCompleted;
            oldJobs.ScanForOldJobs(iniData, statusData);
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
        /// Method to scan for new jobs in the Input Buffer
        /// </summary>
        public static void ScanForNewJobs(IniFileData iniFileData, List<StatusWrapper.StatusData> statusData)
        {
            StatusModels.JobXmlData jobXmlData = new StatusModels.JobXmlData();
            List<string> oldDirectoryList = new List<string>();
            List<string> newDirectoryList = new List<string>();

            Console.WriteLine("\nScanning for new job(s)...");

            while (true)
            {
                // Get new directory list
                var newDirectoryInfo = new DirectoryInfo(iniFileData.InputDir);
                var newDirectoryInfoList = newDirectoryInfo.EnumerateDirectories().ToList();
                foreach (var subdirectory in newDirectoryInfoList)
                {
                    newDirectoryList.Add(subdirectory.ToString());
                }
                newDirectoryList.Sort();

                // Look for a difference between old and new directory lists
                IEnumerable<string> directoryDifferenceQuery = newDirectoryList.Except(oldDirectoryList);
                if (directoryDifferenceQuery.Any())
                {
                    Console.WriteLine("\nFound new job(s)...");

                    oldDirectoryList = newDirectoryList;
                    foreach (string dirName in directoryDifferenceQuery)
                    {
                        if (StaticData.NumberOfJobsExecuting < iniFileData.ExecutionLimit)
                        {
                            // Increment counters to track job execution
                            StaticData.IncrementNumberOfJobsExecuting();

                            String job = dirName.Replace(iniFileData.InputDir, "").Remove(0, 1);

                            // Start scan for new directory in the Input Buffer
                            ScanDirectory scanDir = new ScanDirectory();
                            jobXmlData = scanDir.GetJobXmlData(job, iniFileData.InputDir + @"\" + job);

                            // Get data found in Job xml file
                            StatusModels.StatusMonitorData data = new StatusModels.StatusMonitorData();
                            data.Job = job;
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
                                Console.WriteLine("Shutdown ScanForNewJobs job {0} time {1:HH:mm:ss.fff}", data.Job, DateTime.Now);
                                return;
                            }

                            // Supply the state information required by the task.
                            Console.WriteLine("Starting Job " + data.Job);
                            JobRunThread jobThread = new JobRunThread(iniFileData.InputDir, iniFileData, data, statusData);
                            jobThread.ThreadProc();
                            Thread.Sleep(1000);
                        }
                        else
                        {
                            Thread.Sleep(iniFileData.ScanTime);
                        }
                    }
                }
                Thread.Sleep(iniFileData.ScanTime);
            }
        }
    }
}
