using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;

namespace Status.Services
{
    /// <summary>
    /// Class to run the Command Line Generator
    /// </summary>
    public class CommandLineGenerator
    {
        public readonly string Executable;
        public readonly string ProcessingDir;
        public readonly string StartPort;
        public readonly string CpuCores;

        /// <summary>
        /// Command Line Generator Constructor 
        /// </summary>
        /// <param name="executable"></param>
        /// <param name="processingDir"></param>
        /// <param name="startPort"></param>
        /// <param name="cpuCores"></param>
        public CommandLineGenerator(string executable, string processingDir, int startPort, int cpuCores)
        {
            Executable = executable;
            ProcessingDir = "-d " + processingDir;
            StartPort = "-s " + startPort.ToString();
            CpuCores = "-p " + cpuCores.ToString();
        }

        /// <summary>
        /// Execute the Modeler command line 
        /// </summary>
        /// <param name="monitorData"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public Process ExecuteCommand(string job, ILogger<StatusRepository> logger)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(Executable)
            {
                Arguments = String.Format("{0} {1} {2}", ProcessingDir, StartPort, CpuCores),
                UseShellExecute = true,
                WorkingDirectory = ProcessingDir,
                WindowStyle = ProcessWindowStyle.Minimized
            };

            if (startInfo == null)
            {
                logger.LogError("CommandLineGenerator startInfo failed to instantiate");
            }

            // Start the Modeler process window
            Process ModelerProcess = Process.Start(startInfo);
            if (ModelerProcess == null)
            {
                logger.LogError("CommandLineGeneratorThread ModelerProcess failes to instantiate");
            }

            // Set Process Priority to high
            ModelerProcess.PriorityClass = ProcessPriorityClass.Normal;

            // Store process handle to use for stopping
            StaticClass.ProcessHandles[job] = ModelerProcess;

            // Give the Modeler time so you can read the Main Window Title parameter for display confirmation
            Thread.Sleep(1000);
            StaticClass.Log(String.Format("{0} {1}", ModelerProcess.MainWindowTitle, ModelerProcess.StartInfo.Arguments));

            // Wait 30 seconds for Modeler to get started before reading it's information
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
