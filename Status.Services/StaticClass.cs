using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Status.Services
{
    /// <summary>
    /// Static Data with global access
    /// </summary>
    public static class StaticClass
	{
		public static int NumberOfJobsExecuting = 0;
		public static int RunningJobsIndex = 0;
		public static int logFileSizeLimit = 0;

		public static volatile bool ShutdownFlag = false;
		public static volatile bool CurrentInputJobsScanComplete = false;
		public static volatile bool CurrentProcessingJobScanComplete = false;

		public static List<string> NewInputJobsToRun = new List<String>();
		public static List<string> NewProcessingJobsToRun = new List<String>();

		public static Dictionary<string, bool> InputFileScanComplete = new Dictionary<string, bool>();
		public static Dictionary<string, bool> ProcessingFileScanComplete = new Dictionary<string, bool>();
        public static Dictionary<string, bool> TcpIpScanComplete = new Dictionary<string, bool>();

		public static Dictionary<string, int> NumberOfInputFilesFound = new Dictionary<string, int>();
		public static Dictionary<string, int> NumberOfInputFilesNeeded = new Dictionary<string, int>();
		public static Dictionary<string, int> NumberOfProcessingFilesFound = new Dictionary<string, int>();
		public static Dictionary<string, int> NumberOfProcessingFilesNeeded = new Dictionary<string, int>();

		public static Dictionary<string, Process> ProcessHandles = new Dictionary<string, Process>();

		/// <summary>
		/// Global log to file method
		/// </summary>
		/// <param name="logFile"></param>
		/// <param name="msg"></param>
		public static void Log(string logFile, string msg)
		{
			Console.WriteLine(msg);
			Thread.Sleep(10);
			LoggingToFile log = new LoggingToFile(logFile);
			log.WriteToLogFile(msg);
			Thread.Sleep(10);
		}
	}
}