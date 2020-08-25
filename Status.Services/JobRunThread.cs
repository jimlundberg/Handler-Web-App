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
        public static string DirectoryName;
        public static DirectoryScanType DirScanType;
        public static JobXmlData JobRunXmlData;
        private static readonly Object xmlLock = new Object();
        ILogger<StatusRepository> Logger;

        /// <summary>
        /// Job Run Thread constructor obtains the state information  
        /// </summary>
        /// <param name="dirScanType"></param>
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
            StatusData = statusData;
            Logger = logger;
        }

        /// <summary>
        /// The thread procedure for running a job
        /// </summary>
        public void ThreadProc()
        {
            Thread thread = new Thread(() => RunJob(DirScanType, JobRunXmlData, IniData, StatusData, Logger));
            thread.Start();
        }

        /// <summary>
        /// Input directory scan complete callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void Input_fileScan_FilesFound(object sender, EventArgs e)
        {
            string job = e.ToString();

            StaticClass.Log(IniData.ProcessLogFile,
                    String.Format("Input_fileScan_FilesFound Received required number of files for {0} at {1:HH:mm:ss.fff}", 
                    job, DateTime.Now));

            // Set Flag for ending file scan loop
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

            StaticClass.Log(IniData.ProcessLogFile, 
                String.Format("Processing_fileScan_FilesFound Received required number of files for {0} at {1:HH:mm:ss.fff}",
                job, DateTime.Now));

            // Set Flag for ending file scan loop
            StaticClass.ProcessingFileScanComplete[job] = true;
        }

        /// <summary>
        /// Run a job from Input or Processing Buffers
        /// </summary>
        /// <param name="dirScanType"></param>
        /// <param name="jobXmlData"></param>
        /// <param name="iniData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public static void RunJob(DirectoryScanType dirScanType, JobXmlData jobXmlData, IniFileData iniData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            string job = jobXmlData.Job;
            string jobDirectory = jobXmlData.JobDirectory;

            // Create new status monitor data and fill in the job xml data found
            StatusMonitorData monitorData = new StatusMonitorData();
            monitorData.Job = job;
            monitorData.JobDirectory = jobDirectory;
            monitorData.StartTime = DateTime.Now;
            monitorData.JobIndex = StaticClass.RunningJobsIndex++;
            monitorData.JobSerialNumber = jobXmlData.JobSerialNumber;
            monitorData.TimeStamp = jobXmlData.TimeStamp;
            monitorData.XmlFileName = jobXmlData.XmlFileName;

            // Increment number of jobs executing at beginning of job and time hack
            StaticClass.NumberOfJobsExecuting++;

            // Add initial entry to status list
            StaticClass.StatusDataEntry(statusData, job, iniData, JobStatus.JOB_STARTED, JobType.TIME_RECEIVED, logger);

            // Wait for the job xml file to be ready
            string jobXmlFileName = jobDirectory + @"\" + jobXmlData.XmlFileName;
            var jobXmltask = StaticClass.IsFileReady(jobXmlFileName);
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
            string processLogFile = iniData.ProcessLogFile;
            StaticClass.Log(processLogFile, String.Format("Unit Number                 : " + monitorData.UnitNumber));
            StaticClass.Log(processLogFile, String.Format("Modeler                     : " + monitorData.Modeler));
            StaticClass.Log(processLogFile, String.Format("Num Files Consumed          : " + monitorData.NumFilesConsumed));
            StaticClass.Log(processLogFile, String.Format("Num Files Produced          : " + monitorData.NumFilesProduced));
            StaticClass.Log(processLogFile, String.Format("Num Files To Transfer       : " + monitorData.NumFilesToTransfer));
            StaticClass.Log(processLogFile, String.Format("Job Port Number             : " + monitorData.JobPortNumber));

            // Create the Transfered file list from the Xml file entries
            monitorData.TransferedFileList = new List<string>(NumFilesToTransfer);
            List<XmlNode> TransFeredFileXml = new List<XmlNode>();
            monitorData.TransferedFileList = new List<string>();
            for (int i = 1; i < NumFilesToTransfer + 1; i++)
            {
                string transferFileNodeName = ("/" + TopNode + "/FileConfiguration/Transfered" + i.ToString());
                XmlNode TransferedFileXml = jobXmlDoc.DocumentElement.SelectSingleNode(transferFileNodeName);
                monitorData.TransferedFileList.Add(TransferedFileXml.InnerText);
                StaticClass.Log(processLogFile, String.Format("Transfer File{0}              : {1}", i, TransferedFileXml.InnerText));
            }

            // Create the Processing Buffer job directory
            string ProcessingBufferJobDir = iniData.ProcessingDir + @"\" + job;

            // If this job comes from the Input directory, run the Input job check and start job if found
            if (dirScanType == DirectoryScanType.INPUT_BUFFER)
            {
                // Add initial entry to status list
                StaticClass.StatusDataEntry(statusData, job, iniData, JobStatus.MONITORING_INPUT, JobType.TIME_START, logger);

                // Monitor the Input Buffer job directory until it has the total number of consumed files
                string InputBufferJobDir = iniData.InputDir;
                int numberOfFilesNeeded = monitorData.NumFilesConsumed;
                if (Directory.Exists(InputBufferJobDir))
                {
                    string inputJobFileDir = InputBufferJobDir + @"\" + job;

                    StaticClass.Log(processLogFile, String.Format("Starting File scan of Input for job {0} at {1:HH:mm:ss.fff}",
                        job, DateTime.Now));

                    // Register with the File Watcher class event and start its thread
                    InputFileWatcherThread inputFileWatch = new InputFileWatcherThread(inputJobFileDir, numberOfFilesNeeded, iniData,
                        monitorData, statusData, logger);
                    if (inputFileWatch == null)
                    {
                        logger.LogError("Job Run Thread inputFileWatch failed to instantiate");
                    }
                    inputFileWatch.ProcessCompleted += Input_fileScan_FilesFound;
                    inputFileWatch.ThreadProc();

                    // Wait for Input file scan to complete
                    do
                    {
                        Thread.Sleep(StaticClass.ThreadWaitTime);

                        if (StaticClass.ShutdownFlag == true)
                        {
                            StaticClass.Log(processLogFile, String.Format("\nShutdown RunJob Input Scan for job {0} at {1:HH:mm:ss.fff}",
                                job, DateTime.Now));
                            return;
                        }
                    }
                    while (StaticClass.InputFileScanComplete[job] == false);

                    StaticClass.Log(processLogFile,
                        String.Format("Finished Input Buffer file scan for job {0} at {1:HH:mm:ss.fff}",
                        inputJobFileDir, DateTime.Now));

                    // Add copying entry to status list
                    StaticClass.StatusDataEntry(statusData, job, iniData, JobStatus.COPYING_TO_PROCESSING, JobType.TIME_START, logger);

                    // Move files from Input directory to the Processing directory, creating it first if needed
                    FileHandling.CopyFolderContents(inputJobFileDir, ProcessingBufferJobDir, true, true);
                }
                else
                {
                    logger.LogError("Could not find Input Buffer Directory");
                    throw new System.InvalidOperationException("Could not find Input Buffer Directory");
                }
            }

            // If the shutdown flag is set, exit method
            if (StaticClass.ShutdownFlag == true)
            {
                StaticClass.Log(processLogFile, String.Format("\nShutdown RunJob pre executinon of Job {0} at {1:HH:mm:ss.fff}",
                    job, DateTime.Now));
                return;
            }

            // Add entry to status list
            StaticClass.StatusDataEntry(statusData, job, iniData, JobStatus.EXECUTING, JobType.TIME_START, logger);

            StaticClass.Log(processLogFile, String.Format("Starting Job {0} with Modeler {1} on port {2} with {3} CPU's at {4:HH:mm:ss.fff}",
                job, monitorData.Modeler, monitorData.JobPortNumber, iniData.CPUCores, DateTime.Now));

            // Load and execute Modeler using command line generator
            CommandLineGenerator cmdLine = new CommandLineGenerator();
            cmdLine.SetExecutableFile(iniData.ModelerRootDir + @"\" + monitorData.Modeler + @"\" + monitorData.Modeler + ".exe");
            cmdLine.SetRepositoryDir(ProcessingBufferJobDir);
            cmdLine.SetStartPort(monitorData.JobPortNumber);
            cmdLine.SetCpuCores(iniData.CPUCores);
            CommandLineGeneratorThread commandLinethread = new CommandLineGeneratorThread(cmdLine, monitorData, iniData, logger);
            Thread thread = new Thread(new ThreadStart(commandLinethread.ThreadProc));
            thread.Start();

            // Sleep to allow the Modeler to start before starting Process Buffer job file monitoring
            Thread.Sleep(StaticClass.ThreadWaitTime * 2);

            // Register with the File Watcher class with an event and start its thread
            string processingBufferJobDir = iniData.ProcessingDir + @"\" + job;
            int numFilesNeeded = monitorData.NumFilesConsumed + monitorData.NumFilesProduced;
            ProcessingFileWatcherThread ProcessingFileWatch = new ProcessingFileWatcherThread(
                processingBufferJobDir, numFilesNeeded, iniData, monitorData, statusData, logger);
            if (ProcessingFileWatch == null)
            {
                logger.LogError("JobRunThread ProcessingFileWatch failed to instantiate");
            }
            ProcessingFileWatch.ProcessCompleted += Processing_fileScan_FilesFound;
            ProcessingFileWatch.ThreadProc();

            // Add entry to status list
            StaticClass.StatusDataEntry(statusData, job, iniData, JobStatus.MONITORING_PROCESSING, JobType.TIME_START, logger);

            // Monitor for complete set of files in the Processing Buffer
            StaticClass.Log(processLogFile, String.Format("Starting monitoring for Job {0} Processing Buffer output files at {1:HH:mm:ss.fff}",
                job, DateTime.Now));

            // Wait for both job Processing and TCP/IP to complete
            do
            {
                Thread.Sleep(StaticClass.ThreadWaitTime);

                if (StaticClass.ShutdownFlag == true)
                {
                    StaticClass.Log(processLogFile, String.Format("\nShutdown RunJob job complete scan for job {0} at {1:HH:mm:ss.fff}",
                        job, DateTime.Now));
                    return;
                }
            }
            while ((StaticClass.ProcessingFileScanComplete[job] == false) ||
                   (StaticClass.TcpIpScanComplete[job] == false));

            // Add copy to archieve entry to status list
            StaticClass.StatusDataEntry(statusData, job, iniData, JobStatus.COPYING_TO_ARCHIVE, JobType.TIME_START, logger);

            if (StaticClass.ShutdownFlag == true)
            {
                StaticClass.Log(processLogFile, String.Format("\nShutdown JobRunThread RunJob before xml read for job {0} at {1:HH:mm:ss.fff}",
                    job, DateTime.Now));
                return;
            }

            // Wait for the data.xml file to be ready
            string dataXmlFileName = iniData.ProcessingDir + @"\" + job + @"\" + "data.xml";
            var dataXmltask = StaticClass.IsFileReady(dataXmlFileName);
            dataXmltask.Wait();

            // Load the data.xml file
            XmlDocument dataXmlDoc = new XmlDocument();
            dataXmlDoc.Load(dataXmlFileName);

            // Get the pass or fail data from the OverallResult node
            XmlNode OverallResult = jobXmlDoc.DocumentElement.SelectSingleNode("/Data/OverallResult/result");
            string passFail = OverallResult.InnerText;
            if ((OverallResult != null) && (passFail == "Pass"))
            {
                // If the Finished directory does not exist, create it
                if (!Directory.Exists(iniData.FinishedDir + @"\" + monitorData.JobSerialNumber))
                {
                    Directory.CreateDirectory(iniData.FinishedDir + @"\" + monitorData.JobSerialNumber);
                }

                // Copy the Transfered files to the Finished directory 
                foreach (string file in monitorData.TransferedFileList)
                {
                    FileHandling.CopyFile(iniData.ProcessingDir + @"\" + job + @"\" + file,
                        iniData.FinishedDir + @"\" + monitorData.JobSerialNumber + @"\" + file, logger);
                }

                // Move Processing Buffer Files to the Repository directory when passed
                FileHandling.CopyFolderContents(ProcessingBufferJobDir, iniData.RepositoryDir + @"\" + job, true, true);
            }
            else
            {
                // If the Error directory does not exist, create it
                if (!Directory.Exists(iniData.ErrorDir + @"\" + monitorData.JobSerialNumber))
                {
                    Directory.CreateDirectory(iniData.ErrorDir + @"\" + monitorData.JobSerialNumber);
                }

                // Copy the Transfered files to the Error directory 
                foreach (string file in monitorData.TransferedFileList)
                {
                    FileHandling.CopyFile(iniData.ProcessingDir + @"\" + job + @"\" + file,
                    iniData.ErrorDir + @"\" + monitorData.JobSerialNumber + @"\" + file, logger);
                }

                // Move Processing Buffer Files to the Repository directory when failed
                FileHandling.CopyFolderContents(ProcessingBufferJobDir, iniData.RepositoryDir + @"\" + job, true, true);
            }

            // Decrement the number of jobs executing after one completes
            StaticClass.NumberOfJobsExecuting--;

            StaticClass.Log(processLogFile, String.Format("Job {0} Complete, decrementing job count to {1} at {2:HH:mm:ss.fff}",
                job, StaticClass.NumberOfJobsExecuting, DateTime.Now));

            // Add entry to status list
            StaticClass.StatusDataEntry(statusData, job, iniData, JobStatus.COMPLETE, JobType.TIME_COMPLETE, logger);
        }
    }
}
