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
                    Job = JobDirectory.Remove(0, DirectoryName.Length + 2);
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

    public class MonitorFiles
    {
        private static System.Timers.Timer timer;
        private static int numberOfFiles;
        private static int numberOfFilesFound;
        private static string monitoredDir;
        private static bool filesFound = false;
        private static int timeout;

        public static bool MonitorDirectory(string _monitoredDir, int _numberOfFIles, int _timeout)
        {
            numberOfFiles = _numberOfFIles;
            monitoredDir = _monitoredDir;
            timeout = _timeout;
            filesFound = false;

            timer = new System.Timers.Timer();
            timer.Interval = 5000;
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = true;

            if (filesFound == true)
            {
                Console.WriteLine("File set found... ");
                return true;
            }

            return false;
        }

        private static void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            // Check directory for number of files
            numberOfFilesFound = Directory.GetFiles(monitoredDir, "*", SearchOption.TopDirectoryOnly).Length;
            Console.WriteLine("Directory Checked: {0} number of files {1}", e.SignalTime, numberOfFilesFound);
            if (numberOfFiles == numberOfFilesFound)
            {
                filesFound = true;
            }
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

        public CommandLineGenerator() { cmd = ""; }
        public string GetCurrentDirector() { return Directory.GetCurrentDirectory(); }
        public void SetExecutableDir(string _Executable) { Executable = _Executable; }
        public void SetRepositoryDir(string _ProcessingDir) { ProcessingDir = "-d " + _ProcessingDir; }
        public void SetStartPort(int _StartPort) { StartPort = "-p " + _StartPort.ToString(); }
        public void SetCpuCores(int _CpuCores) { CpuCores = "-s " + _CpuCores.ToString(); }
        public string AddToCommandLine(string addCmd) { return (cmd += addCmd); }

        public void ExecuteCommand()
        {
            var proc = new Process();
            proc.StartInfo.FileName = Executable;
            proc.StartInfo.Arguments = string.Format(@"{0} {1} {2}", ProcessingDir, StartPort, CpuCores);
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.Start();

            string outPut = proc.StandardOutput.ReadToEnd();

            proc.WaitForExit();
            var exitCode = proc.ExitCode;
            proc.Close();
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
        private List<StatusMonitorData> _monitorList;
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
                    ModelersDir = "Modelers Directory Field",
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

        public List<StatusData> _statusList;
        public void StatuDataRepository()
        {
            _statusList = new List<StatusData>()
            {
                new StatusData() { Job = "1278061_202006181549", JobStatus = JobStatus.STARTED, TimeReceived = new DateTime(2020, 6, 18, 8, 15, 0),  TimeStarted = new DateTime(2020, 6, 18, 8, 15, 0),  TimeCompleted = new DateTime(2020, 6, 18, 9, 13, 0) },
                new StatusData() { Job = "1202740_202006171645", JobStatus = JobStatus.RUNNING, TimeReceived = new DateTime(2020, 6, 17, 9, 15, 0),  TimeStarted = new DateTime(2020, 6, 18, 9, 15, 0),  TimeCompleted = new DateTime(2020, 6, 18, 10, 4, 0) },
                new StatusData() { Job = "1278061_202006181549", JobStatus = JobStatus.MONITORING, TimeReceived = new DateTime(2020, 6, 18, 10, 14, 0), TimeStarted = new DateTime(2020, 6, 18, 10, 15, 0), TimeCompleted = new DateTime(2020, 6, 18, 10, 55, 0) },
                new StatusData() { Job = "1278061_202006181549", JobStatus = JobStatus.COMPLETED, TimeReceived = new DateTime(2020, 6, 17, 1, 40, 0),  TimeStarted = new DateTime(2020, 6, 18, 1, 41, 0),  TimeCompleted = new DateTime(2020, 6, 18, 4, 4, 0) },
                new StatusData() { Job = "1278061_202006177423", JobStatus = JobStatus.COPYING, TimeReceived = new DateTime(2020, 6, 16, 1, 40, 0),  TimeStarted = new DateTime(2020, 6, 16, 1, 22, 0),  TimeCompleted = new DateTime(2020, 6, 16, 4, 5, 0) },
                new StatusData() { Job = "1278061_202006181549", JobStatus = JobStatus.STARTED, TimeReceived = new DateTime(2020, 6, 18, 8, 15, 0),  TimeStarted = new DateTime(2020, 6, 18, 8, 15, 0),  TimeCompleted = new DateTime(2020, 6, 18, 9, 13, 0) },
                new StatusData() { Job = "1202740_202006171645", JobStatus = JobStatus.RUNNING, TimeReceived = new DateTime(2020, 6, 17, 9, 15, 0),  TimeStarted = new DateTime(2020, 6, 18, 9, 15, 0),  TimeCompleted = new DateTime(2020, 6, 18, 10, 4, 0) },
                new StatusData() { Job = "1278061_202006181549", JobStatus = JobStatus.MONITORING, TimeReceived = new DateTime(2020, 6, 18, 10, 14, 0), TimeStarted = new DateTime(2020, 6, 18, 10, 15, 0), TimeCompleted = new DateTime(2020, 6, 18, 10, 55, 0) },
                new StatusData() { Job = "1278061_202006181549", JobStatus = JobStatus.COMPLETED, TimeReceived = new DateTime(2020, 6, 17, 1, 40, 0),  TimeStarted = new DateTime(2020, 6, 18, 1, 41, 0),  TimeCompleted = new DateTime(2020, 6, 18, 4, 4, 0) },
                new StatusData() { Job = "1278061_202006177423", JobStatus = JobStatus.COPYING, TimeReceived = new DateTime(2020, 6, 16, 1, 40, 0),  TimeStarted = new DateTime(2020, 6, 16, 1, 22, 0),  TimeCompleted = new DateTime(2020, 6, 16, 4, 5, 0) }
            };
        }

        public IEnumerable<StatusMonitorData> GetMonitorStatus()
        {
            // Create local Monitor Data object to fill in
            StatusMonitorData monitorData = new StatusMonitorData();
            StatusData statusData = new StatusData();

            // Get initial data
            MonitorDataRepository();

            // Check that Config.ini file exists
            string IniFileName = @"C:\SSMCharacterizationHandler\Application\Handler\Config.ini";
            if (File.Exists(IniFileName) == false)
            {
                throw new System.InvalidOperationException("Config.ini file does not exist");
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
            monitorData.ModelersDir = @"C:\SSMCharacterizationHandler\Modelers";
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
            Console.WriteLine("Found new Xml File Name: " + scanDir.XmlFileName);

            // Set data found
            monitorData.Job = scanDir.Job;
            monitorData.JobDirectory = scanDir.DirectoryName;
            monitorData.JobName = scanDir.JobName;
            monitorData.TimeStamp = scanDir.TimeStamp;
            monitorData.XmlFileName = scanDir.XmlFileName;

            // Read Xml File data
            XmlDocument XmlDoc = new XmlDocument();
            XmlDoc.Load(scanDir.XmlFileName);

            XmlNode UnitNumberNode = XmlDoc.DocumentElement.SelectSingleNode("/CONFIG_3155301-029/listitem/value");
            XmlNode ModelerNode = XmlDoc.DocumentElement.SelectSingleNode("/CONFIG_3155301-029/FileConfiguration/Modeler");
            XmlNode ConsumedNode = XmlDoc.DocumentElement.SelectSingleNode("/CONFIG_3155301-029/FileConfiguration/Consumed");
            XmlNode ProducedNode = XmlDoc.DocumentElement.SelectSingleNode("/CONFIG_3155301-029/FileConfiguration/Produced");
            XmlNode TransferedNode = XmlDoc.DocumentElement.SelectSingleNode("/CONFIG_3155301-029/FileConfiguration/Transfered");

            monitorData.UnitNumber = UnitNumberNode.InnerText;
            monitorData.Modeler = ModelerNode.InnerText;
            monitorData.NumFilesConsumed = Convert.ToInt32(ConsumedNode.InnerText);
            monitorData.NumFilesProduced = Convert.ToInt32(ProducedNode.InnerText);
            int NumFilesToTransfer = Convert.ToInt32(TransferedNode.InnerText);
            monitorData.NumFilesToTransfer = NumFilesToTransfer;

            List<string> TransferedFiles = new List<string>(NumFilesToTransfer);
            List<XmlNode> TransFeredFileXml = new List<XmlNode>();
            for (int i = 1; i < NumFilesToTransfer + 1; i++)
            {
                TransferedFiles.Add("/CONFIG_3155301-029/FileConfiguration/Transfered" + i.ToString());
                XmlNode TransferedFileXml = XmlDoc.DocumentElement.SelectSingleNode(TransferedFiles[i - 1]);

                switch (i)
                {
                    case 1:
                        monitorData.Transfered1 = TransferedFileXml.InnerText;
                        break;

                    case 2:
                        monitorData.Transfered2 = TransferedFileXml.InnerText;
                        break;

                    case 3:
                        monitorData.Transfered3 = TransferedFileXml.InnerText;
                        break;

                    case 4:
                        monitorData.Transfered4 = TransferedFileXml.InnerText;
                        break;

                    case 5:
                        monitorData.Transfered5 = TransferedFileXml.InnerText;
                        break;
                }
            }

            // Load and execute command line generator
            //CommandLineGenerator cl = new CommandLineGenerator();
            //cl.SetExecutableDir(monitorData.ModelerRootDir + @"\" + monitorData.Modeler + @"\" + monitorData.Modeler + ".exe");
            //cl.SetRepositoryDir(monitorData.ProcessingDir);
            //cl.SetStartPort(monitorData.StartPort);
            //cl.SetCpuCores(monitorData.CPUCores);
            //cl.ExecuteCommand();

            // Timed listen for Modeler TCP/IP response
            //TcpIpConnection.SetTimer();

            //Console.WriteLine("Press enter to read Modeler TCP/IP");
            //string response = Console.ReadLine();
            //Console.WriteLine("Scan TCP/IP at {0:HH:mm:ss.fff}", DateTime.Now);
            //Console.WriteLine(response);

            //TcpIpConnection.aTimer.Stop();
            //TcpIpConnection.aTimer.Dispose();

            // Monitor for files
            //Console.WriteLine("Monitoring for files...");
            //if (MonitorFiles.MonitorDirectory(monitorData.ProcessingDir, monitorData.NumFilesConsumed, monitorData.MaxTimeLimit))
            //{
            //    // Move file when directory complete
            //    MoveFiles.Copy(monitorData.ProcessingDir, monitorData.FinishedDir);
            //}

            _monitorList.Clear();
            _monitorList.Add(monitorData);
            return _monitorList;
        }

        public IEnumerable<StatusData> GetJobStatus()
        {
            // Get initial data
            StatuDataRepository();
            return _statusList;
        }
    }
}

