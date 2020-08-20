using Microsoft.Extensions.Logging;
using StatusModels;
using System;
using System.Collections.Generic;
using System.IO;
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
		public static volatile bool ProcessingFileScanComplete = false;

		public static List<String> NewInputJobsToRun = new List<String>();
		public static List<String> NewProcessingJobsToRun = new List<String>();

		public static Dictionary<string, bool> InputFileScanComplete = new Dictionary<string, bool>();
		public static Dictionary<string, bool> TcpIpScanComplete = new Dictionary<string, bool>();
		public static Dictionary<string, int> NumberOfInputFilesFound = new Dictionary<string, int>();
		public static Dictionary<string, int> NumberOfInputFilesNeeded = new Dictionary<string, int>();
		public static Dictionary<string, int> NumberOfProcessingFilesFound = new Dictionary<string, int>();
		public static Dictionary<string, int> NumberOfProcessingFilesNeeded = new Dictionary<string, int>();

		/// <summary>
		/// Global log to file method
		/// </summary>
		/// <param name="logFile"></param>
		/// <param name="msg"></param>
		public static void Log(string logFile, string msg)
		{
			LoggingToFile log = new LoggingToFile(logFile);
			log.WriteToLogFile(msg);
			Console.WriteLine(msg);
		}

		/// <summary>
		/// Check for if new jobs are waiting in either the Input or Processing dir and run them if they are
		/// </summary>
		/// <param name="scanType"></param>
		/// <param name="iniData"></param>
		/// <param name="statusData"></param>
		/// <param name="logger"></param>
		public static void NewJobsWaitingCheck(DirectoryScanType scanType, IniFileData iniData,
			List<StatusData> statusData, ILogger<StatusRepository> logger)
		{
			// Run new Input jobs if found in the list
			if (StaticClass.NewInputJobsToRun.Count > 0)
			{
				for (int i = 0; i < StaticClass.NewInputJobsToRun.Count; i++)
				{
					if (StaticClass.NumberOfJobsExecuting < iniData.ExecutionLimit)
					{
						if (scanType == DirectoryScanType.INPUT_BUFFER)
                        {
							string directory = iniData.InputDir + @"\" + StaticClass.NewInputJobsToRun[i];
							CurrentInputJobsScanThread currentInputJobsScanThread = new CurrentInputJobsScanThread();
							currentInputJobsScanThread.StartJob(directory, true, iniData, statusData, logger);
						}
						else if (scanType == DirectoryScanType.PROCESSING_BUFFER)
                        {
							string directory = iniData.ProcessingDir + @"\" + StaticClass.NewProcessingJobsToRun[i];
							CurrentProcessingJobsScanThread currentProcessingJobsScanThread = new CurrentProcessingJobsScanThread();
							currentProcessingJobsScanThread.StartJob(iniData, statusData, logger);
						}

						// Remove job from Input List
						if (scanType == DirectoryScanType.INPUT_BUFFER)
						{
							StaticClass.NewInputJobsToRun.RemoveAt(i);
						}
						else if (scanType == DirectoryScanType.PROCESSING_BUFFER)
						{
							StaticClass.NewProcessingJobsToRun.RemoveAt(i);
						}

						Thread.Sleep(iniData.ScanTime);
					}
				}
			}
		}

		/// <summary>
		/// Job timeout handler
		/// </summary>
		/// <param name="job"></param>
		/// <param name="monitorData"></param>
		/// <param name="iniData"></param>
		/// <param name="logger"></param>
		public static void TimeoutHandler(string job, StatusMonitorData monitorData, IniFileData iniData,
			ILogger<StatusRepository> logger)
		{
			Console.WriteLine(String.Format("Timeout Handler for job {0}", job));

			// Get job name from directory name
			string processingBufferDirectory = iniData.ProcessingDir + @"\" + job;
			string repositoryDirectory = iniData.RepositoryDir + @"\" + job;

			// If the repository directory does not exist, create it
			if (!Directory.Exists(repositoryDirectory))
			{
				Directory.CreateDirectory(repositoryDirectory);
			}

			// Move Processing Buffer Files to the Repository directory when failed
			FileHandling.CopyFolderContents(processingBufferDirectory, repositoryDirectory, logger, true, true);

			// If this job is from the new jobs to run list, remove it
			if (StaticClass.NewInputJobsToRun.Contains(job))
			{
				StaticClass.NewInputJobsToRun.Remove(job);
			}

			StaticClass.NumberOfJobsExecuting--;
		}
	}
}