using Microsoft.Extensions.Logging;
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
        private readonly string ProcessingDirectory;
        private readonly string ProcessingDirParam;
        private readonly string StartPortParam;
        private readonly string CpuCoresParam;

        /// <summary>
        /// Command Line Generator Constructor 
        /// </summary>
        /// <param name="executable"></param>
        /// <param name="processingDirectory"></param>
        /// <param name="startPort"></param>
        /// <param name="cpuCores"></param>
        public CommandLineGenerator(string executable, string processingDirectory, int startPort, int cpuCores)
        {
            Executable = executable;
            ProcessingDirectory = processingDirectory;
            ProcessingDirParam = "-d " + processingDirectory;
            StartPortParam = "-s " + startPort.ToString();
            CpuCoresParam = "-p " + cpuCores.ToString();
        }

        /// <summary>
        /// Command Line Generator default destructor
        /// </summary>
        ~CommandLineGenerator()
        {
            StaticClass.Logger.LogInformation("CommandLineGenerator default destructor called");
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
                Arguments = string.Format("{0} {1} {2}", ProcessingDirParam, StartPortParam, CpuCoresParam),
                UseShellExecute = true,
                WorkingDirectory = ProcessingDirectory,
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
            Thread.Sleep(StaticClass.DISPLAY_PROCESS_TITLE_WAIT);
            StaticClass.Log(string.Format("{0} {1}", ModelerProcess.MainWindowTitle, ModelerProcess.StartInfo.Arguments));

            return ModelerProcess;
        }
    }
}
