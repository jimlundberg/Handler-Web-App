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

namespace Status.Services
{
    public class IniFile
    {
        string Path;
        string EXE = Assembly.GetExecutingAssembly().GetName().Name;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern long WritePrivateProfileString(string Key, string Section, string Value, string FilePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string Key, string Section, string Default, StringBuilder RetVal, int Size, string FilePath);

        public IniFile(string IniPath = null)
        {
            Path = new FileInfo(IniPath ?? EXE + ".ini").FullName;
        }

        public string Read(string Section, string Key = null)
        {
            StringBuilder RetVal = new StringBuilder(255);
            int length = GetPrivateProfileString(Section ?? EXE, Key, "", RetVal, 255, Path);
            return RetVal.ToString();
        }

        public void Write(string Key, string Value, string Section = null)
        {
            WritePrivateProfileString(Section ?? EXE, Key, Value, Path);
        }

        public void DeleteKey(string Key, string Section = null)
        {
            Write(Key, null, Section ?? EXE);
        }

        public void DeleteSection(string Section = null)
        {
            Write(null, null, Section ?? EXE);
        }

        public bool KeyExists(string Key, string Section = null)
        {
            return Read(Key, Section).Length > 0;
        }
    }
    public class ScanInputBufferDirectory
    {
        private readonly IEnumerable<String> CurrentDirectoryList;
        public string DirectoryName;
        public string JobDirectory;
        public string JobName;
        public string Job;
        public string TimeStamp;
        public string XmlFileName;

