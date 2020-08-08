using Microsoft.Extensions.Logging;
using ReadWriteCsvFile;
using StatusModels;
using System;
using System.Collections.Generic;
using System.IO;

namespace Status.Services
{
    /// <summary>
    /// Status Entry class
    /// </summary>
    public class StatusEntry
    {
        List<StatusData> StatusList;
        readonly String Job;
        readonly JobStatus Status;
        readonly JobType TimeSlot;
        readonly String LogFileName;
        private static Object csvLock = new Object();
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
        public StatusEntry(List<StatusData> statusList, String job, JobStatus status, JobType timeSlot, String logFileName, ILogger<StatusRepository> logger)
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
        /// <param name="statusList"></param>
        /// <param name="job"></param>
        /// <param name="status"></param>
        /// <param name="timeSlot"></param>
        public void ListStatus(List<StatusData> statusList, String job, JobStatus status, JobType timeSlot)
        {
            StatusData entry = new StatusData();
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

            Console.WriteLine("Status: Job:{0} Job Status:{1} Time Type:{2}", job, status, timeSlot.ToString());
        }

        /// <summary>
        /// Write Status data to the designated log file
        /// </summary>
        /// <param name="job"></param>
        /// <param name="status"></param>
        /// <param name="timeSlot"></param>
        /// <param name="logFileName"></param>
        /// <param name="logger"></param>
        public void WriteToCsvFile(String job, JobStatus status, JobType timeSlot, String logFileName, ILogger<StatusRepository> logger)
        {
            lock (csvLock)
            {
                using (StreamWriter writer = File.AppendText(logFileName))
                {
                    DateTime timeReceived = new DateTime();
                    DateTime timeStarted = new DateTime();
                    DateTime timeCompleted = new DateTime();
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

                    String line = string.Format("{0},{1},{2},{3},{4}", job, status.ToString(), timeReceived, timeStarted, timeCompleted);
                    writer.WriteLineAsync(line);
                }
            }
        }

        /// <summary>
        /// Read Status Data from CSV File
        /// </summary>
        /// <param name="logFileName"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public List<StatusData> ReadFromCsvFile(String logFileName, ILogger<StatusRepository> logger)
        {
            lock (csvLock)
            {
                List<StatusData> statusDataTable = new List<StatusData>();
                DateTime timeReceived = DateTime.MinValue;
                DateTime timeStarted = DateTime.MinValue;
                DateTime timeCompleted = DateTime.MinValue;

                if (File.Exists(logFileName) == true)
                {
                    using (CsvFileReader reader = new CsvFileReader(logFileName))
                    {
                        CsvRow rowData = new CsvRow();
                        while (reader.ReadRow(rowData))
                        {
                            StatusData rowStatusData = new StatusData();
                            rowStatusData.Job = rowData[0];

                            String jobType = rowData[1];
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
                            if (StaticData.ShutdownFlag == true)
                            {
                                Console.WriteLine("Shutdown ReadFromCsvFile job {0} row {1} time {2:HH:mm:ss.fff}", rowStatusData.Job, rowStatusData, DateTime.Now);
                                return statusDataTable;
                            }
                        }
                    }
                }

                // Return status table list
                return statusDataTable;
            }
        }

        /// <summary>
        /// Read, reject old data and rewrite Log File
        /// </summary>
        /// <param name="logFileName"></param>
        /// <param name="logFileHistory"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public void CheckLogFileHistory(String logFileName, int logFileHistory, ILogger<StatusRepository> logger)
        {
            List<StatusData> statusDataTable = new List<StatusData>();

            if (File.Exists(logFileName) == true)
            {
                using (CsvFileReader reader = new CsvFileReader(logFileName))
                {
                    CsvRow rowData = new CsvRow();
                    while (reader.ReadRow(rowData))
                    {
                        StatusData rowStatusData = new StatusData();
                        bool oldRecord = false;
                        rowStatusData.Job = rowData[0];

                        String jobType = rowData[1];
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

                            case "COMPLETE":
                                rowStatusData.JobStatus = JobStatus.COMPLETE;
                                break;
                        }

                        // Check Time Received if older than history limit
                        DateTime timeReceived = Convert.ToDateTime(rowData[2]);
                        if (((DateTime.Now - timeReceived).TotalDays > logFileHistory) && (timeReceived != DateTime.MinValue))
                        {
                            oldRecord = true;
                        }
                        else
                        {
                            rowStatusData.TimeReceived = Convert.ToDateTime(rowData[2]);
                        }

                        // Check Time Started if older than history limit
                        DateTime timeStarted = Convert.ToDateTime(rowData[3]);
                        if (((DateTime.Now - timeStarted).TotalDays > logFileHistory) && (timeStarted != DateTime.MinValue))
                        {
                            oldRecord = true;
                        }
                        else
                        {
                            rowStatusData.TimeStarted = Convert.ToDateTime(rowData[3]);
                        }

                        // Check Time Complete if older than history limit
                        DateTime timeCompleted = Convert.ToDateTime(rowData[4]);
                        if (((DateTime.Now - timeCompleted).TotalDays > logFileHistory) && (timeCompleted != DateTime.MinValue))
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
                        if (StaticData.ShutdownFlag == true)
                        {
                            Console.WriteLine("Shutdown CheckLogFileHistory job {0} row {1} time {21:HH:mm:ss.fff}", rowStatusData.Job, rowStatusData, DateTime.Now);
                            return;
                        }
                    }
                }

                // Create new csv file with new data
                using (TextWriter writer = new StreamWriter(logFileName))
                {
                    for (int i = 0; i < statusDataTable.Count; i++)
                    {
                        writer.WriteLine("{0},{1},{2},{3},{4}",
                            statusDataTable[i].Job, statusDataTable[i].JobStatus.ToString(),
                            statusDataTable[i].TimeReceived, statusDataTable[i].TimeStarted, statusDataTable[i].TimeCompleted);
                    }

                    writer.Close();
                }
            }
        }
    }
}
