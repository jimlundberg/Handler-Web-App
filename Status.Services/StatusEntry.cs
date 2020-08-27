using Microsoft.Extensions.Logging;
using ReadWriteCsvFile;
using StatusModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Status.Services
{
    /// <summary>
    /// Status Entry class
    /// </summary>
    public class StatusEntry
    {
        List<StatusData> StatusList;
        readonly string Job;
        readonly JobStatus Status;
        readonly JobType TimeSlot;
        readonly string LogFileName;
        private static readonly Object csvLock = new Object();
        ILogger<StatusRepository> Logger;

        public StatusEntry() { }

        /// <summary>
        /// Status Entry logging method
        /// </summary>
        /// <param name="statusList"></param>
        /// <param name="job"></param>
        /// <param name="status"></param>
        /// <param name="timeSlot"></param>
        /// <param name="logFileName"></param>
        /// <param name="logger"></param>
        public StatusEntry(List<StatusData> statusList, string job, JobStatus status,
            JobType timeSlot, string logFileName, ILogger<StatusRepository> logger)
        {
            StatusList = statusList;
            Job = job;
            Status = status;
            TimeSlot = timeSlot;
            LogFileName = logFileName;
            Logger = logger;
        }

        /// <summary>
        /// Log a Status and write to csv file
        /// </summary>
        /// <param name="iniData"></param>
        /// <param name="statusList"></param>
        /// <param name="job"></param>
        /// <param name="status"></param>
        /// <param name="timeSlot"></param>
        public void ListStatus(IniFileData iniData, List<StatusData> statusList,
            string job, JobStatus status, JobType timeSlot)
        {
            StatusData entry = new StatusData();
            if (entry == null)
            {
                Logger.LogError("StatusEntry entry failed to instantiate");
            }

            entry.Job = job;
            entry.JobStatus = status;
            switch (timeSlot)
            {
                case JobType.TIME_RECEIVED:
                    entry.TimeReceived = DateTime.Now;
                    break;

                case JobType.TIME_START:
                    entry.TimeStarted = DateTime.Now;
                    break;

                case JobType.TIME_COMPLETE:
                    entry.TimeCompleted = DateTime.Now;
                    break;
            }

            // Add entry to status data list
            statusList.Add(entry);

            StaticClass.Log(iniData.ProcessLogFile,
                String.Format("Status: Job:{0} Job Status:{1} Time Type:{2}",
                job, status, timeSlot.ToString()));
        }

        /// <summary>
        /// Write Status data to the designated log file
        /// </summary>
        /// <param name="job"></param>
        /// <param name="iniData"></param>
        /// <param name="status"></param>
        /// <param name="timeSlot"></param>
        /// <param name="logFileName"></param>
        public void WriteToCsvFile(string job, JobStatus status, JobType timeSlot, string logFileName)
        {
            lock (csvLock)
            {
                using (StreamWriter writer = File.AppendText(logFileName))
                {
                    DateTime timeReceived = new DateTime();
                    if (timeReceived == null)
                    {
                        Logger.LogError("StatusEntry timeReceived failed to instantiate");
                    }

                    DateTime timeStarted = new DateTime();
                    if (timeStarted == null)
                    {
                        Logger.LogError("StatusEntry timeStarted failed to instantiate");
                    }

                    DateTime timeCompleted = new DateTime();
                    if (timeCompleted == null)
                    {
                        Logger.LogError("StatusEntry timeCompleted failed to instantiate");
                    }

                    switch (timeSlot)
                    {
                        case JobType.TIME_RECEIVED:
                            timeReceived = DateTime.Now;
                            break;

                        case JobType.TIME_START:
                            timeStarted = DateTime.Now;
                            break;

                        case JobType.TIME_COMPLETE:
                            timeCompleted = DateTime.Now;
                            break;
                    }

                    string line = String.Format("{0},{1},{2},{3},{4}",
                        job, status.ToString(), timeReceived, timeStarted, timeCompleted);
                    writer.WriteLineAsync(line);
                }
            }
        }

        /// <summary>
        /// Read Status Data from CSV File 
        /// </summary>
        /// <param name="logFileName"></param>
        /// <param name="iniData"></param>
        /// <returns></returns>
        public List<StatusData> ReadFromCsvFile(IniFileData iniData)
        {
            List<StatusData> statusDataTable = new List<StatusData>();
            DateTime timeReceived = DateTime.MinValue;
            DateTime timeStarted = DateTime.MinValue;
            DateTime timeCompleted = DateTime.MinValue;
            string logFileName = iniData.StatusLogFile;

            if (File.Exists(logFileName) == true)
            {
                lock (csvLock)
                {
                    using (CsvFileReader reader = new CsvFileReader(logFileName))
                    {
                        if (reader == null)
                        {
                            Logger.LogError("ReadFromCsvFile reader failed to instantiate");
                            return null;
                        }

                        CsvRow rowData = new CsvRow();
                        while (reader.ReadRow(rowData))
                        {
                            StatusData rowStatusData = new StatusData();
                            if (rowStatusData == null)
                            {
                                Logger.LogError("ReadFromCsvFile rowStatusData failed to instantiate");
                                return null;
                            }

                            rowStatusData.Job = rowData[0];

                            string jobType = rowData[1];
                            switch (jobType)
                            {
                                case "JOB_STARTED":
                                    rowStatusData.JobStatus = JobStatus.JOB_STARTED;
                                    break;

                                case "EXECUTING":
                                    rowStatusData.JobStatus = JobStatus.EXECUTING;
                                    break;

                                case "MONITORING_INPUT":
                                    rowStatusData.JobStatus = JobStatus.MONITORING_INPUT;
                                    break;

                                case "COPYING_TO_PROCESSING":
                                    rowStatusData.JobStatus = JobStatus.COPYING_TO_PROCESSING;
                                    break;

                                case "MONITORING_PROCESSING":
                                    rowStatusData.JobStatus = JobStatus.MONITORING_PROCESSING;
                                    break;

                                case "MONITORING_TCPIP":
                                    rowStatusData.JobStatus = JobStatus.MONITORING_TCPIP;
                                    break;

                                case "COPYING_TO_ARCHIVE":
                                    rowStatusData.JobStatus = JobStatus.COPYING_TO_ARCHIVE;
                                    break;

                                case "JOB_TIMEOUT":
                                    rowStatusData.JobStatus = JobStatus.JOB_TIMEOUT;
                                    break;

                                case "COMPLETE":
                                    rowStatusData.JobStatus = JobStatus.COMPLETE;
                                    break;
                            }

                            // Get Time Recieved
                            if (rowData[2] == "1/1/0001 12:00:00 AM")
                            {
                                rowStatusData.TimeReceived = DateTime.MinValue;
                            }
                            else
                            {
                                rowStatusData.TimeReceived = Convert.ToDateTime(rowData[2]);
                            }

                            // Get Time Started
                            if (rowData[3] == "1/1/0001 12:00:00 AM")
                            {
                                rowStatusData.TimeStarted = DateTime.MinValue;
                            }
                            else
                            {
                                rowStatusData.TimeStarted = Convert.ToDateTime(rowData[3]);
                            }

                            // Get Time Complete
                            if (rowData[4] == "1/1/0001 12:00:00 AM")
                            {
                                rowStatusData.TimeCompleted = DateTime.MinValue;
                            }
                            else
                            {
                                rowStatusData.TimeCompleted = Convert.ToDateTime(rowData[4]);
                            }

                            // Add data to status table
                            statusDataTable.Add(rowStatusData);

                            // If the shutdown flag is set, exit method
                            if (StaticClass.ShutdownFlag == true)
                            {
                                StaticClass.Log(iniData.ProcessLogFile,
                                    String.Format("\nShutdown ReadFromCsvFile job {0} row {1}", rowStatusData.Job, rowStatusData));
                                return null;
                            }

                            // Check if the pause flag is set, then wait for reset
                            if (StaticClass.PauseFlag == true)
                            {
                                do
                                {
                                    Thread.Yield();
                                }
                                while (StaticClass.PauseFlag == true);
                            }
                        }
                    }
                }

                // Return status table list
                return statusDataTable;
            }

            return null;
        }

        /// <summary>
        /// Read, reject old data and rewrite Log File
        /// </summary>
        /// <param name="iniData"></param>
        public void CheckLogFileHistory(IniFileData iniData)
        {
            List<StatusData> statusDataTable = new List<StatusData>();
            string logFileName = iniData.StatusLogFile;
            int logFileHistoryLimit = iniData.LogFileHistoryLimit;

            if (File.Exists(logFileName) == true)
            {
                lock (csvLock)
                {
                    using (CsvFileReader reader = new CsvFileReader(logFileName))
                    {
                        if (reader == null)
                        {
                            Logger.LogError("CheckLogFileHistory reader failed to instantiate");
                            return;
                        }

                        CsvRow rowData = new CsvRow();
                        if (rowData == null)
                        {
                            Logger.LogError("CheckLogFileHistory rowData failed to instantiate");
                            return;
                        }

                        while (reader.ReadRow(rowData))
                        {
                            StatusData rowStatusData = new StatusData();
                            if (rowStatusData == null)
                            {
                                Logger.LogError("CheckLogFileHistory rowStatusData failed to instantiate");
                                return;
                            }

                            bool oldRecord = false;
                            string job = rowStatusData.Job = rowData[0];

                            string jobType = rowData[1];
                            switch (jobType)
                            {
                                case "JOB_STARTED":
                                    rowStatusData.JobStatus = JobStatus.JOB_STARTED;
                                    break;

                                case "EXECUTING":
                                    rowStatusData.JobStatus = JobStatus.EXECUTING;
                                    break;

                                case "MONITORING_INPUT":
                                    rowStatusData.JobStatus = JobStatus.MONITORING_INPUT;
                                    break;

                                case "COPYING_TO_PROCESSING":
                                    rowStatusData.JobStatus = JobStatus.COPYING_TO_PROCESSING;
                                    break;

                                case "MONITORING_PROCESSING":
                                    rowStatusData.JobStatus = JobStatus.MONITORING_PROCESSING;
                                    break;

                                case "MONITORING_TCPIP":
                                    rowStatusData.JobStatus = JobStatus.MONITORING_TCPIP;
                                    break;

                                case "COPYING_TO_ARCHIVE":
                                    rowStatusData.JobStatus = JobStatus.COPYING_TO_ARCHIVE;
                                    break;

                                case "JOB_TIMEOUT":
                                    rowStatusData.JobStatus = JobStatus.JOB_TIMEOUT;
                                    break;

                                case "COMPLETE":
                                    rowStatusData.JobStatus = JobStatus.COMPLETE;
                                    break;
                            }

                            // Check Time Received if older than history limit
                            DateTime timeReceived = Convert.ToDateTime(rowData[2]);
                            double timeReceivedExperationTime = (DateTime.Now - timeReceived).TotalSeconds;
                            if ((timeReceivedExperationTime > logFileHistoryLimit) && (timeReceived != DateTime.MinValue))
                            {
                                oldRecord = true;
                            }
                            else
                            {
                                rowStatusData.TimeReceived = Convert.ToDateTime(rowData[2]);
                            }

                            // Check Time Started if older than history limit
                            DateTime timeStarted = Convert.ToDateTime(rowData[3]);
                            double timeStartedExperationTime = (DateTime.Now - timeStarted).TotalSeconds;
                            if ((timeStartedExperationTime > logFileHistoryLimit) && (timeStarted != DateTime.MinValue))
                            {
                                oldRecord = true;
                            }
                            else
                            {
                                rowStatusData.TimeStarted = Convert.ToDateTime(rowData[3]);
                            }

                            // Check Time Complete if older than history limit
                            DateTime timeCompleted = Convert.ToDateTime(rowData[4]);
                            double timeCompleteExperationTime = (DateTime.Now - timeCompleted).TotalSeconds;
                            if ((timeCompleteExperationTime > logFileHistoryLimit) && (timeCompleted != DateTime.MinValue))
                            {
                                oldRecord = true;
                            }
                            else
                            {
                                rowStatusData.TimeCompleted = Convert.ToDateTime(rowData[4]);
                            }

                            // Add data to status table if not rejected as old
                            if (oldRecord == false)
                            {
                                statusDataTable.Add(rowStatusData);
                            }

                            // If the shutdown flag is set, exit method
                            if (StaticClass.ShutdownFlag == true)
                            {
                                StaticClass.Log(iniData.ProcessLogFile,
                                    String.Format("\nShutdown CheckLogFileHistory job {0} at {1:HH:mm:ss.fff}",
                                    job, DateTime.Now));
                                return;
                            }

                            // Check if the pause flag is set, then wait for reset
                            if (StaticClass.PauseFlag == true)
                            {
                                do
                                {
                                    Thread.Yield();
                                }
                                while (StaticClass.PauseFlag == true);
                            }
                        }
                    }

                    // Create new csv file with new data
                    using (TextWriter writer = new StreamWriter(logFileName))
                    {
                        for (int i = 0; i < statusDataTable.Count; i++)
                        {
                            lock (csvLock)
                            {
                                writer.WriteLine("{0},{1},{2},{3},{4}",
                                    statusDataTable[i].Job, statusDataTable[i].JobStatus.ToString(),
                                    statusDataTable[i].TimeReceived, statusDataTable[i].TimeStarted, statusDataTable[i].TimeCompleted);
                            }
                        }

                        writer.Close();
                    }
                }
            }
        }
    }
}
