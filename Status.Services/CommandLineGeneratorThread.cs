using Microsoft.Extensions.Logging;
using Status.Models;
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
        private readonly CommandLineGenerator CommandLineGenerator;
        private readonly IniFileData IniData;
        private readonly StatusMonitorData MonitorData;
        public static ILogger<StatusRepository> Logger;

        /// <summary>
        /// The constructor obtains the object information  
        /// </summary>
        /// <param name="commandLineGenerator"></param>
        /// <param name="monitorData"></param>
        /// <param name="iniData"></param>
        /// <param name="logger"></param>
        public CommandLineGeneratorThread(CommandLineGenerator commandLineGenerator,
            StatusMonitorData monitorData, IniFileData iniData, ILogger<StatusRepository> logger)
        {
            CommandLineGenerator = commandLineGenerator;
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
        /// <param name="monitorData"></param>
        /// <param name="iniData"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public Process ExecuteCommand(StatusMonitorData monitorData, IniFileData iniData, ILogger<StatusRepository> logger)
        {
            string job = monitorData.Job;

            ProcessStartInfo startInfo = new ProcessStartInfo(Executable);
            startInfo.Arguments = String.Format("{0} {1} {2}", ProcessingDir, StartPort, CpuCores);
            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = ProcessingDir;
            startInfo.WindowStyle = ProcessWindowStyle.Minimized;

            Process ModelerProcess = Process.Start(startInfo);
            if (ModelerProcess == null)
            {
                logger.LogError("CommandLineGeneratorThread ModelerProcess failes to instantiate");
            }

            // Set Process Priority to high
            ModelerProcess.PriorityClass = ProcessPriorityClass.Normal;

            // Store process handle to use for stopping
            StaticClass.ProcessHandles[job] = ModelerProcess;

            // Give the Modeler time to start so you can read the main window title
            Thread.Sleep(1000);

            StaticClass.Log(String.Format("{0} {1}", ModelerProcess.MainWindowTitle, ModelerProcess.StartInfo.Arguments));

            // Wait for Modeler to get going before reading it's information
            Thread.Sleep(30000);

            // Display Modeler Executable information
            StaticClass.Log($"\nJob {job} Modeler execution process data:");
            StaticClass.Log($"ProcessName                    : {ModelerProcess.ProcessName}");
            StaticClass.Log($"StartTime                      : {ModelerProcess.StartTime}");
            StaticClass.Log($"MainWindowTitle                : {ModelerProcess.MainWindowTitle}");
            StaticClass.Log($"MainModule                     : {ModelerProcess.MainModule}");
            StaticClass.Log($"StartInfo                      : {ModelerProcess.StartInfo}");
            StaticClass.Log($"GetType                        : {ModelerProcess.GetType()}");
            StaticClass.Log($"MainWindowHandle               : {ModelerProcess.MainWindowHandle}");
            StaticClass.Log($"Handle                         : {ModelerProcess.Handle}");
            StaticClass.Log($"Id                             : {ModelerProcess.Id}");
            StaticClass.Log($"PriorityClass                  : {ModelerProcess.PriorityClass}");
            StaticClass.Log($"Basepriority                   : {ModelerProcess.BasePriority}");
            StaticClass.Log($"PriorityBoostEnabled           : {ModelerProcess.PriorityBoostEnabled}");
            StaticClass.Log($"Responding                     : {ModelerProcess.Responding}");
            StaticClass.Log($"ProcessorAffinity              : {ModelerProcess.ProcessorAffinity}");
            StaticClass.Log($"HandleCount                    : {ModelerProcess.HandleCount}");
            StaticClass.Log($"MaxWorkingSet                  : {ModelerProcess.MaxWorkingSet}");
            StaticClass.Log($"MinWorkingSet                  : {ModelerProcess.MinWorkingSet}");
            StaticClass.Log($"NonpagedSystemMemorySize64     : {ModelerProcess.NonpagedSystemMemorySize64}");
            StaticClass.Log($"PeakVirtualMemorySize64        : {ModelerProcess.PeakVirtualMemorySize64}");
            StaticClass.Log($"PagedSystemMemorySize64        : {ModelerProcess.PagedSystemMemorySize64}");
            StaticClass.Log($"PrivateMemorySize64            : {ModelerProcess.PrivateMemorySize64}");
            StaticClass.Log($"VirtualMemorySize64            : {ModelerProcess.VirtualMemorySize64}");
            StaticClass.Log($"NonpagedSystemMemorySize64     : {ModelerProcess.PagedMemorySize64}");
            StaticClass.Log($"WorkingSet64                   : {ModelerProcess.WorkingSet64}");
            StaticClass.Log($"PeakWorkingSet64               : {ModelerProcess.PeakWorkingSet64}");
            StaticClass.Log($"PrivilegedProcessorTime        : {ModelerProcess.PrivilegedProcessorTime}");
            StaticClass.Log($"TotalProcessorTime             : {ModelerProcess.TotalProcessorTime}");
            StaticClass.Log($"UserProcessorTime              : {ModelerProcess.UserProcessorTime}");

            return ModelerProcess;
        }
    }
}