        public ScanInputBufferDirectory(string directoryName)
        {
            // Save directory name for class use
            DirectoryName = directoryName;

            try
            {
                Console.WriteLine("Current Directories:");

                // Get Current baseline directory list
                CurrentDirectoryList = Directory.GetDirectories(directoryName, "*", SearchOption.TopDirectoryOnly);
                foreach (string dir in CurrentDirectoryList)
                {
                    Console.WriteLine(dir);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Get current directory list failed: {0}", e.ToString());
            }
        }

        public bool ScanForNewDirectory()
        {
            try
            {
                // Get new directory list
                string[] newDirectoryList = Directory.GetDirectories(DirectoryName, "*", SearchOption.TopDirectoryOnly);
                IEnumerable<String> differenceQuery = newDirectoryList.Except(CurrentDirectoryList);
                foreach (String dir in differenceQuery)
                {
                    JobDirectory = dir;
                    Job = JobDirectory.Remove(0, DirectoryName.Length + 1);
                    JobName = Job.Substring(0, Job.IndexOf("_"));
                    int start = Job.IndexOf("_") + 1;
                    TimeStamp = Job.Substring(start, Job.Length - start);
                    Console.WriteLine("Found new directory " + JobDirectory);

                    // Wait until the Xml file shows up
                    bool XmlFileFound = false;
                    do
                    {
                        string[] files = System.IO.Directory.GetFiles(dir, "*.xml");
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
                Console.WriteLine("{0} has {1} files at {2} seconds", monitoredDir, numberOfFilesFound, numberOfSeconds);
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

    public class MoveFiles
    {
        public static void CopyDir(string sourceDirectory, string targetDirectory)
        {
            DirectoryInfo Source = new DirectoryInfo(sourceDirectory);
            DirectoryInfo Target = new DirectoryInfo(targetDirectory);
            CopyAllFiles(Source, Target);
        }

        public static void MoveDir(string sourceDirectory, string targetDirectory)
        {
            DirectoryInfo Source = new DirectoryInfo(sourceDirectory);
            DirectoryInfo Target = new DirectoryInfo(targetDirectory);
            if (Directory.Exists(targetDirectory))
            {
                try
                {
                    // Delete all files first
                    string[] files = Directory.GetFiles(targetDirectory);
                    foreach (string file in files)
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

        public static void CopyFile(string sourceFile, string targetFile)
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
        private string cmd;
        private string Executable = "Executable";
        private string ProcessingDir = "Processing dir";
        private string StartPort = "Start Port";
        private string CpuCores = "Cpu Cores";

        public CommandLineGenerator() { }
        public string GetCurrentDirector() { return Directory.GetCurrentDirectory(); }
        public void SetExecutableFile(string _Executable) { Executable = _Executable; }
        public void SetRepositoryDir(string _ProcessingDir) { ProcessingDir = "-d " + _ProcessingDir; }
        public void SetStartPort(int _StartPort) { StartPort = "-s " + _StartPort.ToString(); }
        public void SetCpuCores(int _CpuCores) { CpuCores = "-p " + _CpuCores.ToString(); }
        public string AddToCommandLine(string addCmd) { return (cmd += addCmd); }

        public void ExecuteCommand()
        {
            var proc = new Process();
            proc.StartInfo.FileName = Executable;
            proc.StartInfo.Arguments = string.Format(@"{0} {1} {2}", ProcessingDir, StartPort, CpuCores);
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
          //Console.WriteLine("\n{0} {1}", proc.StartInfo.FileName, proc.StartInfo.Arguments);
            proc.Start();

            string outPut = proc.StandardOutput.ReadToEnd();
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
        static void Connect(String server, Int32 port, String message)
        {
            try
            {
                // Create a TcpClient.
                // Note, for this client to work you need to have a TcpServer
                // connected to the same address as specified by the server, port
                // combination.
                TcpClient client = new TcpClient(server, port);

                // Translate the passed message into ASCII and store it as a Byte array.
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);

                // Get a client stream for reading and writing.
                //  Stream stream = client.GetStream();

                NetworkStream stream = client.GetStream();

                // Send the message to the connected TcpServer.
                stream.Write(data, 0, data.Length);

                Console.WriteLine("Sent: {0}", message);

                // Receive the TcpServer.response.

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

        public static System.Timers.Timer aTimer;

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
            Console.WriteLine("Modeler status requested at {0:HH:mm:ss.fff}",
                              e.SignalTime);

            // Check modeler status
            Connect("127.0.0.1", 3000, "status");
            Console.ReadLine();
        }
    }

    public class StatusRepository : IStatusRepository
    {
        private List<StatusMonitorData> _monitorList = new List<StatusMonitorData>();
        private List<StatusData> _statusList = new List<StatusData>();
        private StatusMonitorData monitorData = new StatusMonitorData();
        private StatusData statusData = new StatusData();

        public void MonitorDataRepository()
        {
            _monitorList = new List<StatusMonitorData>()
            {
                new StatusMonitorData() {
                    Job = "Job Field",
                    JobName = "Job Name Field",
                    TimeStamp = "Time Stamp Field",
                    JobDirectory = "Job Directory Field",
                    IniFileName = ".ini File Name Field",
                    XmlFileName = ".xML File Name Field",
                    ProcessingDir = "Processing Directory Field",
                    RepositoryDir = "Archieve Directory Field",
                    FinishedDir = "Finished Directory Field",
                    ErrorDir = "Error Directory Field",
                    ModelerRootDir = "Modeler Root Directory Field",
                    UnitNumber = "Unit Number Field",
                    CPUCores = 0,
                    Modeler = "Modeler Field",
                    MaxTimeLimit = 0,
                    StartPort = 0,
                    LogFile = "Log File Field",
                    NumFilesConsumed = 0,
                    NumFilesProduced = 0,
                    NumFilesToTransfer = 0
                }
            };
        }

        public void StatuDataRepository()
        {
            _statusList = new List<StatusData>()
            {
                new StatusData() { Job = "1307106_202002181300", JobStatus = JobStatus.JOB_STARTED, TimeReceived = DateTime.Now,  TimeStarted = DateTime.Now,  TimeCompleted = DateTime.Now }
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
        }

        public IEnumerable<StatusMonitorData> GetMonitorStatus()
        {
            // Get initial data
            MonitorDataRepository();

            // Check that Config.ini file exists
            string IniFileName = @"C:\SSMCharacterizationHandler\Handler\Config.ini";
            if (File.Exists(IniFileName) == false)
            {
                throw new System.InvalidOperationException("Config.ini file does not exist in the Handler directory");
            }

            // Get information from the Config.ini file
            var IniParser = new IniFile(IniFileName);
            monitorData.IniFileName = IniFileName;
            monitorData.Modeler = IniParser.Read("Process", "Modeler");
            monitorData.UploadDir = IniParser.Read("Paths", "Upload");
            monitorData.ProcessingDir = IniParser.Read("Paths", "Processing");
            monitorData.RepositoryDir = IniParser.Read("Paths", "Repository");
            monitorData.FinishedDir = IniParser.Read("Paths", "Finished");
            monitorData.ErrorDir = IniParser.Read("Paths", "Error");
            monitorData.ModelerRootDir = IniParser.Read("Paths", "ModelerRootDir");
            monitorData.CPUCores = Int32.Parse(IniParser.Read("Process", "CPUCores"));
            monitorData.StartPort = Int32.Parse(IniParser.Read("Process", "StartPort"));
            monitorData.LogFile = IniParser.Read("Process", "LogFile");

            string timeLimitString = IniParser.Read("Process", "MaxTimeLimit");
            monitorData.MaxTimeLimit = Int32.Parse(timeLimitString.Substring(0, timeLimitString.IndexOf("#")));

            // Start scan for new directory in the Input Buffer
            ScanInputBufferDirectory scanDir = new ScanInputBufferDirectory(@"C:\SSMCharacterizationHandler\Input Buffer");
            bool foundNewDirectory;
            do
            {
                foundNewDirectory = scanDir.ScanForNewDirectory();
                Thread.Sleep(1000);
            }
            while (foundNewDirectory == false);

            // Set data found 
            monitorData.Job = scanDir.Job;
            monitorData.JobDirectory = scanDir.DirectoryName;
            monitorData.JobName = scanDir.JobName;
            monitorData.TimeStamp = scanDir.TimeStamp;
            monitorData.XmlFileName = scanDir.XmlFileName;

            // Display data found
            Console.WriteLine("");
            Console.WriteLine("Found new Job: " + scanDir.Job);
            Console.WriteLine("Found new JobName: " + scanDir.JobName);
            Console.WriteLine("Found new Timestamp: " + scanDir.TimeStamp);
            Console.WriteLine("Found new Job Xml File: " + scanDir.XmlFileName);

            // Add initial entry to status list
            StatusEntry(monitorData.Job, JobStatus.JOB_STARTED, JobType.TIME_RECIEVED);

            // Read Job Xml file and get the top node name
            XmlDocument XmlDoc = new XmlDocument();
            String xmlFileName = monitorData.JobDirectory + @"\" + scanDir.Job + @"\" + scanDir.XmlFileName;
            XmlDoc.Load(xmlFileName);
            XmlElement root = XmlDoc.DocumentElement;
            String TopNode = root.LocalName;

            // Get nodes for the number of files and names of files to transfer from Job .xml file
            XmlNode UnitNumberdNode = XmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/listitem/value");
            XmlNode ConsumedNode = XmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/FileConfiguration/Consumed");
            XmlNode ProducedNode = XmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/FileConfiguration/Produced");
            XmlNode TransferedNode = XmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/FileConfiguration/Transfered");
            XmlNode ModelerNode = XmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/FileConfiguration/Modeler");

            // Get the modeler and number of files to transfer
            monitorData.UnitNumber = UnitNumberdNode.InnerText;
            monitorData.Modeler = ModelerNode.InnerText;
            monitorData.NumFilesConsumed = Convert.ToInt32(ConsumedNode.InnerText);
            monitorData.NumFilesProduced = Convert.ToInt32(ProducedNode.InnerText);
            int NumFilesToTransfer = Convert.ToInt32(TransferedNode.InnerText);
            monitorData.NumFilesToTransfer = NumFilesToTransfer;
            monitorData.Modeler = ModelerNode.InnerText;

            // Add initial entry to status list
            StatusEntry(monitorData.Job, JobStatus.MONITORING_INPUT, JobType.TIME_START);

            // Create the Transfered file list from the Xml file entries
            monitorData.transferedFileList = new List<string>(NumFilesToTransfer);
            List<XmlNode> TransFeredFileXml = new List<XmlNode>();
            monitorData.transferedFileList = new List<String>();
            for (int i = 1; i < NumFilesToTransfer + 1; i++)
            {
                String transferFileNodeName = ("/" + TopNode + "/FileConfiguration/Transfered" + i.ToString());
                XmlNode TransferedFileXml = XmlDoc.DocumentElement.SelectSingleNode(transferFileNodeName);
                monitorData.transferedFileList.Add(TransferedFileXml.InnerText);
            }

            // Add entry of received job to status list
            StatusEntry(monitorData.Job, JobStatus.MONITORING_INPUT, JobType.TIME_START);

            // Monitor the Input directory until it has the total number of consumed files
            String InputBufferDir = monitorData.JobDirectory + @"\" + monitorData.Job;
            bool found = File.Exists(InputBufferDir);
            MonitorDirectoryFiles.MonitorDirectory(InputBufferDir, monitorData.NumFilesConsumed, monitorData.MaxTimeLimit);

            // Add entry to status list
            StatusEntry(monitorData.Job, JobStatus.COPYING_TO_PROCESSING, JobType.TIME_START);

            // Move files from Input directory to the Processing directory, creating it first if needed
            String ProcessingBufferDir = monitorData.ProcessingDir + @"\" + monitorData.Job;
            MoveFiles.MoveDir(InputBufferDir, ProcessingBufferDir);

            // Add entry to status list
            StatusEntry(monitorData.Job, JobStatus.EXECUTING, JobType.TIME_START);

            // Load and execute command line generator
            //CommandLineGenerator cl = new CommandLineGenerator();
            //cl.SetExecutableFile(monitorData.ModelerRootDir + @"\" + monitorData.Modeler + @"\" + monitorData.Modeler + ".exe");
            //cl.SetRepositoryDir(ProcessingBufferDir);
            //cl.SetStartPort(monitorData.StartPort);
            //cl.SetCpuCores(monitorData.CPUCores);
            //CommandLineGeneratorThread commandLinethread = new CommandLineGeneratorThread(cl);
            //Thread thread = new Thread(new ThreadStart(commandLinethread.ThreadProc));
            //thread.Start();

            //do
            //{
            //    // Timed listen for Modeler TCP/IP response
            //    TcpIpConnection.SetTimer();
            //    string response;
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
            StatusEntry(monitorData.Job, JobStatus.MONITORING_PROCESSING, JobType.TIME_START);

            // Monitor for complete set of files in the Processing Buffer
            Console.WriteLine("Monitoring for Processing output files...");
            int NumOfFilesThatNeedToBeGenerated = monitorData.NumFilesConsumed + monitorData.NumFilesProduced;
            if (MonitorDirectoryFiles.MonitorDirectory(ProcessingBufferDir, NumOfFilesThatNeedToBeGenerated, monitorData.MaxTimeLimit))
            {
                // Add copy entry to status list
                StatusEntry(monitorData.Job, JobStatus.COPYING_TO_ARCHIVE, JobType.TIME_START);

                // Check .Xml output file for pass/fail
                bool XmlFileFound = false;

                // Check for Data.xml in the Processing Directory
                do
                {
                    string[] files = System.IO.Directory.GetFiles(ProcessingBufferDir, "Data.xml");
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
                string passFail = OverallResult.InnerText;
                if (passFail == "Pass")
                {
                    // Move Processing Buffer Files to the Finished directory if passed
                    MoveFiles.CopyDir(ProcessingBufferDir, monitorData.FinishedDir + @"\" + monitorData.Job);

                    // If the Repository directory does not exist, create it
                    bool exists = System.IO.Directory.Exists(monitorData.RepositoryDir + @"\" + monitorData.JobName);
                    if (!exists)
                    {
                        System.IO.Directory.CreateDirectory(monitorData.RepositoryDir + @"\" + monitorData.JobName);
                    }

                    // Copy the Transfered files to the repository directory 
                    for (int i = 0; i < monitorData.NumFilesToTransfer; i++)
                    {
                        MoveFiles.CopyFile(monitorData.ProcessingDir + @"\" + monitorData.Job + @"\" + monitorData.transferedFileList[i],
                                           monitorData.RepositoryDir + @"\" + monitorData.JobName + @"\" + monitorData.transferedFileList[i]);
                    }
                }
                else if (passFail == "Fail")
                {
                    // Move fils to the Error directory if failed
                    MoveFiles.CopyDir(ProcessingBufferDir, monitorData.ErrorDir + @"\" + monitorData.Job);
                }

                // Add entry to status list
                StatusEntry(monitorData.Job, JobStatus.COMPLETE, JobType.TIME_COMPLETE);
            }

            _monitorList.Clear();
            _monitorList.Add(monitorData);
            return _monitorList;
        }

        public IEnumerable<StatusData> GetJobStatus()
        {
            return _statusList;
        }
    }
}
