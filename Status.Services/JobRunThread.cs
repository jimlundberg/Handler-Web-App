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
        private IniFileData IniData;
        private StatusMonitorData MonitorData;
        private List<StatusData> StatusData;
        private String DirectoryName;
        private static Object xmlLock = new Object();
        ILogger<StatusRepository> Logger;

        /// <summary>
        /// Job Run Thread constructor obtains the state information
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public JobRunThread(String directory, IniFileData iniData, StatusMonitorData monitorData, List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            IniData = iniData;
            MonitorData = monitorData;
            StatusData = statusData;
            DirectoryName = directory;
            Logger = logger;
        }

        /// <summary>
        /// The thread procedure for running a job
        /// </summary>
        public void ThreadProc()
        {
            Thread thread = new Thread(() => RunJob(DirectoryName, IniData, MonitorData, StatusData, Logger));
            thread.Start();
        }

        /// <summary>
        /// Status Data Entry
        /// </summary>
        /// <param name="statusList"></param>
        /// <param name="job"></param>
        /// <param name="status"></param>
        /// <param name="timeSlot"></param>
        /// <param name="logFileName"></param>
        /// <param name="logger"></param>
        public static void StatusDataEntry(List<StatusData> statusList, String job, 
            JobStatus status, JobType timeSlot, String logFileName, ILogger<StatusRepository> logger)
        {
            StatusEntry statusData = new StatusEntry(statusList, job, status, timeSlot, logFileName, logger);
            statusData.ListStatus(statusList, job, status, timeSlot);
            statusData.WriteToCsvFile(job, status, timeSlot, logFileName, logger);
        }

        /// <summary>
        /// Process of running a job
        /// </summary>
        /// <param name="scanDirectory"></param>
        /// <param name="iniData"></param>
        /// <param name="monitorData"></param>
        /// <param name="statusData"></param>
        /// <param name="logger"></param>
        public static void RunJob(String scanDirectory, IniFileData iniData, StatusMonitorData monitorData, 
            List<StatusData> statusData, ILogger<StatusRepository> logger)
        {
            // Add initial entry to status list
            StatusDataEntry(statusData, monitorData.Job, JobStatus.JOB_STARTED, JobType.TIME_RECEIVED, iniData.LogFile, logger);

            // Set the Start time of the Job
            monitorData.StartTime = DateTime.Now;

            // Wait until Xml file is copied to the directory being scanned
            String job = monitorData.Job;
            String xmlFileName = scanDirectory + @"\" + job + @"\" + monitorData.XmlFileName;
            int NumFilesToTransfer = 0;
            String TopNode;
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
            Console.WriteLine("Unit Number           = " + monitorData.UnitNumber);
            Console.WriteLine("Modeler               = " + monitorData.Modeler);
            Console.WriteLine("Num Files Consumed    = " + monitorData.NumFilesConsumed);
            Console.WriteLine("Num Files Produced    = " + monitorData.NumFilesProduced);
            Console.WriteLine("Num Files To Transfer = " + monitorData.NumFilesToTransfer);
            Console.WriteLine("Num Files To Transfer = " + monitorData.NumFilesToTransfer);
            Console.WriteLine("Job Port Number       = " + monitorData.JobPortNumber);

            // Add initial entry to status list
            StatusDataEntry(statusData, job, JobStatus.MONITORING_INPUT, JobType.TIME_START, iniData.LogFile, logger);

            // Create the Transfered file list from the Xml file entries
            monitorData.transferedFileList = new List<String>(NumFilesToTransfer);
            List<XmlNode> TransFeredFileXml = new List<XmlNode>();
            monitorData.transferedFileList = new List<String>();
            for (int i = 1; i < NumFilesToTransfer + 1; i++)
            {
                String transferFileNodeName = ("/" + TopNode + "/FileConfiguration/Transfered" + i.ToString());
                XmlNode TransferedFileXml = XmlDoc.DocumentElement.SelectSingleNode(transferFileNodeName);
                monitorData.transferedFileList.Add(TransferedFileXml.InnerText);
                Console.WriteLine("Transfer File{0}        = {1}", i, TransferedFileXml.InnerText);
            }

            // If the directory is the Input Buffer, move the directory to Processing
            String InputBufferDir = monitorData.JobDirectory;
            String ProcessingBufferDir = iniData.ProcessingDir + @"\" + job;

            // If this job comes from the Input directory, run the scan and copy
            if (scanDirectory == iniData.InputDir)
            {
                // Monitor the Input directory until it has the total number of consumed files
                if (Directory.Exists(InputBufferDir))
                {
                    // Set Tcp/Ip Job Complete flag for Input Directory Monitoring
                    StaticData.tcpIpScanComplete = true;

                    MonitorDirectoryFiles.MonitorDirectory(StatusModels.DirectoryScanType.INPUT_BUFFER,
                        iniData, monitorData, statusData, InputBufferDir, monitorData.NumFilesConsumed, logger);

                    // Add entry to status list
                    StatusDataEntry(statusData, job, JobStatus.COPYING_TO_PROCESSING, JobType.TIME_START, iniData.LogFile, logger);

                    // Move files from Input directory to the Processing directory, creating it first if needed
                    FileHandling.CopyFolderContents(InputBufferDir, ProcessingBufferDir, logger, true, true);
                }
                else
                {
                    throw new System.InvalidOperationException("Could not find Input Buffer Directory ");
                }
            }

            // Add entry to status list
            StatusDataEntry(statusData, job, JobStatus.EXECUTING, JobType.TIME_START, iniData.LogFile, logger);

            // If the shutdown flag is set, exit method
            if (StaticData.ShutdownFlag == true)
            {
                logger.LogInformation("Shutdown RunJob before Modeler Job {0}", job);
                return;
            }

            // Load and execute command line generator
            CommandLineGenerator cl = new CommandLineGenerator();
            cl.SetExecutableFile(iniData.ModelerRootDir + @"\" + monitorData.Modeler + @"\" + monitorData.Modeler + ".exe");
            cl.SetRepositoryDir(ProcessingBufferDir);
            cl.SetStartPort(monitorData.JobPortNumber);
            cl.SetCpuCores(iniData.CPUCores);
            cl.SetLogger(logger);
            CommandLineGeneratorThread commandLinethread = new CommandLineGeneratorThread(cl, logger);
            Thread modelerThread = new Thread(new ThreadStart(commandLinethread.ThreadProc));
            modelerThread.Start();

            Console.WriteLine("***** Starting Job {0} with Modeler {1} on port {2} with {3} CPU's at {4:HH:mm:ss.fff}",
                monitorData.Job, monitorData.Modeler, monitorData.JobPortNumber, iniData.CPUCores, DateTime.Now);

            // If the shutdown flag is set, exit method
            if (StaticData.ShutdownFlag == true)
            {
                logger.LogInformation("Shutdown RunJob for Modeler Job {0}", job);
                return;
            }

            // Add entry to status list
            StatusDataEntry(statusData, job, JobStatus.MONITORING_PROCESSING, JobType.TIME_START, iniData.LogFile, logger);

            // Set Tcp/Ip Job Complete flag for ProcessingBuffer Directory Monitoring
            StaticData.tcpIpScanComplete = false;

            // Monitor for complete set of files in the Processing Buffer
            Console.WriteLine("Starting monitoring for Job {0} Processing Buffer output files at {1:HH:mm:ss.fff}", job, DateTime.Now);
            int NumOfFilesThatNeedToBeGenerated = monitorData.NumFilesConsumed + monitorData.NumFilesProduced;
            if (MonitorDirectoryFiles.MonitorDirectory(StatusModels.DirectoryScanType.PROCESSING_BUFFER, iniData, monitorData, statusData,
                ProcessingBufferDir, NumOfFilesThatNeedToBeGenerated, logger))
            {
                // If the shutdown flag is set, exit method
                if (StaticData.ShutdownFlag == true)
                {
                    logger.LogInformation("Shutdown RunJob for Modeler Job{0}", job);
                    return;
                }

                // Add copy to archieve entry to status list
                StatusDataEntry(statusData, job, JobStatus.COPYING_TO_ARCHIVE, JobType.TIME_START, iniData.LogFile, logger);

                // Check .Xml output file for pass/fail
                bool XmlFileFound = false;

                // Check for Data.xml in the Processing Directory
                do
                {
                    String[] files = System.IO.Directory.GetFiles(ProcessingBufferDir, "Data.xml");
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

                    Thread.Sleep(500);
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
                    String passFail = OverallResult.InnerText;
                    if (passFail == "Pass")
                    {
                        // If the Finished directory does not exist, create it
                        if (!Directory.Exists(iniData.FinishedDir + @"\" + monitorData.JobSerialNumber))
                        {
                            Directory.CreateDirectory(iniData.FinishedDir + @"\" + monitorData.JobSerialNumber);
                        }

                        // Copy the Transfered files to the Finished directory 
                        for (int i = 0; i < monitorData.NumFilesToTransfer; i++)
                        {
                            FileHandling.CopyFile(iniData.ProcessingDir + @"\" + job + @"\" + monitorData.transferedFileList[i],
                                iniData.FinishedDir + @"\" + monitorData.JobSerialNumber + @"\" + monitorData.transferedFileList[i],
                                logger);
                        }

                        // Move Processing Buffer Files to the Repository directory when passed
                        FileHandling.CopyFolderContents(ProcessingBufferDir, iniData.RepositoryDir + @"\" + monitorData.Job, logger, true, true);
                    }
                    else
                    {
                        // If the Error directory does not exist, create it
                        if (!Directory.Exists(iniData.ErrorDir + @"\" + monitorData.JobSerialNumber))
                        {
                            Directory.CreateDirectory(iniData.ErrorDir + @"\" + monitorData.JobSerialNumber);
                        }

                        // Copy the Transfered files to the Error directory 
                        for (int i = 0; i < monitorData.NumFilesToTransfer; i++)
                        {
                            FileHandling.CopyFile(iniData.ProcessingDir + @"\" + job + @"\" + monitorData.transferedFileList[i],
                                iniData.ErrorDir + @"\" + monitorData.JobSerialNumber + @"\" + monitorData.transferedFileList[i],
                                logger);
                        }

                        // Move Processing Buffer Files to the Repository directory when failed
                        FileHandling.CopyFolderContents(ProcessingBufferDir, iniData.RepositoryDir + @"\" + monitorData.Job, logger, true, true);
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
                    for (int i = 0; i < monitorData.NumFilesToTransfer; i++)
                    {
                        FileHandling.CopyFile(iniData.ProcessingDir + @"\" + job + @"\" + monitorData.transferedFileList[i],
                            iniData.ErrorDir + @"\" + monitorData.JobSerialNumber + @"\" + monitorData.transferedFileList[i],
                            logger);
                    }

                    // Move Processing Buffer Files to the Repository directory when failed
                    FileHandling.CopyFolderContents(ProcessingBufferDir, iniData.RepositoryDir + @"\" + monitorData.Job, logger, true, true);
                }

                StaticData.DecrementNumberOfJobsExecuting();
                Console.WriteLine("-----Job {0} Complete, decrementing job count to {1} at {2:HH:mm:ss.fff}",
                    monitorData.Job, StaticData.NumberOfJobsExecuting, DateTime.Now);

                // Add entry to status list
                StatusDataEntry(statusData, job, JobStatus.COMPLETE, JobType.TIME_COMPLETE, iniData.LogFile, logger);
            }
        }
    }
}
