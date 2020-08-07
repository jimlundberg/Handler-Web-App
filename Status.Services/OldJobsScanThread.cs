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
    public class OldJobsScanThread
    {
        // State information used in the task.
        private IniFileData IniData;
        private List<StatusWrapper.StatusData> StatusData;
        public volatile bool endProcess = false;
        private static Thread thread;
        public event EventHandler ProcessCompleted;

        // The constructor obtains the state information.
        /// <summary>
        /// Process Thread constructor receiving data buffers
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="statusData"></param>
        /// <param name="globalJobIndex"></param>
        /// <param name="numberOfJobsRunning"></param>
        public OldJobsScanThread(IniFileData iniData, List<StatusWrapper.StatusData> statusData)
        {
            IniData = iniData;
            StatusData = statusData;
        }

        protected virtual void OnProcessCompleted(EventArgs e)
        {
            ProcessCompleted?.Invoke(this, e);
        }


        /// <summary>
        /// A Thread procedure that scans for new jobs
        /// </summary>
        public void ThreadProc()
        {
            thread = new Thread(() => ScanForOldJobs(IniData, StatusData));
            thread.Start();
        }

        /// <summary>
        /// Method to scan for old jobs in the Processing Buffer
        /// </summary>
        public void ScanForOldJobs(IniFileData iniFileData, List<StatusWrapper.StatusData> statusData)
        {
            StatusModels.JobXmlData jobXmlData = new StatusModels.JobXmlData();
            List<string> oldDirectoryList = new List<string>();
            List<string> newDirectoryList = new List<string>();
            bool readInputDirectory = false;
            bool foundDirectories = false;

            do
            {
                // Check flag to start reading inputs after first reading is skipped to pick up current files
                if (readInputDirectory)
                {
                    // Get old directory list
                    var oldDirectoryInfo = new DirectoryInfo(iniFileData.ProcessingDir);
                    var oldDirectoryInfoList = oldDirectoryInfo.EnumerateDirectories().ToList();
                    foreach (var subdirectory in oldDirectoryInfoList)
                    {
                        oldDirectoryList.Add(subdirectory.ToString());
                    }
                    oldDirectoryList.Sort();
                }

                // Set flag to read directories from now on after seeing current ones as new
                readInputDirectory = true;

                // Get new directory list
                var newDirectoryInfo = new DirectoryInfo(iniFileData.ProcessingDir);
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
                    foundDirectories = true;
                    Console.WriteLine("\nFound unfinished job(s)...\n");

                    oldDirectoryList = newDirectoryList;
                    foreach (string dirName in directoryDifferenceQuery)
                    {
                        if (StaticData.NumberOfJobsExecuting < iniFileData.ExecutionLimit)
                        {
                            // Increment counts to track job execution and port id
                            StaticData.IncrementNumberOfJobsExecuting();

                            String job = dirName.Replace(iniFileData.ProcessingDir, "").Remove(0, 1);

                            // Delete the data.xml file if present
                            String dataXmlFile = iniFileData.ProcessingDir + @"\" + job + @"\" + "data.xml";
                            if (File.Exists(dataXmlFile))
                            {
                                File.Delete(dataXmlFile);
                            }

                            // Start scan for job files in the Output Buffer
                            ScanDirectory scanDir = new ScanDirectory();
                            jobXmlData = scanDir.GetJobXmlData(job, iniFileData.ProcessingDir + @"\" + job);

                            // Get data found in Xml file into Monitor Data
                            StatusModels.StatusMonitorData data = new StatusModels.StatusMonitorData();
                            data.Job = job;
                            data.JobDirectory = jobXmlData.JobDirectory;
                            data.JobSerialNumber = jobXmlData.JobSerialNumber;
                            data.TimeStamp = jobXmlData.TimeStamp;
                            data.XmlFileName = jobXmlData.XmlFileName;
                            data.JobIndex = StaticData.RunningJobsIndex++;

                            // Display Monitor Data found
                            Console.WriteLine("");
                            Console.WriteLine("Found unfinished Job  = " + data.Job);
                            Console.WriteLine("New Job Directory     = " + data.JobDirectory);
                            Console.WriteLine("New Serial Number     = " + data.JobSerialNumber);
                            Console.WriteLine("New Time Stamp        = " + data.TimeStamp);
                            Console.WriteLine("New Job Xml File      = " + data.XmlFileName);

                            Console.WriteLine("+++++Job {0} Executing slot {1}", data.Job, StaticData.NumberOfJobsExecuting);

                            // Create a thread to execute the task, and then start the thread.
                            Console.WriteLine("Starting Job " + data.Job);
                            JobRunThread jobThread = new JobRunThread(iniFileData.ProcessingDir, iniFileData, data, statusData);
                            jobThread.ThreadProc();

                            // If the shutdown flag is set, exit method
                            if (StaticData.ShutdownFlag == true)
                            {
                                Console.WriteLine("Shutdown ScanForUnfinishedJobs job {0} time {1:HH:mm:ss.fff}", data.Job, DateTime.Now);
                                return;
                            }

                            Thread.Sleep(iniFileData.ScanTime);
                        }
                        else
                        {
                            Thread.Sleep(iniFileData.ScanTime);
                        }
                    }
                }
                else
                {
                    if (foundDirectories)
                    {
                        Console.WriteLine("No more unfinished job(s) Found...");
                    }
                    else
                    {
                        Console.WriteLine("\nNo unfinished job(s) Found...");
                    }
                    StaticData.oldJobScanComplete = true;
                    return;
                }
            }
            while (true);
        }
    }
}

