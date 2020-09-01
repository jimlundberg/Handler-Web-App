using Microsoft.Extensions.Logging;
using Status.Models;
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
        private readonly IniFileData IniData;
        private readonly List<StatusData> StatusDataList;
        private readonly DirectoryScanType DirScanType;
        private readonly JobXmlData JobRunXmlData;
        public static ILogger<StatusRepository> Logger;

        /// <summary>
        /// Job Run Thread constructor obtains the state information  
        /// </summary>
        /// <param name="dirScanType"></param>
        /// <param name="jobXmlData"></param>
        /// <param name="iniData"></param>
        /// <param name="jobXmlData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public JobRunThread(DirectoryScanType dirScanType, JobXmlData jobXmlData, IniFileData iniData,
            List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            DirScanType = dirScanType;
            JobRunXmlData = jobXmlData;
            IniData = iniData;
            StatusDataList = statusData;
            Logger = logger;
        }

        /// <summary>
        /// The thread procedure for running a job
        /// </summary>
        public void ThreadProc()
        {
            StaticClass.JobRunThreadHandle = new Thread(() =>
                RunJob(DirScanType, JobRunXmlData, IniData, StatusDataList, Logger));

            if (StaticClass.JobRunThreadHandle == null)
            {
                Logger.LogError("JobRunThread thread failed to instantiate");
            }
            StaticClass.JobRunThreadHandle.Start();
        }

        /// <summary>
        /// Input directory scan complete callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void Input_fileScan_FilesFound(object sender, EventArgs e)
        {
            string job = e.ToString();

            // Send message after receiving Input Buffer file scan loop complete
            StaticClass.Log(String.Format("Input_fileScan_FilesFound Received required for Job {0} at {1:HH:mm:ss.fff}",
               job, DateTime.Now));

            // Set Flag for ending Input file scan loop
            StaticClass.InputFileScanComplete[job] = true;
        }

        /// <summary>
        /// Processing directory scan complete callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void Processing_fileScan_FilesFound(object sender, EventArgs e)
        {
            string job = e.ToString();

            // Send message after receiving Processing Buffer file scan loop complete
            StaticClass.Log(String.Format("Processing_fileScan_FilesFound Received for Job {0} at {1:HH:mm:ss.fff}",
                job, DateTime.Now));

            // Set Flag for ending Processing file scan loop
            StaticClass.ProcessingFileScanComplete[job] = true;
        }

        /// <summary>
        /// TCP/IP Scan Complete callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void TcpIp_ScanCompleted(object sender, EventArgs e)
        {
            string job = e.ToString();

            StaticClass.Log(String.Format("Processing File Watcher received TCP/IP Scan Completed for job {0} at {1:HH:mm:ss.fff}",
                job, DateTime.Now));

            StaticClass.TcpIpScanComplete[job] = true;
        }

        /// <summary>
        /// Run a job from Input or Processing Buffers
        /// </summary>
        /// <param name="dirScanType"></param>
        /// <param name="jobXmlData"></param>
        /// <param name="iniData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public static void RunJob(DirectoryScanType dirScanType, JobXmlData jobXmlData, IniFileData iniData,
            List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            string job = jobXmlData.Job;
            string xmlJobDirectory = jobXmlData.JobDirectory;
            string processingBufferDirectory = iniData.ProcessingDir;
            string processingBufferJobDir = processingBufferDirectory + @"\" + job;
            string repositoryDirectory = iniData.RepositoryDir;
            string finishedDirectory = iniData.FinishedDir;
            string errorDirectory = iniData.ErrorDir;

            // Create new status monitor data and fill in the job xml data found
            StatusMonitorData monitorData = new StatusMonitorData
            {
                Job = job,
                JobDirectory = xmlJobDirectory,
                StartTime = DateTime.Now,
                JobIndex = StaticClass.RunningJobsIndex++,
                JobSerialNumber = jobXmlData.JobSerialNumber,
                TimeStamp = jobXmlData.TimeStamp,
                XmlFileName = jobXmlData.XmlFileName
            };

            // Increment number of jobs executing at beginning of job and time hack
            StaticClass.NumberOfJobsExecuting++;

            // Add initial entry to status list
            StaticClass.StatusDataEntry(statusData, job, iniData, JobStatus.JOB_STARTED, JobType.TIME_RECEIVED, logger);

            // Wait for the job xml file to be ready
            string jobXmlFileName = xmlJobDirectory + @"\" + jobXmlData.XmlFileName;
            var jobXmltask = StaticClass.IsFileReady(jobXmlFileName, logger);
            jobXmltask.Wait();

            // Read Job xml file and get the top node
            XmlDocument jobXmlDoc = new XmlDocument();
            jobXmlDoc.Load(jobXmlFileName);
            XmlElement root = jobXmlDoc.DocumentElement;
            string TopNode = root.LocalName;

            // Get nodes for the number of files and names of files to transfer from Job .xml file
            XmlNode UnitNumberdNode = jobXmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/listitem/value");
            XmlNode ConsumedNode = jobXmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/FileConfiguration/Consumed");
            XmlNode ProducedNode = jobXmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/FileConfiguration/Produced");
            XmlNode TransferedNode = jobXmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/FileConfiguration/Transfered");
            XmlNode ModelerNode = jobXmlDoc.DocumentElement.SelectSingleNode("/" + TopNode + "/FileConfiguration/Modeler");

            // Assign port number for this Job
            monitorData.JobPortNumber = iniData.StartPort + monitorData.JobIndex;

            // Get the modeler and number of files to transfer
            int NumFilesToTransfer = 0;
            monitorData.UnitNumber = UnitNumberdNode.InnerText;
            monitorData.Modeler = ModelerNode.InnerText;
            monitorData.NumFilesConsumed = Convert.ToInt32(ConsumedNode.InnerText);
            monitorData.NumFilesProduced = Convert.ToInt32(ProducedNode.InnerText);
            if (TransferedNode != null)
            {
                NumFilesToTransfer = Convert.ToInt32(TransferedNode.InnerText);
            }
            monitorData.NumFilesToTransfer = NumFilesToTransfer;

            // Get the modeler and number of files to transfer
            StaticClass.Log(String.Format("Unit Number                    : {0}", monitorData.UnitNumber));
            StaticClass.Log(String.Format("Modeler                        : {0}", monitorData.Modeler));
            StaticClass.Log(String.Format("Num Files Consumed             : {0}", monitorData.NumFilesConsumed));
            StaticClass.Log(String.Format("Num Files Produced             : {0}", monitorData.NumFilesProduced));
            StaticClass.Log(String.Format("Num Files To Transfer          : {0}", monitorData.NumFilesToTransfer));
            StaticClass.Log(String.Format("Job Port Number                : {0}", monitorData.JobPortNumber));

            // Create the Transfered file list from the Xml file entries
            monitorData.TransferedFileList = new List<string>(NumFilesToTransfer);
            List<XmlNode> TransFeredFileXml = new List<XmlNode>();
            monitorData.TransferedFileList = new List<string>();
            for (int i = 1; i < NumFilesToTransfer + 1; i++)
            {
                string transferFileNodeName = ("/" + TopNode + "/FileConfiguration/Transfered" + i.ToString());
                XmlNode TransferedFileXml = jobXmlDoc.DocumentElement.SelectSingleNode(transferFileNodeName);
                monitorData.TransferedFileList.Add(TransferedFileXml.InnerText);
                StaticClass.Log(String.Format("Transfer File{0}                 : {1}", i, TransferedFileXml.InnerText));
            }

            // If this job comes from the Input directory, run the Input job check and start job if found
            if (dirScanType == DirectoryScanType.INPUT_BUFFER)
            {
                // Add initial entry to status list
                StaticClass.StatusDataEntry(statusData, job, iniData, JobStatus.MONITORING_INPUT, JobType.TIME_START, logger);

                // Monitor the Input Buffer job directory until it has the total number of consumed files
                string inputBufferJobDir = iniData.InputDir;
                int numberOfFilesNeeded = monitorData.NumFilesConsumed;
                if (Directory.Exists(inputBufferJobDir))
                {
                    string inputJobFileDir = inputBufferJobDir + @"\" + job;

                    StaticClass.Log(String.Format("Starting File scan of Input for job {0} at {1:HH:mm:ss.fff}",
                        job, DateTime.Now));

                    // Register with the File Watcher class event and start its thread
                    InputFileWatcherThread inputFileWatch = new InputFileWatcherThread(inputJobFileDir, numberOfFilesNeeded,
                        iniData, monitorData, statusData, logger);
                    if (inputFileWatch == null)
                    {
                        logger.LogError("Job Run Thread inputFileWatch failed to instantiate");
                    }
                    inputFileWatch.ProcessCompleted += Input_fileScan_FilesFound;
                    inputFileWatch.ThreadProc();

                    // Wait for Input file scan to complete
                    do
                    {
                        Thread.Yield();

                        if (StaticClass.ShutdownFlag == true)
                        {
                            StaticClass.Log(String.Format("\nShutdown RunJob Input Scan for job {0} at {1:HH:mm:ss.fff}",
                                job, DateTime.Now));
                            return;
                        }

                        // Check if the pause flag is set, then wait for reset
                        if (StaticClass.PauseFlag == true)
                        {
                            do
                            {
                                Thread.Yield();
                            }
                            while (StaticClass.PauseFlag == true);
                        }
                    }
                    while (StaticClass.InputFileScanComplete[job] == false);

                    StaticClass.Log(String.Format("Finished Input Buffer file scan for job {0} at {1:HH:mm:ss.fff}",
                        inputJobFileDir, DateTime.Now));

                    // Add copying entry to status list
                    StaticClass.StatusDataEntry(statusData, job, iniData, JobStatus.COPYING_TO_PROCESSING, JobType.TIME_START, logger);

                    // Move files from Input directory to the Processing directory, creating it first if needed
                    FileHandling.CopyFolderContents(inputJobFileDir, processingBufferJobDir, true, true);
                }
                else
                {
                    logger.LogError("Could not find Input Buffer Directory");
                    throw new InvalidOperationException("Could not find Input Buffer Directory");
                }
            }

            // If the shutdown flag is set, exit method
            if (StaticClass.ShutdownFlag == true)
            {
                StaticClass.Log(String.Format("\nShutdown RunJob pre executinon of Job {0} at {1:HH:mm:ss.fff}",
                    job, DateTime.Now));
                return;
            }

            // Check if the pause flag is set, then wait for reset
            if (StaticClass.PauseFlag == true)
            {
                do
                {
                    Thread.Yield();
                }
                while (StaticClass.PauseFlag == true);
            }

            // Add entry to status list
            StaticClass.StatusDataEntry(statusData, job, iniData, JobStatus.EXECUTING, JobType.TIME_START, logger);

            StaticClass.Log(String.Format("Starting Job {0} with Modeler {1} on port {2} with {3} CPU's at {4:HH:mm:ss.fff}",
                job, monitorData.Modeler, monitorData.JobPortNumber, iniData.CPUCores, DateTime.Now));

            // Execute Modeler using the command line generator
            string executable = iniData.ModelerRootDir + @"\" + monitorData.Modeler + @"\" + monitorData.Modeler + ".exe";
            string processingBuffer = processingBufferJobDir;
            int port = monitorData.JobPortNumber;
            int cpuCores = iniData.CPUCores;
            CommandLineGenerator cmdLineGenerator = new CommandLineGenerator(executable, processingBuffer, port, cpuCores);
            if (cmdLineGenerator == null)
            {
                logger.LogError("JobRunThread cmdLineGenerator failed to instantiate");
            }
            cmdLineGenerator.ExecuteCommand(job, logger);

            // Monitor for complete set of files in the Processing Buffer
            StaticClass.Log(String.Format("Starting monitoring for Job {0} Processing Buffer output files at {1:HH:mm:ss.fff}",
                job, DateTime.Now));

            // Register with the Processing File Watcher class with an event and start its thread
            int numFilesNeeded = monitorData.NumFilesConsumed + monitorData.NumFilesProduced;
            ProcessingFileWatcherThread processingFileWatcher = new ProcessingFileWatcherThread(
                processingBufferJobDir, numFilesNeeded, iniData, monitorData, statusData, logger);
            if (processingFileWatcher == null)
            {
                logger.LogError("JobRunThread ProcessingFileWatch failed to instantiate");
            }
            processingFileWatcher.ProcessCompleted += Processing_fileScan_FilesFound;
            processingFileWatcher.ThreadProc();

            // Start the TCP/IP Communications thread before checking for Processing job files
            TcpIpListenThread tcpIp = new TcpIpListenThread(iniData, monitorData, statusData, Logger);
            if (tcpIp == null)
            {
                Logger.LogError("ProcessingFileWatcherThread tcpIp thread failed to instantiate");
            }
            tcpIp.ProcessCompleted += TcpIp_ScanCompleted;
            tcpIp.StartTcpIpScanProcess(iniData, monitorData, statusData);

            // Add entry to status list
            StaticClass.StatusDataEntry(statusData, job, iniData, JobStatus.MONITORING_PROCESSING, JobType.TIME_START, logger);

            // Wait for the Processing job scan complete which includes TCP/IP
            do
            {
                Thread.Yield();

                if (StaticClass.ShutdownFlag == true)
                {
                    StaticClass.Log(String.Format("\nShutdown RunJob job complete scan for job {0} at {1:HH:mm:ss.fff}",
                        job, DateTime.Now));
                    return;
                }

                // Check if the pause flag is set, then wait for reset
                if (StaticClass.PauseFlag == true)
                {
                    do
                    {
                        Thread.Yield();
                    }
                    while (StaticClass.PauseFlag == true);
                }
            }
            while (StaticClass.ProcessingJobScanComplete[job] == false);

            // Add copy to archieve entry to status list
            StaticClass.StatusDataEntry(statusData, job, iniData, JobStatus.COPYING_TO_ARCHIVE, JobType.TIME_START, logger);

            // Make sure the data.xml file is ready
            string dataXmlFileName = processingBufferDirectory + @"\" + job + @"\" + "data.xml";
            var dataXmltask = StaticClass.IsFileReady(dataXmlFileName, logger);
            dataXmltask.Wait();

            // Data.xml file may not exist for timeout jobs
            if (File.Exists(dataXmlFileName))
            {
                // Get the pass or fail data from the data.xml OverallResult result node
                XmlDocument dataXmlDoc = new XmlDocument();
                dataXmlDoc.Load(dataXmlFileName);
                XmlNode OverallResult = dataXmlDoc.DocumentElement.SelectSingleNode("/Data/OverallResult/result");

                string passFail = "Fail";
                if (OverallResult != null)
                {
                    passFail = OverallResult.InnerText;
                }

                string repositoryJobDirectoryName = repositoryDirectory + @"\" + job;
                if ((OverallResult != null) && (passFail == "Pass"))
                {
                    string finishedJobDirectoryName = finishedDirectory + @"\" + monitorData.JobSerialNumber;

                    // If the Finished directory does not exist, create it
                    if (!Directory.Exists(finishedJobDirectoryName))
                    {
                        Directory.CreateDirectory(finishedJobDirectoryName);
                    }

                    // Copy the Transfered files to the Finished directory 
                    foreach (string file in monitorData.TransferedFileList)
                    {
                        FileHandling.CopyFile(processingBufferJobDir + @"\" + file, finishedJobDirectoryName + @"\" + file);
                    }

                    // Move Processing Buffer Files to the Repository directory when passed
                    FileHandling.CopyFolderContents(processingBufferJobDir, repositoryJobDirectoryName, true, true);
                }
                else // Send files to the Error Buffer and repository
                {
                    string errorJobDirectoryName = errorDirectory + @"\" + monitorData.JobSerialNumber;

                    // If the Error directory does not exist, create it
                    if (!Directory.Exists(errorJobDirectoryName))
                    {
                        Directory.CreateDirectory(errorJobDirectoryName);
                    }

                    // Copy the Transfered files to the Error directory 
                    foreach (string file in monitorData.TransferedFileList)
                    {
                        if (File.Exists(file))
                        {
                            FileHandling.CopyFile(processingBufferJobDir + @"\" + file, errorJobDirectoryName + @"\" + file);
                        }
                    }

                    // Move Processing Buffer Files to the Repository directory when failed
                    FileHandling.CopyFolderContents(processingBufferJobDir, repositoryJobDirectoryName, true, true);
                }
            }

            // Add entry to status list
            StaticClass.StatusDataEntry(statusData, job, iniData, JobStatus.COMPLETE, JobType.TIME_COMPLETE, logger);

            StaticClass.Log(String.Format("Job {0} Complete, decrementing job count to {1} at {2:HH:mm:ss.fff}",
                job, StaticClass.NumberOfJobsExecuting - 1, DateTime.Now));

            StaticClass.NumberOfJobsExecuting--;
        }
    }
}
