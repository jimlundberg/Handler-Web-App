using Microsoft.Extensions.Logging;
using StatusModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Status.Services
{
    /// <summary>
    /// Class to run the Command Line Generator
    /// </summary>
    public class CommandLineGeneratorThread
    {
        /// <summary>
        /// Object used in the task
        /// </summary>
        private CommandLineGenerator CommandLineGenerator;
        private static IniFileData IniData;
        private static StatusMonitorData MonitorData;
        public static ILogger<StatusRepository> Logger;

        /// <summary>
        /// The constructor obtains the object information 
        /// </summary>
        /// <param name="_commandLineGenerator"></param>
        /// <param name="logger"></param>
        public CommandLineGeneratorThread(CommandLineGenerator _commandLineGenerator, StatusMonitorData monitorData, IniFileData iniData, ILogger<StatusRepository> logger)
        {
            CommandLineGenerator = _commandLineGenerator;
            MonitorData = monitorData;
            IniData = iniData;
            Logger = logger;
        }

        /// <summary>
        /// The thread procedure performs the task using the command line object instance 
        /// </summary>
        public void ThreadProc()
        {
            CommandLineGenerator.ExecuteCommand(MonitorData, IniData, Logger);
        }
    }

    /// <summary>
    /// Class to generate and execute a command line start of the Modeler executable
    /// </summary>
    public class CommandLineGenerator
    {
        private string Executable;
        private string ProcessingDir;
        private string StartPort;
        private string CpuCores;

        public CommandLineGenerator() { }
        public string GetCurrentDirector() { return Directory.GetCurrentDirectory(); }
        public void SetExecutableFile(string executable) { Executable = executable; }
        public void SetRepositoryDir(string processingDir) { ProcessingDir = "-d " + processingDir; }
        public void SetStartPort(int startPort) { StartPort = "-s " + startPort.ToString(); }
        public void SetCpuCores(int cpuCores) { CpuCores = "-p " + cpuCores.ToString(); }

        /// <summary>
        /// Execute the Modeler command line
        /// </summary>
        /// <param name="logger"></param>
        /// <returns>Process handle</returns>
        public Process ExecuteCommand(StatusMonitorData monitorData, IniFileData iniData, ILogger<StatusRepository> logger)
        {
            string job = monitorData.Job;
            string logFileName = iniData.ProcessLogFile;

            ProcessStartInfo startInfo = new ProcessStartInfo(Executable);
            startInfo.Arguments = String.Format("{0} {1} {2}", ProcessingDir, StartPort, CpuCores);
            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = ProcessingDir;
            startInfo.WindowStyle = ProcessWindowStyle.Minimized;

            Process ModelerProcess = Process.Start(startInfo);
            if (ModelerProcess == null)
            {
                logger.LogError(String.Format("CommandLineGeneratorThread ModelerProcess failes to instantiate"));
            }

            // Set Process Priority to high
            ModelerProcess.PriorityClass = ProcessPriorityClass.High;

            // Store process handle to use for stopping
            StaticClass.ProcessHandles.Add(job, ModelerProcess);

            // Give the Modeler time to start so you can read the main window title
            Thread.Sleep(StaticClass.ThreadWaitTime);

            StaticClass.Log(logFileName, String.Format("{0} {1}", ModelerProcess.MainWindowTitle, ModelerProcess.StartInfo.Arguments));

            // Wait for Modeler to startup before reading data
            Thread.Sleep(StaticClass.ScanWaitTime * 6);

            // Display Modeler Executable information
            StaticClass.Log(logFileName, $"\nJob {monitorData.Job} Modeler execution process data:");
            StaticClass.Log(logFileName, $"ProcessName                 : {ModelerProcess.ProcessName}");
            StaticClass.Log(logFileName, $"MainWindowTitle             : {ModelerProcess.MainWindowTitle}");
            StaticClass.Log(logFileName, $"StartTime                   : {ModelerProcess.StartTime}");
            StaticClass.Log(logFileName, $"MainModule                  : {ModelerProcess.MainModule}");
            StaticClass.Log(logFileName, $"MainWindowHandle            : {ModelerProcess.MainWindowHandle}");
            StaticClass.Log(logFileName, $"StartInfo                   : {ModelerProcess.StartInfo}");
            StaticClass.Log(logFileName, $"Id                          : {ModelerProcess.Id}");
            StaticClass.Log(logFileName, $"Handle                      : {ModelerProcess.Handle}");
            StaticClass.Log(logFileName, $"GetType                     : {ModelerProcess.GetType()}");
            StaticClass.Log(logFileName, $"PriorityClass               : {ModelerProcess.PriorityClass}");
            StaticClass.Log(logFileName, $"Basepriority                : {ModelerProcess.BasePriority}");
            StaticClass.Log(logFileName, $"PriorityBoostEnabled        : {ModelerProcess.PriorityBoostEnabled}");
            StaticClass.Log(logFileName, $"Responding                  : {ModelerProcess.Responding}");
            StaticClass.Log(logFileName, $"ProcessorAffinity           : {ModelerProcess.ProcessorAffinity}");
            StaticClass.Log(logFileName, $"HandleCount                 : {ModelerProcess.HandleCount}");
            StaticClass.Log(logFileName, $"MaxWorkingSet               : {ModelerProcess.MaxWorkingSet}");
            StaticClass.Log(logFileName, $"MinWorkingSet               : {ModelerProcess.MinWorkingSet}");
            StaticClass.Log(logFileName, $"NonpagedSystemMemorySize64  : {ModelerProcess.NonpagedSystemMemorySize64}");
            StaticClass.Log(logFileName, $"PeakVirtualMemorySize64     : {ModelerProcess.PeakVirtualMemorySize64}");
            StaticClass.Log(logFileName, $"PagedSystemMemorySize64     : {ModelerProcess.PagedSystemMemorySize64}");
            StaticClass.Log(logFileName, $"PrivateMemorySize64         : {ModelerProcess.PrivateMemorySize64}");
            StaticClass.Log(logFileName, $"VirtualMemorySize64         : {ModelerProcess.VirtualMemorySize64}");
            StaticClass.Log(logFileName, $"NonpagedSystemMemorySize64  : {ModelerProcess.PagedMemorySize64}");
            StaticClass.Log(logFileName, $"WorkingSet64                : {ModelerProcess.WorkingSet64}");
            StaticClass.Log(logFileName, $"PeakWorkingSet64            : {ModelerProcess.PeakWorkingSet64}");
            StaticClass.Log(logFileName, $"PrivilegedProcessorTime     : {ModelerProcess.PrivilegedProcessorTime}");
            StaticClass.Log(logFileName, $"TotalProcessorTime          : {ModelerProcess.TotalProcessorTime}");
            StaticClass.Log(logFileName, $"UserProcessorTime           : {ModelerProcess.UserProcessorTime}");

            return ModelerProcess;
        }
    }
}
