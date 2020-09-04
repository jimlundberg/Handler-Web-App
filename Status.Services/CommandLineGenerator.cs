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
        private readonly string Executable;
        private readonly string ProcessingDir;
        private readonly string StartPort;
        private readonly string CpuCores;

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
        /// <param name="job"></param>
        /// <returns></returns>
        public Process ExecuteCommand(string job)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(Executable)
            {
                Arguments = String.Format("{0} {1} {2}", ProcessingDir, StartPort, CpuCores),
                UseShellExecute = true,
                WorkingDirectory = ProcessingDir,
                WindowStyle = ProcessWindowStyle.Minimized
            };

            // Check object
            if (startInfo == null)
            {
                StaticClass.Logger.LogError("CommandLineGenerator startInfo failed to instantiate");
            }

            // Start the Modeler process window
            Process ModelerProcess = Process.Start(startInfo);
            if (ModelerProcess == null)
            {
                StaticClass.Logger.LogError("CommandLineGeneratorThread ModelerProcess failes to instantiate");
            }

            // Set Process Priority to high
            ModelerProcess.PriorityClass = ProcessPriorityClass.Normal;

            // Store process handle to use for stopping
            StaticClass.ProcessHandles[job] = ModelerProcess;

            // Give the Modeler time so you can read the Main Window Title parameter for display confirmation
            Thread.Sleep(1000);
            StaticClass.Log(String.Format("{0} {1}", ModelerProcess.MainWindowTitle, ModelerProcess.StartInfo.Arguments));

            return ModelerProcess;
        }
    }
}
