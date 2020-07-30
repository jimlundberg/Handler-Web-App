using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Xml;
using System.Threading;
using System.Net.Sockets;
using System.Timers;
using StatusModels;
using ReadWriteCsvFile;

/// <summary>
/// Read Write CSV Files handler
/// </summary>
namespace ReadWriteCsvFile
{
    /// <summary>
    /// Class to store one CSV row
    /// </summary>
    public class CsvRow : List<String>
    {
        public String LineText { get; set; }
    }

    /// <summary>
    /// Class to write data to a CSV file
    /// </summary>
    public class CsvFileWriter : StreamWriter
    {
        public CsvFileWriter(Stream stream) : base(stream) { }

        public CsvFileWriter(String filename) : base(filename) { }

        /// <summary>
        /// Writes a single row to a CSV file.
        /// </summary>
        /// <param name="row">The row to be written</param>
        public void WriteRow(CsvRow row)
        {
            StringBuilder builder = new StringBuilder();
            bool firstColumn = true;
            foreach (String value in row)
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
        public CsvFileReader(Stream stream) : base(stream) { }

        public CsvFileReader(String filename) : base(filename) { }

        /// <summary>
        /// Reads a row of data from a CSV file
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public bool ReadRow(CsvRow row)
        {
            row.LineText = ReadLine();
            if (String.IsNullOrEmpty(row.LineText))
            {
                return false;
            }

            int pos = 0;
            int rows = 0;

            while (pos < row.LineText.Length)
            {
                String value;

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

/// <summary>
/// Status data services
/// </summary>
namespace Status.Services
{
    public static class Counters
    {
        public static int NumberOfJobsExecuting = 0;

        public static void IncrementNumberOfJobsExecuting()
        {
            NumberOfJobsExecuting++;
        }

        public static void DecrementNumberOfJobsExecuting()
        {
            NumberOfJobsExecuting--;
        }
    }

    /// <summary>
    /// Class to Read and Ini file data
    /// </summary>
    public class IniFileHandler
    {
        String Path;
        readonly String EXE = Assembly.GetExecutingAssembly().GetName().Name;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern long WritePrivateProfileString(String Key, String Section, String Value, String FilePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(String Key, String Section, String Default, StringBuilder RetVal, int Size, String FilePath);

        public IniFileHandler(String IniPath = null)
        {
            Path = new FileInfo(IniPath ?? EXE + ".ini").FullName;
        }

        public String Read(String Section, String Key = null)
        {
            StringBuilder RetVal = new StringBuilder(255);
            int length = GetPrivateProfileString(Section ?? EXE, Key, "", RetVal, 255, Path);
            return RetVal.ToString();
        }

        public void Write(String Key, String Value, String Section = null)
        {
            WritePrivateProfileString(Section ?? EXE, Key, Value, Path);
        }

        public void DeleteKey(String Key, String Section = null)
        {
            Write(Key, null, Section ?? EXE);
        }

        public void DeleteSection(String Section = null)
        {
            Write(null, null, Section ?? EXE);
        }

        public bool KeyExists(String Key, String Section = null)
        {
            return Read(Key, Section).Length > 0;
        }
    }

    /// <summary>
    /// Class to Scan a directory for new directories
    /// </summary>
    public class ScanDirectory
    {
        public String DirectoryName;
        public String JobDirectory;
        public String JobSerialNumber;
        public String Job;
        public String TimeStamp;
        public String XmlFileName;

        /// <summary>
        /// ScanDirectory constructor
        /// </summary>
        /// <param name="directoryName"></param>
        public ScanDirectory(String directoryName)
        {
            // Save directory name for class use
            DirectoryName = directoryName;
        }

        /// <summary>
        /// Get the Job XML data
        /// </summary>
        /// <param name="jobDirectory"></param>
        /// <returns></returns>
        public StatusModels.JobXmlData GetJobXmlData(String jobDirectory)
        {
            StatusModels.JobXmlData jobScanData = new StatusModels.JobXmlData();
            jobScanData.JobDirectory = jobDirectory;
            jobScanData.Job = jobScanData.JobDirectory.Remove(0, DirectoryName.Length + 1);
            jobScanData.JobSerialNumber = jobScanData.Job.Substring(0, jobScanData.Job.IndexOf("_"));
            int start = jobScanData.Job.IndexOf("_") + 1;
            jobScanData.TimeStamp = jobScanData.Job.Substring(start, jobScanData.Job.Length - start);

            // Wait until the Xml file shows up
            bool XmlFileFound = false;
            do
            {
                String[] files = System.IO.Directory.GetFiles(jobScanData.JobDirectory, "*.xml");
                if (files.Length > 0)
                {
                    jobScanData.XmlFileName = Path.GetFileName(files[0]);
                    XmlFileFound = true;
                    return jobScanData;
                }

                Thread.Sleep(500);
            }
            while (XmlFileFound == false);

            return jobScanData;
        }
    }

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
                Console.WriteLine("{0} has {1} files of {2} at {3} min {4} sec",
                    monitoredDir, numberOfFilesFound, numberOfFilesNeeded,
                    ((numberOfSeconds * (scanTime / 1000)) / 60), ((numberOfSeconds * (scanTime / 1000)) % 60));

                if (numberOfFilesFound >= numberOfFilesNeeded)
                {
                    Console.WriteLine("Recieved all {0} files", numberOfFilesFound);
                    return true;
                }

                Thread.Sleep(scanTime);
                numberOfSeconds++;
            } 
            while ((filesFound == false) && (numberOfSeconds < timeout));

            return false;
        }
    }

    /// <summary>
    /// Class for file copy, move and delete handling
    /// </summary>
    public class FileHandling
    {
        /// <summary>
        /// Copy a directory from source to target
        /// </summary>
        /// <param name="sourceDirectory"></param>
        /// <param name="targetDirectory"></param>
        public static void CopyDir(String sourceDirectory, String targetDirectory)
        {
            DirectoryInfo Source = new DirectoryInfo(sourceDirectory);
            DirectoryInfo Target = new DirectoryInfo(targetDirectory);
            CopyAllFiles(Source, Target);
        }

        /// <summary>
        /// CopyFolderContents - Copy files and folders from source to destination and optionally remove source files/folders
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destinationPath"></param>
        /// <param name="removeSource"></param>
        /// <param name="overwrite"></param>
        public static void CopyFolderContents(string sourcePath, string destinationPath, bool removeSource = false, bool overwrite = false)
        {
            DirectoryInfo sourceDI = new DirectoryInfo(sourcePath);
            DirectoryInfo destinationDI = new DirectoryInfo(destinationPath);

            if (!destinationDI.Exists)
            {
                destinationDI.Create();
            }

            // Copy files one by one
            FileInfo[] sourceFiles = sourceDI.GetFiles();
            foreach (FileInfo sourceFile in sourceFiles)
            {
                // This is the destination folder plus the new filename
                FileInfo destFile = new FileInfo(Path.Combine(destinationDI.FullName, sourceFile.Name));

                if (destFile.Exists)
                {
                    if (overwrite) destFile.Delete();

                    sourceFile.CopyTo(Path.Combine(destinationDI.FullName, sourceFile.Name));
                }
                else
                {
                    sourceFile.CopyTo(Path.Combine(destinationDI.FullName, sourceFile.Name));
                }

                // Finally, delete the source file if removeSource is true
                if (removeSource) sourceFile.Delete();
            }

            // Handle subdirectories
            DirectoryInfo[] dirs = sourceDI.GetDirectories();

            foreach (DirectoryInfo dir in dirs)
            {
                // Get destination folder
                string destination = Path.Combine(destinationDI.FullName, dir.Name);

                // Call CopyFolderContents() recursively
                // Overwrite doesn't matter in the case of a folder.  We just won't need to create it
                CopyFolderContents(dir.FullName, destination, removeSource, overwrite);

                if (removeSource) dir.Delete();
            }
        }

        /// <summary>
        /// Move a directory from source to target
        /// </summary>
        /// <param name="sourceDirectory"></param>
        /// <param name="targetDirectory"></param>
        public static void MoveDir(String sourceDirectory, String targetDirectory)
        {
            DirectoryInfo Source = new DirectoryInfo(sourceDirectory);
            DirectoryInfo Target = new DirectoryInfo(targetDirectory);
            if (Directory.Exists(targetDirectory))
            {
                try
                {
                    // Delete all files first
                    String[] files = Directory.GetFiles(targetDirectory);
                    foreach (String file in files)
                    {
                        File.Delete(file);
                        Console.WriteLine($"{file} is deleted.");
                    }

                    // Delete the Target directory
                    File.SetAttributes(targetDirectory, FileAttributes.Normal);
                    Thread.Sleep(250);
                    Target.Delete(true);
                    Thread.Sleep(250);
                }
                catch (UnauthorizedAccessException)
                {
                    // Bailing out to keep application running
                    Console.WriteLine("Failed to delete " + targetDirectory);
                }
            }

            Source.MoveTo(targetDirectory);
            Console.WriteLine(@"Copied {0} -> {1}", sourceDirectory, targetDirectory);
        }

        /// <summary>
        /// Copy file from source to target
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="targetFile"></param>
        public static void CopyFile(String sourceFile, String targetFile)
        {
            FileInfo Source = new FileInfo(sourceFile);
            if (File.Exists(targetFile))
            {
                File.Delete(targetFile);
                Thread.Sleep(500);
            }

            Source.CopyTo(targetFile);
            Console.WriteLine(@"Copied {0} -> {1}", sourceFile, targetFile);
        }

        /// <summary>
        /// Copy all files from source to target directory
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        public static void CopyAllFiles(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                Console.WriteLine(@"Copying {0} -> {1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo SourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(SourceSubDir.Name);
                CopyAllFiles(SourceSubDir, nextTargetSubDir);
            }
        }
    }

    /// <summary>
    /// Class to generate and execute a command line start of the Modeler executable
    /// </summary>
    public class CommandLineGenerator
    {
        private String cmd;
        private String Executable = "Executable";
        private String ProcessingDir = "Processing dir";
        private String StartPort = "Start Port";
        private String CpuCores = "Cpu Cores";

        public CommandLineGenerator() { }
        public String GetCurrentDirector() { return Directory.GetCurrentDirectory(); }
        public void SetExecutableFile(String _Executable) { Executable = _Executable; }
        public void SetRepositoryDir(String _ProcessingDir) { ProcessingDir = "-d " + _ProcessingDir; }
        public void SetStartPort(int _StartPort) { StartPort = "-s " + _StartPort.ToString(); }
        public void SetCpuCores(int _CpuCores) { CpuCores = "-p " + _CpuCores.ToString(); }
        public String AddToCommandLine(String addCmd) { return (cmd += addCmd); }

        /// <summary>
        /// Execute the Modeler command line
        /// </summary>
        public void ExecuteCommand()
        {
            var process = new Process();
            process.StartInfo.FileName = Executable;
            process.StartInfo.Arguments = String.Format(@"{0} {1} {2}", ProcessingDir, StartPort, CpuCores);
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            Console.WriteLine("\n{0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);
            process.Start();

            String outPut = process.StandardOutput.ReadToEnd();
            Console.WriteLine(outPut);

            process.WaitForExit();
            var exitCode = process.ExitCode;
            process.Close();
        }
    }

    /// <summary>
    /// Class to run the Command Line Generator
    /// </summary>
    public class CommandLineGeneratorThread
    {
        /// <summary>
        /// Object used in the task
        /// </summary>
        private CommandLineGenerator commandLineGenerator;

        /// <summary>
        /// The constructor obtains the object information 
        /// </summary>
        /// <param name="_commandLineGenerator"></param>
        public CommandLineGeneratorThread(CommandLineGenerator _commandLineGenerator)
        {
            commandLineGenerator = _commandLineGenerator;
        }

        /// <summary>
        /// The thread procedure performs the task using the command line object instance 
        /// </summary>
        public void ThreadProc()
        {
            commandLineGenerator.ExecuteCommand();
        }
    }

    /// <summary>
    /// Status Entry class
    /// </summary>
    public class StatusEntry
    {
        List<StatusWrapper.StatusData> StatusList;
        String Job;
        JobStatus Status;
        JobType TimeSlot;
        String LogFileName;

        public StatusEntry() { }

        /// <summary>
        /// StatusEntry Constructor
        /// </summary>
        /// <param name="statusList"></param>
        /// <param name="job"></param>
        /// <param name="status"></param>
        /// <param name="timeSlot"></param>
        /// <param name="logFileName"></param>
        public StatusEntry(List<StatusWrapper.StatusData> statusList, String job, JobStatus status, JobType timeSlot, String logFileName)
        {
            StatusList = statusList;
            Job = job;
            Status = status;
            TimeSlot = timeSlot;
            LogFileName = logFileName;
        }

        /// <summary>
        /// Log a Status and write to csv file
        /// </summary>
        /// <param name="statusList"></param>
        /// <param name="job"></param>
        /// <param name="status"></param>
        /// <param name="timeSlot"></param>
        public void ListStatus(List<StatusWrapper.StatusData> statusList, String job, JobStatus status, JobType timeSlot)
        {
            StatusWrapper.StatusData entry = new StatusWrapper.StatusData();
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
        /// Write Status data to the designated cvs data file
        /// </summary>
        /// <param name="statusList"></param>
        /// <param name="job"></param>
        /// <param name="status"></param>
        /// <param name="timeSlot"></param>
        public void WriteToCsvFile(String job, JobStatus status, JobType timeSlot, String logFileName)
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

        /// <summary>
        /// Read Status Data from CSV File
        /// </summary>
        /// <param name="logFileName"></param>
        /// <returns></returns>
        public List<StatusWrapper.StatusData> ReadFromCsvFile(String logFileName)
        {
            List<StatusWrapper.StatusData> statusDataTable = new List<StatusWrapper.StatusData>();
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
                        StatusWrapper.StatusData rowStatusData = new StatusWrapper.StatusData();
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
                    }
                }
            }

            // Return status table list
            return statusDataTable;
        }
    }

    /// <summary>
    /// TCP/IP Thread Class
    /// </summary>
    public class TcpIpThread
    {
        // State information used in the task.
        static public IniFileData IniData;
        static public StatusMonitorData MonitorData;
        static public List<StatusWrapper.StatusData> StatusData;

        // The constructor obtains the state information.
        public TcpIpThread(IniFileData iniData, StatusMonitorData monitorData, List<StatusWrapper.StatusData> statusData)
        {
            IniData = iniData;
            MonitorData = monitorData;
            StatusData = statusData;
        }

        // The thread procedure performs the task
        public void ThreadProc()
        {
            TcpIpMonitor(MonitorData.JobPortNumber);
        }

        public void StatusEntry(List<StatusWrapper.StatusData> statusList, String job, JobStatus status, JobType timeSlot)
        {
            StatusWrapper.StatusData entry = new StatusWrapper.StatusData();
            entry.Job = job;
            entry.JobStatus = status;
            switch (timeSlot)
            {
                case JobType.TIME_START:
                    entry.TimeStarted = DateTime.Now;
                    break;

                case JobType.TIME_RECEIVED:
                    entry.TimeReceived = DateTime.Now;
                    break;

                case JobType.TIME_COMPLETE:
                    entry.TimeCompleted = DateTime.Now;
                    break;
            }

            statusList.Add(entry);
            Console.WriteLine("Status: Job:{0} Job Status:{1} Time Type:{2}", job, status, timeSlot.ToString());
        }

        public void TcpIpMonitor(int TcpIpPortNumber)
        {
            TcpIpConnection tcpIpConnection = new TcpIpConnection();
            TcpIpConnection.PortNumber = MonitorData.JobPortNumber;
            TcpIpConnection.SetTimer();
            tcpIpConnection.Connect("127.0.0.1", TcpIpPortNumber, "status");
            TcpIpConnection.aTimer.Stop();
            TcpIpConnection.aTimer.Dispose();
        }

        /// <summary>
        /// Class to monitor and report status the TCP/IP connection to the Monitor application that is executing 
        /// </summary>
        public class TcpIpConnection
        {
            public static System.Timers.Timer aTimer;
            static public int PortNumber;

            /// <summary>
            /// Connect to TCP/IP Port
            /// </summary>
            /// <param name="server"></param>
            /// <param name="port"></param>
            /// <param name="message"></param>
            public void Connect(String server, Int32 port, String message)
            {
                try
                {
                    // Set current port number
                    PortNumber = port;

                    // Create a TcpClient.
                    // Note, for this client to work you need to have a TcpServer
                    // connected to the same address as specified by the server, port combination.
                    TcpClient client = new TcpClient(server, PortNumber);

                    // Translate the passed message into ASCII and store it as a Byte array.
                    Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);

                    // Get a client stream for reading and writing.
                    // Stream stream = client.GetStream();
                    NetworkStream stream = client.GetStream();

                    bool jobComplete = false;
                    int sleepTime = 15000;
                    do
                    {
                        // Send the message to the connected TcpServer.
                        stream.Write(data, 0, data.Length);

                        // Receive the TcpServer.response.
                        Console.WriteLine("Querying Modeler for Job {0} on port {1} at {2:HH:mm:ss.fff}", MonitorData.Job, PortNumber, DateTime.Now);
                        Console.WriteLine("Senting: {0}", message);

                        // Buffer to store the response bytes.
                        data = new Byte[256];

                        // String to store the response ASCII representation.
                        String responseData = String.Empty;

                        // Read the first batch of the TcpServer response bytes.
                        Int32 bytes = stream.Read(data, 0, data.Length);
                        responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);

                        // Send status for response received
                        switch (responseData)
                        {
                            case "Step 1 in process.":
                            case "Step 2 in process.":
                            case "Step 3 in process.":
                            case "Step 4 in process.":
                                Console.WriteLine("Received: {0} from Job {1}", responseData, MonitorData.Job);
                                break;

                            case "Step 5 in process.":
                            case "Step 6 in process.":
                                Console.WriteLine("Received: {0} from Job {1}", responseData, MonitorData.Job);
                                sleepTime = 1000;
                                break;

                            case "Whole process done, socket closed.":
                                Console.WriteLine("Received: {0} from Job {1}", responseData, MonitorData.Job);
                                jobComplete = true;
                                break;

                            default:
                                Console.WriteLine("$$$$$Received Weird Response: {0} from Job {1}", responseData, MonitorData.Job);
                                break;
                        }

                        Thread.Sleep(sleepTime);
                    }
                    while (jobComplete == false);

                    Console.WriteLine("Exiting TCP/IP Scan of Job {0}", MonitorData.Job);

                    // Close everything.
                    stream.Close();
                    client.Close();
                }
                catch (ArgumentNullException e)
                {
                    Console.WriteLine("ArgumentNullException: {0}", e);
                }
                catch (SocketException e)
                {
                    Console.WriteLine("SocketException: {0}", e);
                }

                Console.WriteLine("\n Press Enter to exit...");
                Console.Read();
            }

            public static void SetTimer()
            {
                // Create a timer with a five second interval.
                aTimer = new System.Timers.Timer(15000);

                // Hook up the Elapsed event for the timer. 
                aTimer.Elapsed += OnTimedEvent;
                aTimer.AutoReset = true;
                aTimer.Enabled = true;
            }

            public static void OnTimedEvent(Object source, ElapsedEventArgs e)
            {
                // Check modeler status
                Console.ReadLine();
            }
        }
    }

