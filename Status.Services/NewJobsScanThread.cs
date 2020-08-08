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
        // State information used in the scanning task.
        private static IniFileData IniData;
        private static List<StatusWrapper.StatusData> StatusData;
        public volatile bool endProcess = false;
        private static Thread thread;

        /// <summary>
        /// Processing complete callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void oldJob_ProcessCompleted(object sender, EventArgs e)
        {
            // Set Flag for ending directory scan loop
            Console.WriteLine("Old Job Scan Completed!");
            StaticData.oldJobScanComplete = true;
            ScanForNewJobs(IniData, StatusData);
        }

        /// <summary>
        /// Process Thread constructor receiving data buffers
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="statusData"></param>
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
        /// <param name="iniFileData"></param>
        /// <param name="statusData"></param>
        public static void ScanForNewJobs(IniFileData iniFileData, List<StatusWrapper.StatusData> statusData)
        {
            StatusModels.JobXmlData jobXmlData = new StatusModels.JobXmlData();
            List<string> newDirectoryList = new List<string>();
            List<string> runDirectoryList = new List<string>();

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

                // Look for a difference between new and run directory lists
                if (newDirectoryList != runDirectoryList)
                {
                    runDirectoryList = newDirectoryList;
                }

                for (int i = 0; i < runDirectoryList.Count(); i++)
                {
                    if (StaticData.NumberOfJobsExecuting < iniFileData.ExecutionLimit)
                    {
                        // Increment counters to track job execution
                        StaticData.IncrementNumberOfJobsExecuting();

                        // Get job name from directory name
                        String job = runDirectoryList[i].Replace(iniFileData.InputDir, "").Remove(0, 1);

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

                        // Remove job from the run list when run
                        runDirectoryList.Remove(runDirectoryList[i]);

                        Thread.Sleep(1000);
                    }
                    else
                    {
                        Thread.Sleep(iniFileData.ScanTime);
                    }
                }

                // Time between scans
                Thread.Sleep(iniFileData.ScanTime);
            }
        }
    }
}
