using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;

namespace Status.Services
{
    /// <summary>
    /// Class to generate and execute a command line start of the Modeler executable
    /// </summary>
    public class CommandLineGenerator
    {
        private String Executable = "Executable";
        private String ProcessingDir = "Processing dir";
        private String StartPort = "Start Port";
        private String CpuCores = "Cpu Cores";
        private ILogger<StatusRepository> Logger;

        public CommandLineGenerator() { }
        public String GetCurrentDirector() { return Directory.GetCurrentDirectory(); }
        public void SetExecutableFile(String _Executable) { Executable = _Executable; }
        public void SetRepositoryDir(String _ProcessingDir) { ProcessingDir = "-d " + _ProcessingDir; }
        public void SetStartPort(int _StartPort) { StartPort = "-s " + _StartPort.ToString(); }
        public void SetCpuCores(int _CpuCores) { CpuCores = "-p " + _CpuCores.ToString(); }
        public void SetLogger(ILogger<StatusRepository> logger) { Logger = logger; }

        /// <summary>
        /// Execute the Modeler command line
        /// </summary>
        /// <param name="logger"></param>
        public void ExecuteCommand(ILogger<StatusRepository> logger)
        {
            var process = new Process();
            process.StartInfo.FileName = Executable;
            process.StartInfo.Arguments = String.Format(@"{0} {1} {2}", ProcessingDir, StartPort, CpuCores);
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            Logger.LogInformation("{0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);
            if (process.Start())
            {
                logger.LogCritical("Modeler Execution Process failed to Start() {0}", Executable);
                return;
            }

            String outPut = process.StandardOutput.ReadToEnd();
            Logger.LogInformation(outPut);

            process.WaitForExit();
            var exitCode = process.ExitCode;
            process.Close();
        }
    }
}
