using Microsoft.Extensions.Logging;
using StatusModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml;

namespace Status.Services
{
    /// <summary>
    /// Class to run a Job as a thread
    /// </summary>
    public class JobRunThread
    {
        public static IniFileData IniData;
        public static StatusMonitorData MonitorData;
        public static List<StatusData> StatusData;
        public static bool NewJobsFound = false;
        public static string DirectoryName;
        private static readonly Object xmlLock = new Object();
        ILogger<StatusRepository> Logger;

        /// <summary>
        /// Job Run Thread constructor obtains the state information
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public JobRunThread(string directory, bool newJobsFound, IniFileData iniData, JobXmlData xmlData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            IniData = iniData;
            NewJobsFound = newJobsFound;
            StatusData = statusData;
            DirectoryName = directory;
            Logger = logger;

            MonitorData = new StatusMonitorData();
            MonitorData.Job = xmlData.Job;
            MonitorData.JobDirectory = xmlData.JobDirectory;
            MonitorData.JobIndex = StaticData.RunningJobsIndex++;
            MonitorData.JobSerialNumber = xmlData.JobSerialNumber;
            MonitorData.TimeStamp = xmlData.TimeStamp;
            MonitorData.XmlFileName = xmlData.XmlFileName;
        }

        /// <summary>
        /// The thread procedure for running a job
        /// </summary>
        public void ThreadProc()
        {
            Thread thread = new Thread(() => RunJob(DirectoryName, NewJobsFound, IniData, MonitorData, StatusData, Logger));
            thread.Start();
        }

        /// <summary>
        /// Input directory scan complete callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void Input_fileScan_FilesFound(object sender, EventArgs e)
        {
            // Set Flag for ending file scan loop
            StaticData.Log(IniData.ProcessLogFile,
                    String.Format("Input_fileScan_FilesFound Received required number of files for {0}", 
                    e.ToString()));
            StaticData.ExitInputFileScan = false;
        }

        /// <summary>
        /// Processing directory scan complete callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void Processing_fileScan_FilesFound(object sender, EventArgs e)
        {
            // Set Flag for ending file scan loop
            StaticData.Log(IniData.ProcessLogFile, 
                String.Format("Processing_fileScan_FilesFound Received required number of files for {0}",
                e.ToString()));
            StaticData.ExitProcessingFileScan.Add(e.ToString(), false);
        }

        /// <summary>
        /// Status Data Entry
        /// </summary>
        /// <param name="statusList"></param>
        /// <param name="job"></param>
        /// <param name="iniData"></param>
        /// <param name="status"></param>
        /// <param name="timeSlot"></param>
        /// <param name="logFileName"></param>
        /// <param name="logger"></param>
        public static void StatusDataEntry(List<StatusData> statusList, string job, IniFileData iniData,
            JobStatus status, JobType timeSlot, string logFileName, ILogger<StatusRepository> logger)
        {
            StatusEntry statusData = new StatusEntry(statusList, job, status, timeSlot, logFileName, logger);
            statusData.ListStatus(iniData, statusList, job, status, timeSlot);
            statusData.WriteToCsvFile(job, iniData, status, timeSlot, logFileName, logger);
        }

        /// <summary>
        /// Process of running a job 
        /// </summary>
        /// <param name="jobDirectory"></param>
        /// <param name="runningNewJobs"></param>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public static void RunJob(string jobDirectory, bool runningNewJobs, IniFileData iniData, StatusMonitorData monitorData,
            List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            // Add initial entry to status list
            StatusDataEntry(statusData, monitorData.Job, iniData, JobStatus.JOB_STARTED, JobType.TIME_RECEIVED, iniData.StatusLogFile, logger);

            // Set the Start time of the Job
            monitorData.StartTime = DateTime.Now;

