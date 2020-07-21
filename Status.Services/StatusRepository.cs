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
    public class ScanDirectory
    {
        public String DirectoryName;
        public String JobDirectory;
        public String JobSerialNumber;
        public String Job;
        public String TimeStamp;
        public String XmlFileName;

        public ScanDirectory(String directoryName)
        {
            // Save directory name for class use
            DirectoryName = directoryName;
        }

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

    public class MonitorDirectoryFiles
    {
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
        private IniFileData IniData;
        private StatusMonitorData MonitorData;
        private List<StatusData> StatusData;
        private string DirectoryName;

        // The constructor obtains the state information.
        public JobRunThread(String directory, IniFileData iniData, StatusMonitorData monitorData, List<StatusData> statusData)
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

        public void RunJob(String scanDirectory, IniFileData iniData, StatusMonitorData monitorData, List<StatusData> statusData)
        {
            // Add initial entry to status list
            StatusEntry(statusData, monitorData.Job, JobStatus.JOB_STARTED, JobType.TIME_RECIEVED);

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
                StatusEntry(statusData, job, JobStatus.COPYING_TO_PROCESSING, JobType.TIME_START);

                // Move files from Input directory to the Processing directory, creating it first if needed
                FileHandling.MoveDir(InputBufferDir, ProcessingBufferDir);
            }

            // Add entry to status list
            StatusEntry(statusData, job, JobStatus.EXECUTING, JobType.TIME_START);

            // Load and execute command line generator
            CommandLineGenerator cl = new CommandLineGenerator();
            cl.SetExecutableFile(iniData.ModelerRootDir + @"\" + monitorData.Modeler + @"\" + monitorData.Modeler + ".exe");
            cl.SetRepositoryDir(ProcessingBufferDir);
            cl.SetStartPort(monitorData.JobPortNumber);
            cl.SetCpuCores(iniData.CPUCores);
            CommandLineGeneratorThread commandLinethread = new CommandLineGeneratorThread(cl);
            Thread thread = new Thread(new ThreadStart(commandLinethread.ThreadProc));
            thread.Start();

            Console.WriteLine("***** Started Job {0} with Modeler {1} on port {2} with {3} CPU's",
                ProcessingBufferDir, monitorData.Modeler, monitorData.JobPortNumber, iniData.CPUCores);

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
            if (MonitorDirectoryFiles.MonitorDirectory(
                ProcessingBufferDir, NumOfFilesThatNeedToBeGenerated, iniData.MaxTimeLimit, iniData.ScanTime))
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

                    // Move Processing Buffer Files to the Repository directory if passed
                    FileHandling.MoveDir(ProcessingBufferDir, iniData.RepositoryDir + @"\" + monitorData.JobSerialNumber);
                }
                else if (passFail == "Fail")
                {
                    if (!System.IO.Directory.Exists(iniData.ErrorDir + @"\" + monitorData.JobSerialNumber))
                    {
                        System.IO.Directory.CreateDirectory(iniData.ErrorDir + @"\" + monitorData.JobSerialNumber);
                    }

                    // Copy the Transfered files to the Finished directory 
                    for (int i = 0; i < monitorData.NumFilesToTransfer; i++)
                    {
                        FileHandling.CopyFile(iniData.ProcessingDir + @"\" + job + @"\" + monitorData.transferedFileList[i],
                            iniData.ErrorDir + @"\" + monitorData.JobSerialNumber + @"\" + monitorData.transferedFileList[i]);
                    }

                    // Move Processing Buffer Files to the Repository directory if passed
                    FileHandling.MoveDir(ProcessingBufferDir, iniData.RepositoryDir + @"\" + monitorData.JobSerialNumber);
                }

                // Add entry to status list
                StatusEntry(statusData, job, JobStatus.COMPLETE, JobType.TIME_COMPLETE);
            }
        }
    }

    public class StatusRepository : IStatusRepository
    {
        private IniFileData iniFileData = new IniFileData();
        private List<StatusMonitorData> monitorData = new List<StatusMonitorData>();
        private List<StatusData> statusList = new List<StatusData>();
        private StatusData statusData = new StatusData();
        private int GlobalJobIndex = 0;
        private bool RunStop = true;

        public void MonitorDataRepository()
        {
            monitorData = new List<StatusMonitorData>()
            {
                new StatusMonitorData() {
                    Job = "Job Field",
                    JobIndex = GlobalJobIndex,
                    JobSerialNumber = "Job Serial Number Field",
                    TimeStamp = "Time Stamp Field",
                    JobDirectory = "Job Directory Field",
                    XmlFileName = "XML File Name Field",
                    UnitNumber = "Unit Number Field",
                    Modeler = "Modeler Field",
                    JobPortNumber = 3000,
                    NumFilesConsumed = 4,
                    NumFilesProduced = 4,
                    NumFilesToTransfer = 3,
                    transferedFileList = new List<String>()
                }
            };
        }

        public void StatuDataRepository()
        {
            statusList = new List<StatusData>()
            {
                new StatusData() { Job = "Job Field", JobStatus = JobStatus.JOB_STARTED, 
                    TimeReceived = DateTime.Now,  TimeStarted = DateTime.Now,  TimeCompleted = DateTime.Now }
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
            statusList.Add(entry);

            Console.WriteLine("Status: Job {0} Job Status {1} Job Type {2}", job, status, timeSlot.ToString());
        }

        public void ScanForUnfinishedJobs()
        {
            StatusModels.JobXmlData jobXmlData = new StatusModels.JobXmlData();
            DirectoryInfo directory = new DirectoryInfo(iniFileData.ProcessingDir);
            DirectoryInfo[] subdirs = directory.GetDirectories();
            if (subdirs.Length != 0)
            {
                Console.WriteLine("\nFound unfinished jobs...");
                for (int i = 0; i < subdirs.Length; i++)
                {
                    String job = subdirs[i].Name;

                    // Start scan for new directory in the Input Buffer
                    ScanDirectory scanDir = new ScanDirectory(iniFileData.ProcessingDir);
                    jobXmlData = scanDir.GetJobXmlData(iniFileData.ProcessingDir + @"\" + job);

                    // Store data found in Xml file into Monitor Data
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

                    // Increment execution count to track job by this as an index number
                    data.ExecutionCount++;

                    if (data.ExecutionCount <= iniFileData.ExecutionLimit)
                    {
                        // Supply the state information required by the task.
                        JobRunThread jobThread = new JobRunThread(iniFileData.ProcessingDir, iniFileData, data, statusList);

                        // Create a thread to execute the task, and then start the thread.
                        Thread t = new Thread(new ThreadStart(jobThread.ThreadProc));
                        Console.WriteLine("Starting Job " + data.Job);
                        t.Start();
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        Console.WriteLine("Job {0} Index {1} Exceeded Execution Limit of {2}",
                            data.Job, data.ExecutionCount, iniFileData.ExecutionLimit);
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

        public class ProcessThread
        {
            // State information used in the task.
            private IniFileData IniData;
            private List<StatusData> StatusData;

            // The constructor obtains the state information.
            public ProcessThread(IniFileData iniData, List<StatusData> statusData)
            {
                IniData = iniData;
                StatusData = statusData;
            }

            public void ScanForNewJobs()
            {
                StatusModels.JobXmlData jobXmlData = new StatusModels.JobXmlData();
                DirectoryInfo directory = new DirectoryInfo(IniData.InputDir);
                List<String> directoryList = new List<String>();

                Console.WriteLine("\nWaiting for new job(s)...");

                while (true) // Loop all the time
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
                          //data.JobIndex = GlobalJobIndex++;

                            // Display data found
                            Console.WriteLine("");
                            Console.WriteLine("Found new Job         = " + data.Job);
                            Console.WriteLine("New Job Directory     = " + data.JobDirectory);
                            Console.WriteLine("New Serial Number     = " + data.JobSerialNumber);
                            Console.WriteLine("New Time Stamp        = " + data.TimeStamp);
                            Console.WriteLine("New Job Xml File      = " + data.XmlFileName);

                            // Increment execution count to track job by this as an index number
                            data.ExecutionCount++;

                            if (data.ExecutionCount <= IniData.ExecutionLimit)
                            {
                                // Supply the state information required by the task.
                                JobRunThread jobThread = new JobRunThread(IniData.InputDir, IniData, data, StatusData);

                                // Create a thread to execute the task, and then start the thread.
                                Thread t = new Thread(new ThreadStart(jobThread.ThreadProc));
                                Console.WriteLine("Starting Job " + data.Job);
                                t.Start();
                                Thread.Sleep(1000);
                            }
                            else
                            {
                                Console.WriteLine("Job {0} Index {1} Exceeded Execution Limit of {2}",
                                    data.Job, data.ExecutionCount, IniData.ExecutionLimit);
                            }
                        }
                    }

                    // Sleep to allow job to finish before checking for more
                    Thread.Sleep(IniData.ScanTime);
                }
            }
        }

        public StatusModels.IniFileData GetMonitorStatus()
        {
            RunStop = true;

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

            Console.WriteLine("\nConfig.ini data found:");
            Console.WriteLine("Input Dir       = " + iniFileData.InputDir);
            Console.WriteLine("Processing Dir  = " + iniFileData.ProcessingDir);
            Console.WriteLine("Repository Dir  = " + iniFileData.RepositoryDir);
            Console.WriteLine("Finished Dir    = " + iniFileData.FinishedDir);
            Console.WriteLine("Error Dir       = " + iniFileData.ErrorDir);
            Console.WriteLine("Modeler Roo Dir = " + iniFileData.ModelerRootDir);
            Console.WriteLine("CPU Cores       = " + iniFileData.CPUCores);
            Console.WriteLine("Execution Limit = " + iniFileData.ExecutionLimit);
            Console.WriteLine("Start Port      = " + iniFileData.StartPort);
            Console.WriteLine("Log File        = " + iniFileData.LogFile);
            Console.WriteLine("Scan Time       = " + iniFileData.ScanTime);
            Console.WriteLine("Max Time Limit  = " + iniFileData.MaxTimeLimit);

            // Scan for jobs not completed
            ScanForUnfinishedJobs();

            // Start scan for new jobs on it's own thread
            ProcessThread processThread = new ProcessThread(iniFileData, statusList);
            processThread.ScanForNewJobs();

            return iniFileData;
        }

        public void StopMonitor()
        {
            RunStop = false;
        }

        public IEnumerable<StatusData> GetJobStatus()
        {
            return statusList;
        }
    }
}
