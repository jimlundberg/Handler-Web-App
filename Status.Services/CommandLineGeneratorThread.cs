using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;

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
        public static ILogger<StatusRepository> Logger;

        /// <summary>
        /// The constructor obtains the object information 
        /// </summary>
        /// <param name="_commandLineGenerator"></param>
        /// <param name="logger"></param>
        public CommandLineGeneratorThread(CommandLineGenerator _commandLineGenerator, ILogger<StatusRepository> logger)
        {
            CommandLineGenerator = _commandLineGenerator;
            Logger = logger;
        }

        /// <summary>
        /// The thread procedure performs the task using the command line object instance 
        /// </summary>
        public void ThreadProc()
        {
            CommandLineGenerator.ExecuteCommand(Logger);
        }
    }

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
            Debug.WriteLine("{0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);
            process.Start();

            string outPut = process.StandardOutput.ReadToEnd();
            Debug.WriteLine(outPut);

            process.WaitForExit();
            var exitCode = process.ExitCode;
            process.Close();
        }
    }
}