            // Wait until Xml file is copied to the directory being scanned
            string job = monitorData.Job;
            string xmlFileName = jobDirectory + @"\" + job + @"\" + monitorData.XmlFileName;
            int NumFilesToTransfer = 0;
            string TopNode;
            XmlDocument XmlDoc;
            lock (xmlLock)
            {
                XmlDoc = new XmlDocument();
                try
                {
                    // Read Job Xml file
                    XmlDoc.Load(xmlFileName);
                }
                catch
                {
                    throw new System.InvalidOperationException("Missing Xml File data");
                }

                // Get the top node of the Xml file
                XmlElement root = XmlDoc.DocumentElement;
                TopNode = root.LocalName;

                // Get nodes for the number of files and names of files to transfer from Job .xml file
                XmlNode UnitNumberdNode = XmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/listitem/value");
                XmlNode ConsumedNode = XmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/FileConfiguration/Consumed");
                XmlNode ProducedNode = XmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/FileConfiguration/Produced");
                XmlNode TransferedNode = XmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/FileConfiguration/Transfered");
                XmlNode ModelerNode = XmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/FileConfiguration/Modeler");

                // Assign port number for this Job
                monitorData.JobPortNumber = iniData.StartPort + monitorData.JobIndex;

                // Get the modeler and number of files to transfer
                monitorData.UnitNumber = UnitNumberdNode.InnerText;
                monitorData.Modeler = ModelerNode.InnerText;
                monitorData.NumFilesConsumed = Convert.ToInt32(ConsumedNode.InnerText);
                monitorData.NumFilesProduced = Convert.ToInt32(ProducedNode.InnerText);
                if (TransferedNode != null)
                {
                    NumFilesToTransfer = Convert.ToInt32(TransferedNode.InnerText);
                }
                monitorData.NumFilesToTransfer = NumFilesToTransfer;
            }

            // Get the modeler and number of files to transfer
            StaticData.Log(iniData.ProcessLogFile, String.Format("Unit Number           = " + monitorData.UnitNumber));
            StaticData.Log(iniData.ProcessLogFile, String.Format("Modeler               = " + monitorData.Modeler));
            StaticData.Log(iniData.ProcessLogFile, String.Format("Num Files Consumed    = " + monitorData.NumFilesConsumed));
            StaticData.Log(iniData.ProcessLogFile, String.Format("Num Files Produced    = " + monitorData.NumFilesProduced));
            StaticData.Log(iniData.ProcessLogFile, String.Format("Num Files To Transfer = " + monitorData.NumFilesToTransfer));
            StaticData.Log(iniData.ProcessLogFile, String.Format("Job Port Number       = " + monitorData.JobPortNumber));

            // Add initial entry to status list
            StatusDataEntry(statusData, job, iniData, JobStatus.MONITORING_INPUT, JobType.TIME_START, iniData.StatusLogFile, logger);

            // Create the Transfered file list from the Xml file entries
            monitorData.TransferedFileList = new List<String>(NumFilesToTransfer);
            List<XmlNode> TransFeredFileXml = new List<XmlNode>();
            monitorData.TransferedFileList = new List<String>();
            for (int i = 1; i < NumFilesToTransfer + 1; i++)
            {
                string transferFileNodeName = ("/" + TopNode + "/FileConfiguration/Transfered" + i.ToString());
                XmlNode TransferedFileXml = XmlDoc.DocumentElement.SelectSingleNode(transferFileNodeName);
                monitorData.TransferedFileList.Add(TransferedFileXml.InnerText);
                StaticData.Log(iniData.ProcessLogFile, String.Format("Transfer File{0}        = {1}", i, TransferedFileXml.InnerText));
            }

            // If the directory is the Input Buffer, move the directory to Processing
            string InputBufferJobDir = monitorData.JobDirectory;
            string ProcessingBufferJobDir = iniData.ProcessingDir + @"\" + job;

            // If this job comes from the Input directory, run the scan and copy
            if (jobDirectory == iniData.InputDir)
            {
                // Monitor the Input directory until it has the total number of consumed files
                if (Directory.Exists(InputBufferJobDir))
                {
                    // Reset the Tcp/Ip Job Complete flag for Input Directory Monitoring
                    // TcpIpScanComplete = false;

                    // Skip if this is a new job found no started
                    if (runningNewJobs == false)
                    {
                        Console.WriteLine("Starting File scan of Input for job {0} at {1:HH:mm:ss.fff}", InputBufferJobDir, DateTime.Now);

                        // Register with the File Watcher class event and start its thread
                        StaticData.ExitInputFileScan = false;
                        InputFileWatcherThread inputFileWatch = new InputFileWatcherThread(InputBufferJobDir,
                            monitorData.NumFilesConsumed, iniData, monitorData, statusData, logger);
                        if (inputFileWatch == null)
                        {
                            logger.LogError("Job Run Thread inputFileWatch failed to instantiate");
                        }
                        inputFileWatch.ProcessCompleted += Input_fileScan_FilesFound;
                        inputFileWatch.ThreadProc();

                        // Wait for Input file scan to complete
                        while (StaticData.ExitInputFileScan == false)
                        {
                            // If the shutdown flag is set, exit method
                            if (StaticData.ShutdownFlag == true)
                            {
                                logger.LogInformation("Shutdown RunJob for Modeler Job {0}", job);
                                return;
                            }
                            Thread.Sleep(250);
                        }

                        Console.WriteLine("Finished scan for Input files of job {0} at {1:HH:mm:ss.fff}", InputBufferJobDir, DateTime.Now);
                    }

                    // Add entry to status list
                    StatusDataEntry(statusData, job, iniData, JobStatus.COPYING_TO_PROCESSING, JobType.TIME_START, iniData.StatusLogFile, logger);

                    // Move files from Input directory to the Processing directory, creating it first if needed
                    FileHandling.CopyFolderContents(InputBufferJobDir, ProcessingBufferJobDir, logger, true, true);
                }
                else
                {
                    logger.LogError("Could not find Input Buffer Directory");
                    throw new System.InvalidOperationException("Could not find Input Buffer Directory");
                }
            }

