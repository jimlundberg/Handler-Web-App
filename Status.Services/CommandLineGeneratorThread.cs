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
        private CommandLineGenerator commandLineGenerator;

        /// <summary>
        /// The constructor obtains the object information 
        /// </summary>
        /// <param name="_commandLineGenerator"></param>
        public CommandLineGeneratorThread(CommandLineGenerator _commandLineGenerator)
        {
            commandLineGenerator = _commandLineGenerator;
        }

        /// <summary>
        /// The thread procedure performs the task using the command line object instance 
        /// </summary>
        public void ThreadProc()
        {
            commandLineGenerator.ExecuteCommand();
        }
    }
}