    /// <summary>
    /// Class to run a Job as a thread
    /// </summary>
    public class JobRunThread
    {
        private IniFileData IniData;
        private StatusMonitorData MonitorData;
        private List<StatusWrapper.StatusData> StatusData;
        private String DirectoryName;

        /// <summary>
        /// Job Run Thread constructor obtains the state information
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></param>
        /// <param name="statusData"></param>
        public JobRunThread(String directory, IniFileData iniData, StatusMonitorData monitorData, List<StatusWrapper.StatusData> statusData)
        {
            IniData = iniData;
            MonitorData = monitorData;
            StatusData = statusData;
            DirectoryName = directory;
        }

        // The thread procedure performs the task
        public void ThreadProc()
        {
            RunJob(DirectoryName, IniData, MonitorData, StatusData);
        }

        public void StatusDataEntry(List<StatusWrapper.StatusData> statusList, String job, JobStatus status, JobType timeSlot, String logFileName)
        {
            StatusEntry statusData = new StatusEntry(statusList, job, status, timeSlot, logFileName);
            statusData.ListStatus(statusList, job, status, timeSlot);
            statusData.WriteToCsvFile(job, status, timeSlot, logFileName);
        }

        /// <summary>
        /// Process of running  job
        /// </summary>
        /// <param name="scanDirectory"></param>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></param>
        /// <param name="statusData"></param>
        /// <param name="numberOfJobsExecuting"></param>
        public void RunJob(String scanDirectory, IniFileData iniData, StatusMonitorData monitorData, List<StatusWrapper.StatusData> statusData)
        {
            // Add initial entry to status list
            StatusDataEntry(statusData, monitorData.Job, JobStatus.JOB_STARTED, JobType.TIME_RECEIVED, iniData.LogFile);

            // Wait until Xml file is copied to the directory being scanned
            String job = monitorData.Job;
            String xmlFileName = scanDirectory + @"\" + job + @"\" + monitorData.XmlFileName;
            XmlDocument XmlDoc = new XmlDocument();
            try
            {
                // Read Job Xml file
                XmlDoc.Load(xmlFileName);
            }
            catch
            {
                throw new System.InvalidOperationException("Missing Xml File data");
            }

            // Get the top node of the Xml file
            XmlElement root = XmlDoc.DocumentElement;
            String TopNode = root.LocalName;

            // Get nodes for the number of files and names of files to transfer from Job .xml file
            XmlNode UnitNumberdNode = XmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/listitem/value");
            XmlNode ConsumedNode = XmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/FileConfiguration/Consumed");
            XmlNode ProducedNode = XmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/FileConfiguration/Produced");
            XmlNode TransferedNode = XmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/FileConfiguration/Transfered");
            XmlNode ModelerNode = XmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/FileConfiguration/Modeler");

            // Assign port number for this Job
            monitorData.JobPortNumber = iniData.StartPort + monitorData.JobIndex;

            // Get the modeler and number of files to transfer
            monitorData.UnitNumber = UnitNumberdNode.InnerText;
            monitorData.Modeler = ModelerNode.InnerText;
            monitorData.NumFilesConsumed = Convert.ToInt32(ConsumedNode.InnerText);
            monitorData.NumFilesProduced = Convert.ToInt32(ProducedNode.InnerText);
            int NumFilesToTransfer = 0;
            if (TransferedNode != null)
            {
                NumFilesToTransfer = Convert.ToInt32(TransferedNode.InnerText);
            }
            monitorData.NumFilesToTransfer = NumFilesToTransfer;

            // Get the modeler and number of files to transfer
            Console.WriteLine("Unit Number           = " + monitorData.UnitNumber);
            Console.WriteLine("Modeler               = " + monitorData.Modeler);
            Console.WriteLine("Num Files Consumed    = " + monitorData.NumFilesConsumed);
            Console.WriteLine("Num Files Produced    = " + monitorData.NumFilesProduced);
            Console.WriteLine("Num Files To Transfer = " + monitorData.NumFilesToTransfer);
            Console.WriteLine("Num Files To Transfer = " + monitorData.NumFilesToTransfer);
            Console.WriteLine("Job Port Number       = " + monitorData.JobPortNumber);

            // Add initial entry to status list
            StatusDataEntry(statusData, job, JobStatus.MONITORING_INPUT, JobType.TIME_START, iniData.LogFile);

            // Create the Transfered file list from the Xml file entries
            monitorData.transferedFileList = new List<String>(NumFilesToTransfer);
            List<XmlNode> TransFeredFileXml = new List<XmlNode>();
            monitorData.transferedFileList = new List<String>();
            for (int i = 1; i < NumFilesToTransfer + 1; i++)
            {
                String transferFileNodeName = ("/" + TopNode + "/FileConfiguration/Transfered" + i.ToString());
                XmlNode TransferedFileXml = XmlDoc.DocumentElement.SelectSingleNode(transferFileNodeName);
                monitorData.transferedFileList.Add(TransferedFileXml.InnerText);
                Console.WriteLine("Transfer File{0}        = {1}", i, TransferedFileXml.InnerText);
            }

            // If the directory is the Input Buffer, move the directory to Processing
            String InputBufferDir = monitorData.JobDirectory;
            String ProcessingBufferDir = iniData.ProcessingDir + @"\" + job;

            // If this job comes from the Input directory, run the scan and copy
            if (scanDirectory == iniData.InputDir)
            {
                // Monitor the Input directory until it has the total number of consumed files
                if (Directory.Exists(InputBufferDir))
                {
                    MonitorDirectoryFiles.MonitorDirectory(
                        InputBufferDir, monitorData.NumFilesConsumed, IniData.MaxTimeLimit, IniData.ScanTime);
                }
                else
                {
                    throw new System.InvalidOperationException("Could not find Input Buffer Directory ");
                }

                // Add entry to status list
                StatusDataEntry(statusData, job, JobStatus.COPYING_TO_PROCESSING, JobType.TIME_START, iniData.LogFile);

                // Move files from Input directory to the Processing directory, creating it first if needed
                FileHandling.MoveDir(InputBufferDir, ProcessingBufferDir);
            }

            // Add entry to status list
            StatusDataEntry(statusData, job, JobStatus.EXECUTING, JobType.TIME_START, iniData.LogFile);

            // Load and execute command line generator
            CommandLineGenerator cl = new CommandLineGenerator();
            cl.SetExecutableFile(iniData.ModelerRootDir + @"\" + monitorData.Modeler + @"\" + monitorData.Modeler + ".exe");
            cl.SetRepositoryDir(ProcessingBufferDir);
            cl.SetStartPort(monitorData.JobPortNumber);
            cl.SetCpuCores(iniData.CPUCores);
            CommandLineGeneratorThread commandLinethread = new CommandLineGeneratorThread(cl);
            Thread modelerThread = new Thread(new ThreadStart(commandLinethread.ThreadProc));
            modelerThread.Start();

            Console.WriteLine("***** Started Job {0} with Modeler {1} on port {2} with {3} CPU's",
                monitorData.Job, monitorData.Modeler, monitorData.JobPortNumber, iniData.CPUCores);

            // Wait for Modeler application to start
            Thread.Sleep(30000);

            // Start TCP/IP monitor thread
            TcpIpThread tcpIpThread = new TcpIpThread(iniData, monitorData, statusData);
            tcpIpThread.TcpIpMonitor(monitorData.JobPortNumber);

            Console.WriteLine("\n***** Started Tcp/Ip monitor of Job {0} with on port {1}", monitorData.Job, monitorData.JobPortNumber);

            // Add entry to status list
            StatusDataEntry(statusData, job, JobStatus.MONITORING_PROCESSING, JobType.TIME_START, iniData.LogFile);

            // Monitor for complete set of files in the Processing Buffer
            Console.WriteLine("Monitoring for Processing output files...");
            int NumOfFilesThatNeedToBeGenerated = monitorData.NumFilesConsumed + monitorData.NumFilesProduced;
            if (MonitorDirectoryFiles.MonitorDirectory(
                ProcessingBufferDir, NumOfFilesThatNeedToBeGenerated, iniData.MaxTimeLimit, iniData.ScanTime))
            {
                // Add copy entry to status list
                StatusDataEntry(statusData, job, JobStatus.COPYING_TO_ARCHIVE, JobType.TIME_START, iniData.LogFile);

                // Check .Xml output file for pass/fail
                bool XmlFileFound = false;

                // Check for Data.xml in the Processing Directory
                do
                {
                    String[] files = System.IO.Directory.GetFiles(ProcessingBufferDir, "Data.xml");
                    if (files.Length > 0)
                    {
                        xmlFileName = files[0];
                        XmlFileFound = true;
                    }

                    Thread.Sleep(500);
                }
                while (XmlFileFound == false);

                // Read output Xml file data
                XmlDocument XmlOutputDoc = new XmlDocument();
                XmlDoc.Load(xmlFileName);

                // Get the pass or fail data from the OverallResult node
                XmlNode OverallResult = XmlDoc.DocumentElement.SelectSingleNode("/Data/OverallResult/result");
                String passFail = OverallResult.InnerText;
                if (passFail == "Pass")
                {
                    // If the Finished directory does not exist, create it
                    if (!System.IO.Directory.Exists(iniData.FinishedDir + @"\" + monitorData.JobSerialNumber))
                    {
                        System.IO.Directory.CreateDirectory(iniData.FinishedDir + @"\" + monitorData.JobSerialNumber);
                    }

                    // Copy the Transfered files to the Finished directory 
                    for (int i = 0; i < monitorData.NumFilesToTransfer; i++)
                    {
                        FileHandling.CopyFile(iniData.ProcessingDir + @"\" + job + @"\" + monitorData.transferedFileList[i],
                            iniData.FinishedDir + @"\" + monitorData.JobSerialNumber + @"\" + monitorData.transferedFileList[i]);
                    }

                    // Move Processing Buffer Files to the Repository directory when passed
                    FileHandling.MoveDir(ProcessingBufferDir, iniData.RepositoryDir + @"\" + monitorData.Job);
                }
                else if (passFail == "Fail")
                {
                    // If the Error directory does not exist, create it
                    if (!System.IO.Directory.Exists(iniData.ErrorDir + @"\" + monitorData.JobSerialNumber))
                    {
                        System.IO.Directory.CreateDirectory(iniData.ErrorDir + @"\" + monitorData.JobSerialNumber);
                    }

                    // Copy the Transfered files to the Error directory 
                    for (int i = 0; i < monitorData.NumFilesToTransfer; i++)
                    {
                        FileHandling.CopyFile(iniData.ProcessingDir + @"\" + job + @"\" + monitorData.transferedFileList[i],
                            iniData.ErrorDir + @"\" + monitorData.JobSerialNumber + @"\" + monitorData.transferedFileList[i]);
                    }

                    // Move Processing Buffer Files to the Repository directory when failed
                    FileHandling.MoveDir(ProcessingBufferDir, iniData.RepositoryDir + @"\" + monitorData.Job);
                }

                Counters.DecrementNumberOfJobsExecuting();
                Console.WriteLine("-----Job {0} Complete, decrementing job count to {1}", monitorData.Job, Counters.NumberOfJobsExecuting);

                // Add entry to status list
                StatusDataEntry(statusData, job, JobStatus.COMPLETE, JobType.TIME_COMPLETE, iniData.LogFile);
            }
        }
    }

    /// <summary>
    /// Status Data storage object
    /// </summary>
    public class StatusRepository : IStatusRepository
    {
        private ProcessThread processThread;
        private IniFileData iniFileData = new IniFileData();
        private List<StatusMonitorData> monitorData = new List<StatusMonitorData>();
        private List<StatusWrapper.StatusData> statusList = new List<StatusWrapper.StatusData>();
        private StatusWrapper.StatusData statusData = new StatusWrapper.StatusData();
        public int GlobalJobIndex = 0;
        private bool RunStop = true;

        /// <summary>
        /// Scan for Unfinished jobs in the Processing Buffer
        /// </summary>
        public void ScanForUnfinishedJobs()
        {
            StatusModels.JobXmlData jobXmlData = new StatusModels.JobXmlData();
            DirectoryInfo directory = new DirectoryInfo(iniFileData.ProcessingDir);
            DirectoryInfo[] subdirs = directory.GetDirectories();
            if ((subdirs.Length != 0) && (Counters.NumberOfJobsExecuting < iniFileData.ExecutionLimit))
            {
                Console.WriteLine("\nFound unfinished jobs...");
                for (int i = 0; i < subdirs.Length; i++)
                {
                    String job = subdirs[i].Name;

                    // Delete the data.xml file if present
                    String dataXmlFile = iniFileData.ProcessingDir + @"\" + job + @"\" + "data.xml";
                    if (File.Exists(dataXmlFile))
                    {
                        File.Delete(dataXmlFile);
                    }

                    // Start scan for job files in the Output Buffer
                    ScanDirectory scanDir = new ScanDirectory(iniFileData.ProcessingDir);
                    jobXmlData = scanDir.GetJobXmlData(iniFileData.ProcessingDir + @"\" + job);

                    // Get data found in Xml file into Monitor Data
                    StatusModels.StatusMonitorData data = new StatusModels.StatusMonitorData();
                    data.Job = jobXmlData.Job;
                    data.JobDirectory = jobXmlData.JobDirectory;
                    data.JobSerialNumber = jobXmlData.JobSerialNumber;
                    data.TimeStamp = jobXmlData.TimeStamp;
                    data.XmlFileName = jobXmlData.XmlFileName;
                    data.JobIndex = GlobalJobIndex++;

                    // Display Monitor Data found
                    Console.WriteLine("");
                    Console.WriteLine("Found unfinished Job  = " + data.Job);
                    Console.WriteLine("New Job Directory     = " + data.JobDirectory);
                    Console.WriteLine("New Serial Number     = " + data.JobSerialNumber);
                    Console.WriteLine("New Time Stamp        = " + data.TimeStamp);
                    Console.WriteLine("New Job Xml File      = " + data.XmlFileName);

                    if (Counters.NumberOfJobsExecuting < iniFileData.ExecutionLimit)
                    {
                        // Increment counts to track job execution and port id
                        Counters.IncrementNumberOfJobsExecuting();
                        data.ExecutionCount++;

                        Console.WriteLine("+++++Job {0} Executing {1}", data.Job, Counters.NumberOfJobsExecuting);

                        JobRunThread jobThread = new JobRunThread(iniFileData.ProcessingDir, iniFileData, data, statusList);

                        // Create a thread to execute the task, and then start the thread.
                        Thread t = new Thread(new ThreadStart(jobThread.ThreadProc));
                        Console.WriteLine("Starting Job " + data.Job);
                        t.Start();
                        Thread.Sleep(30000);
                    }
                    else
                    {
                        Console.WriteLine("Job {0} Executing {1} Exceeded Execution Limit of {2}",
                            data.Job, Counters.NumberOfJobsExecuting, iniFileData.ExecutionLimit);
                        Thread.Sleep(iniFileData.ScanTime);
                    }

                    // If Stop button pressed, set RunStop Flag to false to stop
                    if (RunStop == false)
                    {
                        return;
                    }
                }
            }
            else
            {
                Console.WriteLine("\nNo Unfinished Jobs found");
            }
        }

        /// <summary>
        /// Class to run the whole monitoring process as a thread
        /// </summary>
        public class ProcessThread
        {
            // State information used in the task.
            private IniFileData IniData;
            private List<StatusWrapper.StatusData> StatusData;
            public volatile bool endProcess = false;
            private int GlobalJobIndex = 0;

            // The constructor obtains the state information.
            /// <summary>
            /// Process Thread constructor receiving data buffers
            /// </summary>
            /// <param name="iniData"></param>
            /// <param name="statusData"></param>
            /// <param name="globalJobIndex"></param>
            /// <param name="numberOfJobsRunning"></param>
            public ProcessThread(IniFileData iniData, List<StatusWrapper.StatusData> statusData, int globalJobIndex)
            {
                IniData = iniData;
                StatusData = statusData;
                GlobalJobIndex = globalJobIndex;
            }

            /// <summary>
            /// Method to set flag to stop the monitoring process
            /// </summary>
            public void StopProcess()
            {
                endProcess = true;
            }

            /// <summary>
            /// Method to scan for new jobs in the Input Buffer
            /// </summary>
            public void ScanForNewJobs()
            {
                endProcess = false;
                StatusModels.JobXmlData jobXmlData = new StatusModels.JobXmlData();
                DirectoryInfo directory = new DirectoryInfo(IniData.InputDir);
                List<String> directoryList = new List<String>();

                Console.WriteLine("\nWaiting for new job(s)...\n");

                while (endProcess == false) // Loop until flag set
                {
                    // Check if there are any directories
                    DirectoryInfo[] subdirs = directory.GetDirectories();
                    if (subdirs.Length != 0)
                    {
                        for (int i = 0; i < subdirs.Length; i++)
                        {
                            String job = subdirs[i].Name;

                            // Start scan for new directory in the Input Buffer
                            ScanDirectory scanDir = new ScanDirectory(IniData.InputDir);
                            jobXmlData = scanDir.GetJobXmlData(IniData.InputDir + @"\" + job);

                            // Set data found
                            StatusModels.StatusMonitorData data = new StatusModels.StatusMonitorData();
                            data.Job = jobXmlData.Job;
                            data.JobDirectory = jobXmlData.JobDirectory;
                            data.JobSerialNumber = jobXmlData.JobSerialNumber;
                            data.TimeStamp = jobXmlData.TimeStamp;
                            data.XmlFileName = jobXmlData.XmlFileName;
                            data.JobIndex = GlobalJobIndex++;

                            // Display data found
                            Console.WriteLine("");
                            Console.WriteLine("Found new Job         = " + data.Job);
                            Console.WriteLine("New Job Directory     = " + data.JobDirectory);
                            Console.WriteLine("New Serial Number     = " + data.JobSerialNumber);
                            Console.WriteLine("New Time Stamp        = " + data.TimeStamp);
                            Console.WriteLine("New Job Xml File      = " + data.XmlFileName);

                            if (Counters.NumberOfJobsExecuting <= IniData.ExecutionLimit)
                            {
                                // Increment counters to track job execution and port id
                                Counters.IncrementNumberOfJobsExecuting();
                                data.ExecutionCount++;

                                Console.WriteLine("+++++Job {0} Executing slot {1}", data.Job, Counters.NumberOfJobsExecuting);

                                // Supply the state information required by the task.
                                JobRunThread jobThread = new JobRunThread(IniData.InputDir, IniData, data, StatusData);

                                // Create a thread to execute the task, and then start the thread.
                                Thread t = new Thread(new ThreadStart(jobThread.ThreadProc));
                                Console.WriteLine("Starting Job " + data.Job);
                                t.Start();
                                Thread.Sleep(30000);
                            }
                            else
                            {
                                i--; // Retry job
                                Console.WriteLine("Job {0} job count {1} trying to exceeded Execution Limit of {2}",
                                    data.Job, Counters.NumberOfJobsExecuting, IniData.ExecutionLimit);
                                Thread.Sleep(IniData.ScanTime);
                            }
                        }
                    }

                    // Sleep to allow job to finish before checking for more
                    Thread.Sleep(IniData.ScanTime);
                }
                Console.WriteLine("\nExiting job Scan...");
            }
        }

        /// <summary>
        /// Get the Monitor Status Entry point
        /// </summary>
        /// <returns></returns>
        public void GetMonitorStatus()
        {
            RunStop = true;
            GlobalJobIndex = 0;

            // Scan for jobs not completed
            ScanForUnfinishedJobs();

            // Start scan for new jobs on it's own thread
            processThread = new ProcessThread(iniFileData, statusList, GlobalJobIndex);
            processThread.ScanForNewJobs();
        }

        /// <summary>
        /// Get the Monitor Status Entry point
        /// </summary>
        /// <returns></returns>
        public void GetIniFileData()
        {
            // Check that Config.ini file exists
            String IniFileName = "Config.ini";
            if (File.Exists(IniFileName) == false)
            {
                throw new System.InvalidOperationException("Config.ini file does not exist in the Handler directory");
            }

            // Get information from the Config.ini file
            var IniParser = new IniFileHandler(IniFileName);
            iniFileData.IniFileName = IniFileName;
            iniFileData.InputDir = IniParser.Read("Paths", "Input");
            iniFileData.ProcessingDir = IniParser.Read("Paths", "Processing");
            iniFileData.RepositoryDir = IniParser.Read("Paths", "Repository");
            iniFileData.FinishedDir = IniParser.Read("Paths", "Finished");
            iniFileData.ErrorDir = IniParser.Read("Paths", "Error");
            iniFileData.ModelerRootDir = IniParser.Read("Paths", "ModelerRootDir");
            iniFileData.CPUCores = Int32.Parse(IniParser.Read("Process", "CPUCores"));
            iniFileData.ExecutionLimit = Int32.Parse(IniParser.Read("Process", "ExecutionLimit"));
            iniFileData.StartPort = Int32.Parse(IniParser.Read("Process", "StartPort"));
            iniFileData.LogFile = IniParser.Read("Process", "LogFile");
            iniFileData.ScanTime = Int32.Parse(IniParser.Read("Process", "ScanTime"));
            String timeLimitString = IniParser.Read("Process", "MaxTimeLimit");
            iniFileData.MaxTimeLimit = Int32.Parse(timeLimitString.Substring(0, timeLimitString.IndexOf("#")));
            String logFileHistory = IniParser.Read("Process", "LogFileHistory");
            iniFileData.LogFileHistory = Int32.Parse(logFileHistory.Substring(0, logFileHistory.IndexOf("#")));

            Console.WriteLine("\nConfig.ini data found:");
            Console.WriteLine("Input Dir             = " + iniFileData.InputDir);
            Console.WriteLine("Processing Dir        = " + iniFileData.ProcessingDir);
            Console.WriteLine("Repository Dir        = " + iniFileData.RepositoryDir);
            Console.WriteLine("Finished Dir          = " + iniFileData.FinishedDir);
            Console.WriteLine("Error Dir             = " + iniFileData.ErrorDir);
            Console.WriteLine("Modeler Root Dir      = " + iniFileData.ModelerRootDir);
            Console.WriteLine("Log File              = " + iniFileData.LogFile);
            Console.WriteLine("CPU Cores             = " + iniFileData.CPUCores + " Cores");
            Console.WriteLine("Execution Limit       = " + iniFileData.ExecutionLimit + " Jobs");
            Console.WriteLine("Start Port            = " + iniFileData.StartPort);
            Console.WriteLine("Scan Time             = " + iniFileData.ScanTime + " Miliseconds");
            Console.WriteLine("Max Time Limit        = " + iniFileData.MaxTimeLimit + " Seconds");
            Console.WriteLine("Log File History      = " + iniFileData.LogFileHistory + " Days");
        }

        /// <summary>
        /// Method to Check the History of the log file
        /// </summary>
        public void CheckCsvFileHistory()
        {
//            ReadWriteCsvFile.CsvFileReader csv = new ReadWriteCsvFile.CsvFileReader();
//            csv.CheckCsvFileHistory(iniFileData.LogFile, iniFileData.LogFileHistory);
        }

        /// <summary>
        /// Method to stop the Monitor process
        /// </summary>
        public void StopMonitor()
        {
            if (processThread != null)
            {
                processThread.StopProcess();
            }
        }

        /// <summary>
        /// Method to return the status data to the requestor
        /// </summary>
        /// <returns></returns>
        public IEnumerable<StatusWrapper.StatusData> GetJobStatus()
        {
            return statusList;
        }

        /// <summary>
        /// Get csV history data
        /// </summary>
        /// <returns></returns>
        public IEnumerable<StatusWrapper.StatusData> GetHistoryData()
        {
            List<StatusWrapper.StatusData> statusList = new List<StatusWrapper.StatusData>();
            StatusEntry status = new StatusEntry();
            statusList = status.ReadFromCsvFile(iniFileData.LogFile);
            return statusList;
        }
    }
}
