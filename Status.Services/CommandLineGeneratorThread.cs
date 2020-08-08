using Microsoft.Extensions.Logging;

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
}
