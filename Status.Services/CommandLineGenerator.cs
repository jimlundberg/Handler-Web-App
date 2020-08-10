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
        private string Executable = "Executable";
        private string ProcessingDir = "Processing dir";
        private string StartPort = "Start Port";
        private string CpuCores = "Cpu Cores";
        private ILogger<StatusRepository> Logger;

        public CommandLineGenerator() { }
        public string GetCurrentDirector() { return Directory.GetCurrentDirectory(); }
        public void SetExecutableFile(string _Executable) { Executable = _Executable; }
        public void SetRepositoryDir(string _ProcessingDir) { ProcessingDir = "-d " + _ProcessingDir; }
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
            Console.WriteLine("{0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);
            process.Start();

            string outPut = process.StandardOutput.ReadToEnd();
            Console.WriteLine(outPut);

            process.WaitForExit();
            var exitCode = process.ExitCode;
            process.Close();
        }
    }
}
