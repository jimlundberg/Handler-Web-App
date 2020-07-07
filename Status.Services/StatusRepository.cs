using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Xml;
using System.Threading;
using System.Timers;
using StatusModels;

namespace Status.Services
{
    class IniFile
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

    class CommandLineGenerator
    {
        private string cmd;
        private string Executable = "Executable";
        private string ProcessingDir = "Processing dir";
        private string StartPort = "Start Port";
        private string CpuCores = "Cpu Cores";

        public CommandLineGenerator() { cmd = ""; }

        public string GetCurrentDirector() { return Directory.GetCurrentDirectory(); }
        public void SetExecutableDir(string _Executable) { Executable = _Executable; }
        public void SetRepositoryDir(string _ProcessingDir) { ProcessingDir = _ProcessingDir; }
        public void SetStartPort(string _StartPort) { StartPort = "-p " + _StartPort; }
        public void SetCpuCores(string _CpuCores) { CpuCores = "-s " + _CpuCores; }
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

    public class StatusRepository : IStatusRepository
    {
        private List<StatusMonitorData> _monitorList;
        public void MonitorDataRepository()
        {
            _monitorList = new List<StatusMonitorData>()
            {
                new StatusMonitorData() {
                    Job = "Job Field",
                    JobPrefix = "Job Prefix Field",
                    JobDirectory = "Job Directory Field",
                    IniFileName = "Ini File Name Field",
                    XmlFileName = "XML File Name Field",
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
                    NumFilesToTransfer = 0
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
            // Get initial data
            MonitorDataRepository();

            // Read local .ini file data
            StatusMonitorData monitorData = new StatusMonitorData();

            string IniFileName = @"C:\SSMCharacterizationHandler\Application\config.ini";
            var IniParser = new IniFile(IniFileName);
            monitorData.IniFileName = IniFileName;
            monitorData.Modeler = IniParser.Read("Process", "Modeler");
            monitorData.UploadDir = IniParser.Read("Paths", "Upload");
            monitorData.ProcessingDir = IniParser.Read("Paths", "Processing");
            monitorData.RepositoryDir = IniParser.Read("Paths", "Repository");
            monitorData.FinishedDir = IniParser.Read("Paths", "Finished");
            monitorData.ErrorDir = IniParser.Read("Paths", "Error");
            monitorData.ModelerRootDir = IniParser.Read("Paths", "ModelerRoot");
            monitorData.CPUCores = Int32.Parse(IniParser.Read("Process", "CPUCores"));
            monitorData.StartPort = Int32.Parse(IniParser.Read("Process", "StartPort"));
            string timeLimitString = IniParser.Read("Process", "MaxTimeLimit");
            monitorData.MaxTimeLimit = Int32.Parse(timeLimitString.Substring(0, timeLimitString.IndexOf("#")));

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
