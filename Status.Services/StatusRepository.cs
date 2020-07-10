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
                            XmlFileName = files[0];
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
        public static void Copy(string sourceDirectory, string targetDirectory)
        {
            DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
            DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

            CopyAll(diSource, diTarget);
        }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
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
            Console.WriteLine("\n{0} {1}", proc.StartInfo.FileName, proc.StartInfo.Arguments);
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
                    UploadDir = "Upload Directory Field",
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
                    NumFilesToTransfer = 0,
                    Transfered1 = "Transfered1",
                    Transfered2 = "Transfered2",
                    Transfered3 = "Transfered3",
                    Transfered4 = "Transfered4",
                    Transfered5 = "Transfered5"
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

        public IEnumerable<StatusMonitorData> GetMonitorStatus()
        {
            // Get initial data
            MonitorDataRepository();

            // Check that Config.ini file exists
            string IniFileName = @"C:\SSMCharacterizationHandler\Application\Handler\Config.ini";
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
            monitorData.Modeler = IniParser.Read("Process", "Modeler");
            monitorData.CPUCores = Int32.Parse(IniParser.Read("Process", "CPUCores"));
            monitorData.StartPort = Int32.Parse(IniParser.Read("Process", "StartPort"));

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

            // Display 
            Console.WriteLine("");
            Console.WriteLine("Found new Job: " + scanDir.Job);
            Console.WriteLine("Found new JobName: " + scanDir.JobName);
            Console.WriteLine("Found new Timestamp: " + scanDir.TimeStamp);
            Console.WriteLine("Found new Job Xml File: " + scanDir.XmlFileName);

            // Set data found
            monitorData.Job = scanDir.Job;
            monitorData.JobDirectory = scanDir.DirectoryName;
            monitorData.JobName = scanDir.JobName;
            monitorData.TimeStamp = scanDir.TimeStamp;
            monitorData.XmlFileName = scanDir.XmlFileName;

            // Add initial entry to status list
            statusData.Job = monitorData.Job;
            statusData.JobStatus = JobStatus.JOB_STARTED;
            statusData.TimeStarted = DateTime.Now;
            _statusList.Add(statusData);
            Console.WriteLine("status = JOB STARTED");

            // Read Xml file data
            XmlDocument XmlDoc = new XmlDocument();
            XmlDoc.Load(scanDir.XmlFileName);

            // Get the Xml file top node name
            XmlElement root = XmlDoc.DocumentElement;
            String TopNode = root.LocalName;

            // Get number of files and names of files to transfer from Job .xml file
            XmlNode ConsumedNode = XmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/FileConfiguration/Consumed");
            XmlNode ProducedNode = XmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/FileConfiguration/Produced");
            XmlNode TransferedNode = XmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/FileConfiguration/Transfered");
            XmlNode ModelerNode = XmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/FileConfiguration/Modeler");

            monitorData.Modeler = ModelerNode.InnerText;
            monitorData.NumFilesConsumed = Convert.ToInt32(ConsumedNode.InnerText);
            monitorData.NumFilesProduced = Convert.ToInt32(ProducedNode.InnerText);
            int NumFilesToTransfer = Convert.ToInt32(TransferedNode.InnerText);
            monitorData.NumFilesToTransfer = NumFilesToTransfer;

            List<string> TransferedFiles = new List<string>(NumFilesToTransfer);
            List<XmlNode> TransFeredFileXml = new List<XmlNode>();
            for (int i = 1; i < NumFilesToTransfer + 1; i++)
            {
                TransferedFiles.Add("/" + TopNode + "/FileConfiguration/Transfered" + i.ToString());
                XmlNode TransferedFileXml = XmlDoc.DocumentElement.SelectSingleNode(TransferedFiles[i - 1]);

                switch (i)
                {
                    case 1: monitorData.Transfered1 = TransferedFileXml.InnerText; break;
                    case 2: monitorData.Transfered2 = TransferedFileXml.InnerText; break;
                    case 3: monitorData.Transfered3 = TransferedFileXml.InnerText; break;
                    case 4: monitorData.Transfered4 = TransferedFileXml.InnerText; break;
                    case 5: monitorData.Transfered5 = TransferedFileXml.InnerText; break;
                }
            }

            // Add entry to status list
            statusData.JobStatus = JobStatus.MONITORING_INPUT;
            statusData.TimeReceived = DateTime.Now;
            _statusList.Add(statusData);

            // Monitor the Input directory until it has the total number of consumed files
            String InputBufferDir = monitorData.JobDirectory + @"\" + monitorData.Job;
            bool found = File.Exists(InputBufferDir);
            MonitorDirectoryFiles.MonitorDirectory(InputBufferDir, monitorData.NumFilesConsumed, monitorData.MaxTimeLimit);

            // Add entry to status list
            statusData.JobStatus = JobStatus.COPYING_TO_PROCESSING;
            statusData.TimeReceived = DateTime.Now;
            _statusList.Add(statusData);
            Console.WriteLine("status = COPYING TO PROCESSING");

            // Move files from Input directory to the Processing directory, creating it first if needed
            String ProcessingBufferDir = monitorData.ProcessingDir + @"\" + monitorData.Job;
            if (File.Exists(ProcessingBufferDir))
            {
                File.Delete(ProcessingBufferDir);
            }
            Directory.CreateDirectory(ProcessingBufferDir);

            // Move the Job files into the Process Buffer for Modeler usage
            MoveFiles.Copy(InputBufferDir, ProcessingBufferDir);

            // Add entry to status list
            statusData.JobStatus = JobStatus.EXECUTING;
            statusData.TimeReceived = DateTime.Now;
            _statusList.Add(statusData);
            Console.WriteLine("status = JOB EXECUTING");

            // Load and execute command line generator
            CommandLineGenerator cl = new CommandLineGenerator();
            cl.SetExecutableFile(monitorData.ModelerRootDir + @"\" + monitorData.Modeler + @"\" + monitorData.Modeler + ".exe");
            cl.SetRepositoryDir(ProcessingBufferDir);
            cl.SetStartPort(monitorData.StartPort);
            cl.SetCpuCores(monitorData.CPUCores);
            CommandLineGeneratorThread commandLinethread = new CommandLineGeneratorThread(cl);
            Thread thread = new Thread(new ThreadStart(commandLinethread.ThreadProc));
            thread.Start();
            //thread.Join();  // Join makes you wait for thread to complete

            do
            {
                // Timed listen for Modeler TCP/IP response
                TcpIpConnection.SetTimer();

             // Console.WriteLine("Press enter to read Modeler TCP/IP");
                string response = Console.ReadLine();
                Console.WriteLine("Scan TCP/IP at {0:HH:mm:ss.fff}", DateTime.Now);
                Console.WriteLine(response);

                // Not sure what the messages are yet
                if (response == "Complete") break;

                TcpIpConnection.aTimer.Stop();
                TcpIpConnection.aTimer.Dispose();
                Thread.Sleep(30000);
            }
            while (true);

            // Add entry to status list
            statusData.JobStatus = JobStatus.MONITORING_PROCESSING;
            statusData.TimeReceived = DateTime.Now;
            _statusList.Add(statusData);
            Console.WriteLine("status = MONITORING PROCESSING");

            // Monitor for complete set of files in the Processing Buffer
            Console.WriteLine("Monitoring for Processing output files...");
            int NumOfFilesThatNeedToBeGenerated = monitorData.NumFilesConsumed + monitorData.NumFilesProduced;
            if (MonitorDirectoryFiles.MonitorDirectory(ProcessingBufferDir, NumOfFilesThatNeedToBeGenerated, monitorData.MaxTimeLimit))
            {
                // Check .Xml output file for pass/fail
                bool XmlFileFound = false;
                string XmlFileName = "";

                // Check for Data.xml in the Processing Directory
                do
                {
                    string[] files = System.IO.Directory.GetFiles(monitorData.ProcessingDir, "*.xml");
                    if (files.Length > 0)
                    {
                        XmlFileName = files[0];
                        XmlFileFound = true;
                    }

                    Thread.Sleep(500);
                }
                while (XmlFileFound == false);

                // Add entry to status list
                statusData.JobStatus = JobStatus.COPYING_TO_ARCHIVE;
                statusData.TimeReceived = DateTime.Now;
                _statusList.Add(statusData);
                Console.WriteLine("status = COPYING TO ARCHIVE");

                // Read output Xml file data
                XmlDocument XmlOutputDoc = new XmlDocument();
                XmlDoc.Load(XmlFileName);

                // Get the pass or fail data from the OverallResult node
                XmlNode OverallResult = XmlDoc.DocumentElement.SelectSingleNode("/Data/OverallResult/result");
                string passFail = OverallResult.InnerText;
                if (passFail == "Pass")
                {
                    // Move fils to the Archieve directory if passed
                    MoveFiles.Copy(ProcessingBufferDir, monitorData.FinishedDir + @"\" + monitorData.Job);
                }
                else if (passFail == "Fail")
                {
                    // Move fils to the Error directory if failed
                    MoveFiles.Copy(ProcessingBufferDir, monitorData.ErrorDir + @"\" + monitorData.Job);
                }

                // Add entry to status list
                statusData.JobStatus = JobStatus.COMPLETE;
                statusData.TimeCompleted = DateTime.Now;
                _statusList.Add(statusData);
                Console.WriteLine("status = JOB COMPLETE");
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

