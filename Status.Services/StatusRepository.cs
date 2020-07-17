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
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;

namespace Status.Services
{
    public class IniFile
    {
        String Path;
        String EXE = Assembly.GetExecutingAssembly().GetName().Name;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern long WritePrivateProfileString(String Key, String Section, String Value, String FilePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(String Key, String Section, String Default, StringBuilder RetVal, int Size, String FilePath);

        public IniFile(String IniPath = null)
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
    public class ScanInputBufferDirectory
    {
        private readonly IEnumerable<String> CurrentDirectoryList;
        public String DirectoryName;
        public String JobDirectory;
        public String JobSerialNumber;
        public String Job;
        public String TimeStamp;
        public String XmlFileName;

        public ScanInputBufferDirectory(String directoryName)
        {
            // Save directory name for class use
            DirectoryName = directoryName;

            try
            {
                // Get Current baseline directory list
                CurrentDirectoryList = Directory.GetDirectories(directoryName, "*", SearchOption.TopDirectoryOnly);
                if (CurrentDirectoryList.Count() > 0)
                {
                    Console.WriteLine("\nFound the following jobs:");
                    foreach (String dir in CurrentDirectoryList)
                    {
                        Console.WriteLine(dir);
                    }
                }
                else 
                {
                    Console.WriteLine("\nWaiting for new job...");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Get current directory list failed: {0}", e.ToString());
            }
        }

        public bool ScanForNewJob()
        {
            try
            {
                // Get new directory list
                String[] newDirectoryList = Directory.GetDirectories(DirectoryName, "*", SearchOption.TopDirectoryOnly);
                IEnumerable<String> differenceQuery = newDirectoryList.Except(CurrentDirectoryList);
                foreach (String dir in differenceQuery)
                {
                    JobDirectory = dir;
                    Job = JobDirectory.Remove(0, DirectoryName.Length + 1);
                    JobSerialNumber = Job.Substring(0, Job.IndexOf("_"));
                    int start = Job.IndexOf("_") + 1;
                    TimeStamp = Job.Substring(start, Job.Length - start);
                    Console.WriteLine("Found new directory " + JobDirectory);

                    // Wait until the Xml file shows up
                    bool XmlFileFound = false;
                    do
                    {
                        String[] files = System.IO.Directory.GetFiles(dir, "*.xml");
                        if (files.Length > 0)
                        {
                            XmlFileName = Path.GetFileName(files[0]);
                            XmlFileFound = true;
                        }

                        Thread.Sleep(500);

                    }
                    while (XmlFileFound == false);

                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Get new directory list failed: {0}", e.ToString());
            }

            return false;
        }
    }

    public class MonitorDirectoryFiles
    {
        public static bool MonitorDirectory(String monitoredDir, int numberOfFilesNeeded, int timeout)
        {
            bool filesFound = false;
            int numberOfSeconds = 0;

            do
            {
                int numberOfFilesFound = Directory.GetFiles(monitoredDir, "*", SearchOption.TopDirectoryOnly).Length;
                Console.WriteLine("{0} has {1} files of {2} at {3} seconds",
                    monitoredDir, numberOfFilesFound, numberOfFilesNeeded, numberOfSeconds);
                if (numberOfFilesFound >= numberOfFilesNeeded)
                {
                    Console.WriteLine("Recieved all {0} files", numberOfFilesFound);
                    return true;
                }

                Thread.Sleep(1000);
                numberOfSeconds++;
            } 
            while ((filesFound == false) && (numberOfSeconds < timeout));

            return false;
        }
    }

    public class FileHandling
    {
        public static void CopyDir(String sourceDirectory, String targetDirectory)
        {
            DirectoryInfo Source = new DirectoryInfo(sourceDirectory);
            DirectoryInfo Target = new DirectoryInfo(targetDirectory);
            CopyAllFiles(Source, Target);
        }

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
                    Thread.Sleep(100);
                    Target.Delete(true);
                    Thread.Sleep(100);
                }
                catch (UnauthorizedAccessException)
                {
                    FileAttributes attributes = File.GetAttributes(targetDirectory);
                    if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        attributes &= ~FileAttributes.ReadOnly;
                        File.SetAttributes(targetDirectory, attributes);
                        Thread.Sleep(500);
                        File.Delete(targetDirectory);
                        Thread.Sleep(500);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            Source.MoveTo(targetDirectory);
            Console.WriteLine(@"Copied {0} -> {1}", sourceDirectory, targetDirectory);
        }

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

        public void ExecuteCommand()
        {
            var proc = new Process();
            proc.StartInfo.FileName = Executable;
            proc.StartInfo.Arguments = String.Format(@"{0} {1} {2}", ProcessingDir, StartPort, CpuCores);
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
          //Console.WriteLine("\n{0} {1}", proc.StartInfo.FileName, proc.StartInfo.Arguments);
            proc.Start();

            String outPut = proc.StandardOutput.ReadToEnd();
            Console.WriteLine(outPut);

            proc.WaitForExit();
            var exitCode = proc.ExitCode;
            proc.Close();
        }
    }

    public class CommandLineGeneratorThread
    {
        // Object used in the task.
        private CommandLineGenerator commandLineGenerator;

        // The constructor obtains the object information.
        public CommandLineGeneratorThread(CommandLineGenerator _commandLineGenerator)
        {
            commandLineGenerator = _commandLineGenerator;
        }

        // The thread procedure performs the task using the command line object instance
        public void ThreadProc()
        {
            commandLineGenerator.ExecuteCommand();
        }
    }

    public class TcpIpConnection
    {
        public static System.Timers.Timer aTimer;
        static public int PortNumber;

        static void Connect(String server, Int32 port, String message)
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

                // Send the message to the connected TcpServer.
                stream.Write(data, 0, data.Length);

                // Receive the TcpServer.response.
                Console.WriteLine("Sent: {0}", message);

                // Buffer to store the response bytes.
                data = new Byte[256];

                // String to store the response ASCII representation.
                String responseData = String.Empty;

                // Read the first batch of the TcpServer response bytes.
                Int32 bytes = stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Console.WriteLine("Received: {0}", responseData);

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
            aTimer = new System.Timers.Timer(60000);

            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        public static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            // Check modeler status
            Console.WriteLine("Modeler status sent at {0:HH:mm:ss.fff} on port {1}", e.SignalTime, PortNumber);
            Connect("127.0.0.1", PortNumber, "status");
            Console.ReadLine();
        }
    }

    public class JobRunThread
    {
        // State information used in the task.
        private StatusMonitorData MonitorData;
        private List<StatusData> StatusData;

        // The constructor obtains the state information.
        public JobRunThread(StatusMonitorData monitorData, List<StatusData> statusData)
        {
            MonitorData = monitorData;
            StatusData = statusData;
        }

        // The thread procedure performs the task
        public void ThreadProc()
        {
            RunJob(MonitorData, StatusData);
        }

        public void StatusEntry(List<StatusData> statusList, String job, JobStatus status, JobType timeSlot)
        {
            StatusData entry = new StatusData();
            entry.Job = job;
            entry.JobStatus = status;
            switch (timeSlot)
            {
                case JobType.TIME_START:
                    entry.TimeStarted = DateTime.Now;
                    break;

                case JobType.TIME_RECIEVED:
                    entry.TimeReceived = DateTime.Now;
                    break;

                case JobType.TIME_COMPLETE:
                    entry.TimeCompleted = DateTime.Now;
                    break;
            }

            statusList.Add(entry);
            Console.WriteLine("Status: Job:{0} Job Status:{1} Time Type:{2}", job, status, timeSlot.ToString());
        }

        public void RunJob(StatusMonitorData monitorData, List<StatusData> statusData)
        {
            // Add initial entry to status list
            StatusEntry(statusData, monitorData.Job, JobStatus.JOB_STARTED, JobType.TIME_RECIEVED);

            // Wait until Xml file is copied to the Processing directory
            String job = monitorData.Job;
            String xmlFileName = monitorData.InputDir + @"\" + job + @"\" + monitorData.XmlFileName;
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
            monitorData.JobPortNumber = monitorData.StartPort + monitorData.JobIndex;

            // Get the modeler and number of files to transfer
            monitorData.UnitNumber = UnitNumberdNode.InnerText;
            monitorData.Modeler = ModelerNode.InnerText;
            monitorData.NumFilesConsumed = Convert.ToInt32(ConsumedNode.InnerText);
            monitorData.NumFilesProduced = Convert.ToInt32(ProducedNode.InnerText);
            int NumFilesToTransfer = 0;
            NumFilesToTransfer = Convert.ToInt32(TransferedNode.InnerText);
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
            StatusEntry(statusData, job, JobStatus.MONITORING_INPUT, JobType.TIME_START);

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

            // Monitor the Input directory until it has the total number of consumed files
            String InputBufferDir = monitorData.JobDirectory + @"\" + job;
            if (Directory.Exists(InputBufferDir))
            {
                MonitorDirectoryFiles.MonitorDirectory(InputBufferDir, monitorData.NumFilesConsumed, monitorData.MaxTimeLimit);
            }
            else
            {
                throw new System.InvalidOperationException("Could not find Input Buffer Directory ");
            }

            // Add entry to status list
            StatusEntry(statusData, job, JobStatus.COPYING_TO_PROCESSING, JobType.TIME_START);

            // Move files from Input directory to the Processing directory, creating it first if needed
            String ProcessingBufferDir = monitorData.ProcessingDir + @"\" + job;
            FileHandling.MoveDir(InputBufferDir, ProcessingBufferDir);

            // Add entry to status list
            StatusEntry(statusData, job, JobStatus.EXECUTING, JobType.TIME_START);

            // Load and execute command line generator
            //CommandLineGenerator cl = new CommandLineGenerator();
            //cl.SetExecutableFile(monitorData.ModelerRootDir + @"\" + monitorData.Modeler + @"\" + monitorData.Modeler + ".exe");
            //cl.SetRepositoryDir(ProcessingBufferDir);
            //cl.SetStartPort(monitorData.JobPortNumber);
            //cl.SetCpuCores(monitorData.CPUCores);
            //CommandLineGeneratorThread commandLinethread = new CommandLineGeneratorThread(cl);
            //Thread thread = new Thread(new ThreadStart(commandLinethread.ThreadProc));
            //thread.Start();

            //do
            //{
            //    // Timed listen for Modeler TCP/IP response
            //    TcpIpConnection.SetTimer();
            //    String response;
            //    do
            //    {
            //        response = Console.ReadLine();
            //        Console.WriteLine("Scan TCP/IP at {0:HH:mm:ss.fff}", DateTime.Now);
            //        Console.WriteLine(response);

            //        // Not sure what the messages are yet
            //        switch (response)
            //        {
            //            case "Complete":
            //                break;
            //        }

            //        Thread.Sleep(5000);
            //    }
            //    while (response != "Complete");

            //    TcpIpConnection.aTimer.Stop();
            //    TcpIpConnection.aTimer.Dispose();
            //    Thread.Sleep(30000);
            //}
            //while (true);

            // Add entry to status list
            StatusEntry(statusData, job, JobStatus.MONITORING_PROCESSING, JobType.TIME_START);

            // Monitor for complete set of files in the Processing Buffer
            Console.WriteLine("Monitoring for Processing output files...");
            int NumOfFilesThatNeedToBeGenerated = monitorData.NumFilesConsumed + monitorData.NumFilesProduced;
            if (MonitorDirectoryFiles.MonitorDirectory(ProcessingBufferDir, NumOfFilesThatNeedToBeGenerated, monitorData.MaxTimeLimit))
            {
                // Add copy entry to status list
                StatusEntry(statusData, job, JobStatus.COPYING_TO_ARCHIVE, JobType.TIME_START);

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
                    bool exists = System.IO.Directory.Exists(monitorData.FinishedDir + @"\" + monitorData.JobSerialNumber);
                    if (!exists)
                    {
                        System.IO.Directory.CreateDirectory(monitorData.FinishedDir + @"\" + monitorData.JobSerialNumber);
                    }

                    // Copy the Transfered files to the Finished directory 
                    for (int i = 0; i < monitorData.NumFilesToTransfer; i++)
                    {
                        FileHandling.CopyFile(monitorData.ProcessingDir + @"\" + job + @"\" + monitorData.transferedFileList[i],
                                            monitorData.FinishedDir + @"\" + monitorData.JobSerialNumber + @"\" + monitorData.transferedFileList[i]);
                    }

                    // Move Processing Buffer Files to the Repository directory if passed
                    FileHandling.MoveDir(ProcessingBufferDir, monitorData.RepositoryDir + @"\" + monitorData.JobSerialNumber);
                }
                else if (passFail == "Fail")
                {
                    // Move fils to the Error directory if failed
                    FileHandling.CopyDir(ProcessingBufferDir, monitorData.ErrorDir + @"\" + job);
                }

                // Add entry to status list
                StatusEntry(statusData, job, JobStatus.COMPLETE, JobType.TIME_COMPLETE);
            }

            //_monitorList.Clear();
            //_monitorList.Add(monitorData);
        }
    }

    public class StatusRepository : IStatusRepository
    {
        private List<StatusMonitorData> _monitorList = new List<StatusMonitorData>();
        private List<StatusData> _statusList = new List<StatusData>();
        private StatusMonitorData monitorData = new StatusMonitorData();
        private StatusData statusData = new StatusData();
        private int GlobalJobIndex = 0;

        public void MonitorDataRepository()
        {
            _monitorList = new List<StatusMonitorData>()
            {
                new StatusMonitorData() {
                    Job = "Job Field",
                    JobIndex = GlobalJobIndex,
                    JobSerialNumber = "Job Serial Number Field",
                    TimeStamp = "Time Stamp Field",
                    JobDirectory = "Job Directory Field",
                    IniFileName = ".ini File Name Field",
                    InputDir = "Input File Name Field",
                    ProcessingDir = "Processing Directory Field",
                    RepositoryDir = "Archieve Directory Field",
                    FinishedDir = "Finished Directory Field",
                    ErrorDir = "Error Directory Field",
                    LogFile = "Log File Field",
                    ModelerRootDir = "Modeler Root Directory Field",
                    XmlFileName = ".xML File Name Field",
                    UnitNumber = "Unit Number Field",
                    Modeler = "Modeler Field",
                    CPUCores = 0,
                    ExecutionLimit = 0,
                    ExecutionCount = 0,
                    MaxTimeLimit = 0,
                    StartPort = 0,
                    JobPortNumber = 0,
                    NumFilesConsumed = 0,
                    NumFilesProduced = 0,
                    NumFilesToTransfer = 0,
                    transferedFileList = new List<String>()
                }
            };
        }

        public void StatuDataRepository()
        {
            _statusList = new List<StatusData>()
            {
                new StatusData() { Job = "Job Field", JobStatus = JobStatus.JOB_STARTED, TimeReceived = DateTime.Now,  TimeStarted = DateTime.Now,  TimeCompleted = DateTime.Now }
            };
        }

        public void StatusEntry(String job, JobStatus status, JobType timeSlot)
        {
            StatusData entry = new StatusData();
            entry.Job = job;
            entry.JobStatus = status;
            switch (timeSlot)
            {
                case JobType.TIME_START:
                    entry.TimeStarted = DateTime.Now;
                    break;

                case JobType.TIME_RECIEVED:
                    entry.TimeReceived = DateTime.Now;
                    break;

                case JobType.TIME_COMPLETE:
                    entry.TimeCompleted = DateTime.Now;
                    break;
            }
            _statusList.Add(entry);

            Console.WriteLine("Status: Job {0} Job Status {1} Job Type {2}", job, status, timeSlot.ToString());
        }

        public void ScanForJob()
        {
            while (true) // Loop all the time
            {
                // Start scan for new directory in the Input Buffer
                ScanInputBufferDirectory scanDir = new ScanInputBufferDirectory(monitorData.InputDir);
                bool foundNewJob = false;
                do
                {
                    foundNewJob = scanDir.ScanForNewJob();
                    Thread.Sleep(1000);
                }
                while (foundNewJob == false);

                // Set data found
                monitorData.Job = scanDir.Job;
                monitorData.JobDirectory = scanDir.DirectoryName;
                monitorData.JobSerialNumber = scanDir.JobSerialNumber;
                monitorData.TimeStamp = scanDir.TimeStamp;
                monitorData.XmlFileName = scanDir.XmlFileName;
                monitorData.JobIndex = GlobalJobIndex++;

                // Display data found
                Console.WriteLine("");
                Console.WriteLine("Found new Job         = " + monitorData.Job);
                Console.WriteLine("New Job Directory     = " + monitorData.JobDirectory);
                Console.WriteLine("New Serial Number     = " + monitorData.JobSerialNumber);
                Console.WriteLine("New Time Stamp        = " + monitorData.TimeStamp);
                Console.WriteLine("New Job Xml File      = " + monitorData.XmlFileName);

                // Increment execution count to track job by this as an index number
                monitorData.ExecutionCount++;

                if (monitorData.ExecutionCount <= monitorData.ExecutionLimit)
                {
                    // Supply the state information required by the task.
                    JobRunThread jobThread = new JobRunThread(monitorData, _statusList);

                    // Create a thread to execute the task, and then start the thread.
                    Thread t = new Thread(new ThreadStart(jobThread.ThreadProc));
                    t.Start();
                    Console.WriteLine("Starting Job " + monitorData.Job);
                    t.Join();
                    Console.WriteLine("Job {0} has completed", monitorData.Job);
                }
                else
                {
                    Console.WriteLine("Job {0} Index {1} Exceeded Execution Limit of {2}",
                        monitorData.Job, monitorData.ExecutionCount, monitorData.ExecutionLimit);
                }
            }
        }

        public IEnumerable<StatusMonitorData> GetMonitorStatus()
        {
            // Get initial data
            MonitorDataRepository();

            // Check that Config.ini file exists
            String IniFileName = @"C:\SSMCharacterizationHandler\Handler\Config.ini";
            if (File.Exists(IniFileName) == false)
            {
                throw new System.InvalidOperationException("Config.ini file does not exist in the Handler directory");
            }

            // Get information from the Config.ini file
            var IniParser = new IniFile(IniFileName);
            monitorData.IniFileName = IniFileName;
            monitorData.InputDir = IniParser.Read("Paths", "Input");
            monitorData.Modeler = IniParser.Read("Process", "Modeler");
            monitorData.ProcessingDir = IniParser.Read("Paths", "Processing");
            monitorData.RepositoryDir = IniParser.Read("Paths", "Repository");
            monitorData.FinishedDir = IniParser.Read("Paths", "Finished");
            monitorData.ErrorDir = IniParser.Read("Paths", "Error");
            monitorData.ModelerRootDir = IniParser.Read("Paths", "ModelerRootDir");
            monitorData.CPUCores = Int32.Parse(IniParser.Read("Process", "CPUCores"));
            monitorData.ExecutionLimit = Int32.Parse(IniParser.Read("Process", "ExecutionLimit"));
            monitorData.StartPort = Int32.Parse(IniParser.Read("Process", "StartPort"));
            monitorData.LogFile = IniParser.Read("Process", "LogFile");
            String timeLimitString = IniParser.Read("Process", "MaxTimeLimit");
            monitorData.MaxTimeLimit = Int32.Parse(timeLimitString.Substring(0, timeLimitString.IndexOf("#")));
            monitorData.ExecutionCount = 0;

            // Start scan for new jobs after page activation
            ScanForJob();

            return _monitorList;
        }

        public IEnumerable<StatusData> GetJobStatus()
        {
            return _statusList;
        }
    }
}
