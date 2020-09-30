using Microsoft.Extensions.Logging;
using ReadWriteCsvFile;
using Status.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Status.Services
{
    /// <summary>
    /// Handles csv file reading, writing, and expiration date checking
    /// </summary>
    public class CsvFileHandler
    {
        /// <summary>
        /// CSV File Handler default constructor
        /// </summary>
        public CsvFileHandler() { }

        /// <summary>
        /// Write Status data to the designated log file
        /// </summary>
        /// <param name="job"></param>
        /// <param name="status"></param>
        /// <param name="timeSlot"></param>
        public void WriteToCsvFile(string job, JobStatus status, JobType timeSlot)
        {
            using (StreamWriter writer = File.AppendText(StaticClass.IniData.StatusLogFile))
            {
                DateTime timeReceived = new DateTime();
                if (timeReceived == null)
                {
                    StaticClass.Logger.LogError("WriteToCsvFile timeReceived failed to instantiate");
                }

                DateTime timeStarted = new DateTime();
                if (timeStarted == null)
                {
                    StaticClass.Logger.LogError("WriteToCsvFile timeStarted failed to instantiate");
                }

                DateTime timeCompleted = new DateTime();
                if (timeCompleted == null)
                {
                    StaticClass.Logger.LogError("WriteToCsvFile timeCompleted failed to instantiate");
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

                string line = string.Format("{0},{1},{2},{3},{4}", job, status.ToString(), timeReceived, timeStarted, timeCompleted);

                writer.WriteLineAsync(line);
            }
        }

        /// <summary>
        /// Read Status Data from CSV File 
        /// </summary>
        /// <returns>Status List from CSV</returns>
        public List<StatusData> ReadFromCsvFile()
        {
            List<StatusData> statusDataTable = new List<StatusData>();
            string statusLogFile = StaticClass.IniData.StatusLogFile;

            if (File.Exists(statusLogFile) == true)
            {
                using (CsvFileReader reader = new CsvFileReader(statusLogFile))
                {
                    if (reader == null)
                    {
                        StaticClass.Logger.LogError("ReadFromCsvFile reader failed to instantiate");
                        return null;
                    }

                    CsvRow rowData = new CsvRow();
                    while (reader.ReadRow(rowData))
                    {
                        StatusData rowStatusData = new StatusData();
                        if (rowStatusData == null)
                        {
                            StaticClass.Logger.LogError("ReadFromCsvFile rowStatusData failed to instantiate");
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
                        if (StaticClass.ShutDownPauseCheck("ReadFromCsvFile") == true)
                        {
                            return null;
                        }
                    }
                }

                // Return status table list
                return statusDataTable;
            }

            return null;
        }

        /// <summary>
        /// Read, delete old data and rewrite Log File
        /// </summary>
        public void CheckLogFileHistory()
        {
            List<StatusData> statusDataTable = new List<StatusData>();
            string logFileName = StaticClass.IniData.StatusLogFile;

            if (File.Exists(logFileName) == true)
            {
                using (CsvFileReader reader = new CsvFileReader(logFileName))
                {
                    if (reader == null)
                    {
                        StaticClass.Logger.LogError("CsvFileHandler reader failed to instantiate");
                        return;
                    }

                    CsvRow rowData = new CsvRow();
                    if (rowData == null)
                    {
                        StaticClass.Logger.LogError("CsvFileHandler rowData failed to instantiate");
                        return;
                    }

                    while (reader.ReadRow(rowData))
                    {
                        StatusData rowStatusData = new StatusData();
                        if (rowStatusData == null)
                        {
                            StaticClass.Logger.LogError("CsvFileHandler rowStatusData failed to instantiate");
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
                        if ((timeReceived < DateTime.Now.AddDays(-StaticClass.IniData.LogFileHistoryLimit)) && (timeReceived != DateTime.MinValue))
                        {
                            oldRecord = true;
                        }
                        else
                        {
                            rowStatusData.TimeReceived = Convert.ToDateTime(rowData[2]);
                        }

                        // Check Time Started if older than history limit
                        DateTime timeStarted = Convert.ToDateTime(rowData[3]);
                        if ((timeStarted < DateTime.Now.AddDays(-StaticClass.IniData.LogFileHistoryLimit)) && (timeStarted != DateTime.MinValue))
                        {
                            oldRecord = true;
                        }
                        else
                        {
                            rowStatusData.TimeStarted = Convert.ToDateTime(rowData[3]);
                        }

                        // Check Time Complete if older than history limit
                        DateTime timeCompleted = Convert.ToDateTime(rowData[4]);
                        if ((timeCompleted < DateTime.Now.AddDays(-StaticClass.IniData.LogFileHistoryLimit)) && (timeCompleted != DateTime.MinValue))
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
                        if (StaticClass.ShutDownPauseCheck("CheckLogFileHistory") == true)
                        {
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
                            statusDataTable[i].Job,
                            statusDataTable[i].JobStatus.ToString(),
                            statusDataTable[i].TimeReceived,
                            statusDataTable[i].TimeStarted,
                            statusDataTable[i].TimeCompleted);
                    }
                    writer.Close();
                }
            }
        }
    }
}

/// <summary>
/// Separate Read/Write CSV file handler Namespace
/// </summary>
namespace ReadWriteCsvFile
{
    /// <summary>
    /// Class to store one CSV row
    /// </summary>
    public class CsvRow : List<string>
    {
        public string LineText { get; set; }
    }

    /// <summary>
    /// Class to write data to a CSV file
    /// </summary>
    public class CsvFileWriter : StreamWriter
    {
        /// <summary>
        /// CSV FIle Writer Constructor
        /// </summary>
        /// <param name="stream"></param>
        public CsvFileWriter(Stream stream) : base(stream) { }

        /// <summary>
        /// CSV File Writer Constructor
        /// </summary>
        /// <param name="filename"></param>
        public CsvFileWriter(string filename) : base(filename) { }

        /// <summary>
        /// Writes a single row to a CSV file.
        /// </summary>
        /// <param name="row">The row to be written</param>
        public void WriteRow(CsvRow row)
        {
            StringBuilder builder = new StringBuilder();

            bool firstColumn = true;
            foreach (string value in row)
            {
                // Add separator if this isn't the first value
                if (!firstColumn)
                {
                    builder.Append(',');
                }

                // Implement special handling for values that contain comma or quote
                // Enclose in quotes and double up any double quotes
                if (value.IndexOfAny(new char[] { '"', ',' }) != -1)
                {
                    builder.AppendFormat("\"{0}\"", value.Replace("\"", "\"\""));
                }
                else
                {
                    builder.Append(value);
                }
                firstColumn = false;
            }
            row.LineText = builder.ToString();
            WriteLine(row.LineText);
        }
    }

    /// <summary>
    /// Class to read data from a CSV file
    /// </summary>
    public class CsvFileReader : StreamReader
    {
        /// <summary>
        /// Csv File Reader constructor
        /// </summary>
        /// <param name="stream"></param>
        public CsvFileReader(Stream stream) : base(stream) { }

        /// <summary>
        /// CSV File Reader constructor
        /// </summary>
        /// <param name="filename"></param>
        public CsvFileReader(string filename) : base(filename) { }

        /// <summary>
        /// Reads a row of data from a CSV file
        /// </summary>
        /// <param name="row"></param>
        /// <returns>pass or fail</returns>
        public bool ReadRow(CsvRow row)
        {
            row.LineText = ReadLine();
            if (string.IsNullOrEmpty(row.LineText))
            {
                return false;
            }

            int pos = 0;
            int rows = 0;

            while (pos < row.LineText.Length)
            {
                string value;

                // Special handling for quoted field
                if (row.LineText[pos] == '"')
                {
                    // Skip initial quote
                    pos++;

                    // Parse quoted value
                    int start = pos;
                    while (pos < row.LineText.Length)
                    {
                        // Test for quote character
                        if (row.LineText[pos] == '"')
                        {
                            // Found one
                            pos++;

                            // If two quotes together, keep one
                            // Otherwise, indicates end of value
                            if (pos >= row.LineText.Length || row.LineText[pos] != '"')
                            {
                                pos--;
                                break;
                            }
                        }
                        pos++;
                    }
                    value = row.LineText.Substring(start, pos - start);
                    value = value.Replace("\"\"", "\"");
                }
                else
                {
                    // Parse unquoted value
                    int start = pos;
                    while (pos < row.LineText.Length && row.LineText[pos] != ',')
                    {
                        pos++;
                    }
                    value = row.LineText.Substring(start, pos - start);
                }

                // Add field to list
                if (rows < row.Count)
                {
                    row[rows] = value;
                }
                else
                {
                    row.Add(value);
                }
                rows++;

                // Eat up to and including next comma
                while (pos < row.LineText.Length && row.LineText[pos] != ',')
                {
                    pos++;
                }
                if (pos < row.LineText.Length)
                {
                    pos++;
                }
            }

            // Delete any unused items
            while (row.Count > rows)
            {
                row.RemoveAt(rows);
            }

            // Return true if any columns read
            return (row.Count > 0);
        }
    }
}