            // Add entry to status list
            StatusDataEntry(statusData, job, iniData, JobStatus.EXECUTING, JobType.TIME_START, iniData.StatusLogFile, logger);

            // If the shutdown flag is set, exit method
            if (StaticData.ShutdownFlag == true)
            {
                StaticData.Log(iniData.ProcessLogFile, String.Format("Shutdown RunJob before Modeler Job {0}", job));
                return;
            }

            // Load and execute Modeler using command line generator
            CommandLineGenerator cl = new CommandLineGenerator();
            cl.SetExecutableFile(iniData.ModelerRootDir + @"\" + monitorData.Modeler + @"\" + monitorData.Modeler + ".exe");
            cl.SetRepositoryDir(ProcessingBufferJobDir);
            cl.SetStartPort(monitorData.JobPortNumber);
            cl.SetCpuCores(iniData.CPUCores);
            cl.SetLogger(logger);
            CommandLineGeneratorThread commandLinethread = new CommandLineGeneratorThread(cl, logger);
            Thread thread = new Thread(new ThreadStart(commandLinethread.ThreadProc));
            thread.Start();

            StaticData.Log(iniData.ProcessLogFile, 
                String.Format("Starting Job {0} with Modeler {1} on port {2} with {3} CPU's at {4:HH:mm:ss.fff}",
                monitorData.Job, monitorData.Modeler, monitorData.JobPortNumber, iniData.CPUCores, DateTime.Now));

            // If the shutdown flag is set, exit method
            if (StaticData.ShutdownFlag == true)
            {
                logger.LogInformation("Shutdown RunJob for Modeler Job {0}", job);
                return;
            }

            // Add entry to status list
            StatusDataEntry(statusData, job, iniData, JobStatus.MONITORING_PROCESSING, JobType.TIME_START, iniData.StatusLogFile, logger);

            // Set Tcp/Ip Job Complete flag for ProcessingBuffer Directory Monitoring
            //TcpIpScanComplete = false;

            // Monitor for complete set of files in the Processing Buffer
            StaticData.Log(iniData.ProcessLogFile, 
                String.Format("Starting monitoring for Job {0} Processing Buffer output files at {1:HH:mm:ss.fff}", job, DateTime.Now));

            int NumOfFilesThatNeedToBeGenerated = monitorData.NumFilesConsumed + monitorData.NumFilesProduced;

            // Register with the File Watcher class with an event and start its thread
            StaticData.ExitProcessingFileScan[job] = false;
            string processingBufferJobDir = iniData.ProcessingDir + @"\" + MonitorData.Job;
            ProcessingFileWatcherThread ProcessingFileWatch = new ProcessingFileWatcherThread(processingBufferJobDir,
                monitorData.NumFilesConsumed + monitorData.NumFilesProduced, 
                iniData, monitorData, statusData, logger);
            if (ProcessingFileWatch == null)
            {
                logger.LogError("Job Run Thread ProcessingFileWatch failed to instantiate");
            }
            ProcessingFileWatch.ProcessCompleted += Processing_fileScan_FilesFound;
            ProcessingFileWatch.ThreadProc();

            // Wait for job Processing to complete
            do
            {
                Thread.Sleep(250);
            }
            while ((StaticData.ExitProcessingFileScan[job] == false) &&
                   (StaticData.ShutdownFlag == false));

            // Decrement number of jobs executing here
            StaticData.NumberOfJobsExecuting--;

            // Add copy to archieve entry to status list
            StatusDataEntry(statusData, job, iniData, JobStatus.COPYING_TO_ARCHIVE, JobType.TIME_START, iniData.StatusLogFile, logger);

            // Check .Xml output file for pass/fail
            bool XmlFileFound = false;

            // Check for Data.xml in the Processing Directory
            do
            {
                String[] files = Directory.GetFiles(ProcessingBufferJobDir, "Data.xml");
                if (files.Length > 0)
                {
                    xmlFileName = files[0];
                    XmlFileFound = true;
                }

                if (StaticData.ShutdownFlag == true)
                {
                    logger.LogInformation("Shutdown RunJob Scanning xml Job {0}", job);
                    return;
                }

                Thread.Sleep(250);
            }
            while (XmlFileFound == false);

            lock (xmlLock)
            {
                // Read output Xml file data
                XmlDocument XmlOutputDoc = new XmlDocument();
                XmlDoc.Load(xmlFileName);
            }

            // Get the pass or fail data from the OverallResult node
            XmlNode OverallResult = XmlDoc.DocumentElement.SelectSingleNode("/Data/OverallResult/result");
            if (OverallResult != null)
            {
                string passFail = OverallResult.InnerText;
                if (passFail == "Pass")
                {
                    // If the Finished directory does not exist, create it
                    if (!Directory.Exists(iniData.FinishedDir + @"\" + monitorData.JobSerialNumber))
                    {
                        Directory.CreateDirectory(iniData.FinishedDir + @"\" + monitorData.JobSerialNumber);
                    }

                    // Copy the Transfered files to the Finished directory 
                    foreach (var file in monitorData.TransferedFileList)
                    {
                        FileHandling.CopyFile(iniData.ProcessingDir + @"\" + job + @"\" + file,
                            iniData.FinishedDir + @"\" + monitorData.JobSerialNumber + @"\" + file, logger);
                    }

                    // Move Processing Buffer Files to the Repository directory when passed
                    FileHandling.CopyFolderContents(ProcessingBufferJobDir, iniData.RepositoryDir + @"\" + monitorData.Job, logger, true, true);
                }
                else
                {
                    // If the Error directory does not exist, create it
                    if (!Directory.Exists(iniData.ErrorDir + @"\" + monitorData.JobSerialNumber))
                    {
                        Directory.CreateDirectory(iniData.ErrorDir + @"\" + monitorData.JobSerialNumber);
                    }

                    // Copy the Transfered files to the Error directory 
                    foreach (var file in monitorData.TransferedFileList)
                    {
                            FileHandling.CopyFile(iniData.ProcessingDir + @"\" + job + @"\" + file,
                            iniData.ErrorDir + @"\" + monitorData.JobSerialNumber + @"\" + file, logger);
                    }

                    // Move Processing Buffer Files to the Repository directory when failed
                    FileHandling.CopyFolderContents(ProcessingBufferJobDir, iniData.RepositoryDir + @"\" + monitorData.Job, logger, true, true);
                }
            }
            else
            {
                // If the Error directory does not exist, create it
                if (!Directory.Exists(iniData.ErrorDir + @"\" + monitorData.JobSerialNumber))
                {
                    Directory.CreateDirectory(iniData.ErrorDir + @"\" + monitorData.JobSerialNumber);
                }

                // Copy the Transfered files to the Error directory 
                foreach (var file in monitorData.TransferedFileList)
                {
                    FileHandling.CopyFile(iniData.ProcessingDir + @"\" + job + @"\" + file,
                    iniData.ErrorDir + @"\" + monitorData.JobSerialNumber + @"\" + file, logger);
                }

                // Move Processing Buffer Files to the Repository directory when failed
                FileHandling.CopyFolderContents(ProcessingBufferJobDir, iniData.RepositoryDir + @"\" + monitorData.Job, logger, true, true);
            }

            StaticData.Log(iniData.ProcessLogFile, String.Format("Job {0} Complete, decrementing job count to {1} at {2:HH:mm:ss.fff}",
                monitorData.Job, StaticData.NumberOfJobsExecuting, DateTime.Now));

            // Add entry to status list
            StatusDataEntry(statusData, job, iniData, JobStatus.COMPLETE, JobType.TIME_COMPLETE, iniData.StatusLogFile, logger);
        }
    }
}
