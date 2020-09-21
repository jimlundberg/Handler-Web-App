using Microsoft.Extensions.Logging;
using Status.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly DirectoryScanType DirScanType;
        private readonly JobXmlData JobRunXmlData;
        public event EventHandler ProcessCompleted;

        /// <summary>
        /// Job Run Thread constructor obtains the state information  
        /// </summary>
        /// <param name="dirScanType"></param>
        /// <param name="jobXmlData"></param>
        public JobRunThread(JobXmlData jobXmlData, DirectoryScanType dirScanType)
        {
            DirScanType = dirScanType;
            JobRunXmlData = jobXmlData;
        }

        /// <summary>
        /// The thread procedure for running a job
        /// </summary>
        public void ThreadProc()
        {
            StaticClass.JobRunThreadHandle = new Thread(() => RunJob(JobRunXmlData, DirScanType));

            if (StaticClass.JobRunThreadHandle == null)
            {
                StaticClass.Logger.LogError("JobRunThread JobRunThreadHandle thread failed to instantiate");
            }
            StaticClass.JobRunThreadHandle.Start();
        }

        /// <summary>
        /// Process complete callback
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnProcessCompleted(EventArgs e)
        {
            ProcessCompleted?.Invoke(this, e);
        }

        /// <summary>
        /// Get the data from the Job xml file
        /// </summary>
        /// <param name="jobXmlData"></param>
        /// <param name="monitorData"></param>
        /// <returns></returns>
        public StatusMonitorData GetJobXmlData(JobXmlData jobXmlData, StatusMonitorData monitorData)
        {
            // Wait for Job xml file to be ready
            string jobXmlFileName = jobXmlData.JobDirectory + @"\" + jobXmlData.XmlFileName;
            if (StaticClass.CheckFileReady(jobXmlFileName))
            {
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

                // Assign then increment port number for this Job
                monitorData.JobPortNumber = StaticClass.IniData.StartPort + StaticClass.JobPortIndex++;

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
                StaticClass.Log($"Unit Number                    : {monitorData.UnitNumber}");
                StaticClass.Log($"Modeler                        : {monitorData.Modeler}");
                StaticClass.Log($"Num Files Consumed             : {monitorData.NumFilesConsumed}");
                StaticClass.Log($"Num Files Produced             : {monitorData.NumFilesProduced}");
                StaticClass.Log($"Num Files To Transfer          : {monitorData.NumFilesToTransfer}");
                StaticClass.Log($"Job Port Number                : {monitorData.JobPortNumber}");

                // Create the Transfered file list from the Xml file entries
                monitorData.TransferedFileList = new List<string>(NumFilesToTransfer);
                for (int i = 1; i < NumFilesToTransfer + 1; i++)
                {
                    string transferFileNodeName = ("/" + TopNode + "/FileConfiguration/Transfered" + i.ToString());
                    XmlNode TransferedFileXml = jobXmlDoc.DocumentElement.SelectSingleNode(transferFileNodeName);
                    monitorData.TransferedFileList.Add(TransferedFileXml.InnerText);
                    StaticClass.Log(string.Format("Transfer File{0}                 : {1}", i, TransferedFileXml.InnerText));
                }
            }
            else
            {
                StaticClass.Logger.LogError("File {0} is not available at {1:HH:mm:ss.fff}\n", jobXmlFileName, DateTime.Now);
            }

            return monitorData;
        }

        /// <summary>
        /// Run Input Buffer Scan
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="job"></param>
        /// <param name="monitorData"></param>
        public void RunInputBufferScan(string directory, string job, StatusMonitorData monitorData)
        {
            // Add initial entry to status list
            StaticClass.StatusDataEntry(job, JobStatus.MONITORING_INPUT, JobType.TIME_START);

            // Monitor the Input Buffer job directory until it has the total number of consumed files
            string inputBufferJobDir = StaticClass.IniData.InputDir;
            int numberOfFilesNeeded = monitorData.NumFilesConsumed;
            if (Directory.Exists(inputBufferJobDir))
            {
                string inputJobFileDir = inputBufferJobDir + @"\" + job;

                // Register with the File Watcher class event and start its thread
                InputFileWatcherThread inputFileWatch = new InputFileWatcherThread(inputJobFileDir, numberOfFilesNeeded);
                if (inputFileWatch == null)
                {
                    StaticClass.Logger.LogError("Job Run Thread inputFileWatch failed to instantiate");
                }
                inputFileWatch.ThreadProc();

                // Wait for Input file scan to complete
                do
                {
                    if (StaticClass.ShutDownPauseCheck("Run Job") == true)
                    {
                        return;
                    }

                    Thread.Yield();
                }
                while (StaticClass.InputFileScanComplete[job] == false);

                StaticClass.Log(string.Format("Finished Input file scan for Job {0} at {1:HH:mm:ss.fff}",
                    inputJobFileDir, DateTime.Now));

                // Add copying entry to status list
                StaticClass.StatusDataEntry(job, JobStatus.COPYING_TO_PROCESSING, JobType.TIME_START);

                // Move files from Input directory to the Processing directory, creating it first if needed
                FileHandling.CopyFolderContents(inputJobFileDir, directory, true, true);
            }
            else
            {
                StaticClass.Logger.LogError("Could not find Input Buffer Directory");
                throw new InvalidOperationException("Could not find Input Buffer Directory");
            }
        }

        /// <summary>
        /// Display the Modeler Process information
        /// </summary>
        /// <param name=job"></param>
        /// <param name="modelerProcess"></param>
        public void DisplayProcessInfo(string job, Process modelerProcess)
        {
            // Display Modeler Executable information
            StaticClass.Log($"\nJob {job} Modeler execution process data:");
            StaticClass.Log($"ProcessName                    : {modelerProcess.ProcessName}");
            StaticClass.Log($"StartTime                      : {modelerProcess.StartTime}");
            StaticClass.Log($"MainWindowTitle                : {modelerProcess.MainWindowTitle}");
            StaticClass.Log($"MainModule                     : {modelerProcess.MainModule}");
            StaticClass.Log($"StartInfo                      : {modelerProcess.StartInfo}");
            StaticClass.Log($"GetType                        : {modelerProcess.GetType()}");
            StaticClass.Log($"MainWindowHandle               : {modelerProcess.MainWindowHandle}");
            StaticClass.Log($"Handle                         : {modelerProcess.Handle}");
            StaticClass.Log($"Id                             : {modelerProcess.Id}");
            StaticClass.Log($"PriorityClass                  : {modelerProcess.PriorityClass}");
            StaticClass.Log($"Basepriority                   : {modelerProcess.BasePriority}");
            StaticClass.Log($"PriorityBoostEnabled           : {modelerProcess.PriorityBoostEnabled}");
            StaticClass.Log($"Responding                     : {modelerProcess.Responding}");
            StaticClass.Log($"ProcessorAffinity              : {modelerProcess.ProcessorAffinity}");
            StaticClass.Log($"HandleCount                    : {modelerProcess.HandleCount}");
            StaticClass.Log($"MaxWorkingSet                  : {modelerProcess.MaxWorkingSet}");
            StaticClass.Log($"MinWorkingSet                  : {modelerProcess.MinWorkingSet}");
            StaticClass.Log($"NonpagedSystemMemorySize64     : {modelerProcess.NonpagedSystemMemorySize64}");
            StaticClass.Log($"PeakVirtualMemorySize64        : {modelerProcess.PeakVirtualMemorySize64}");
            StaticClass.Log($"PagedSystemMemorySize64        : {modelerProcess.PagedSystemMemorySize64}");
            StaticClass.Log($"PrivateMemorySize64            : {modelerProcess.PrivateMemorySize64}");
            StaticClass.Log($"VirtualMemorySize64            : {modelerProcess.VirtualMemorySize64}");
            StaticClass.Log($"NonpagedSystemMemorySize64     : {modelerProcess.PagedMemorySize64}");
            StaticClass.Log($"WorkingSet64                   : {modelerProcess.WorkingSet64}");
            StaticClass.Log($"PeakWorkingSet64               : {modelerProcess.PeakWorkingSet64}");
            StaticClass.Log($"PrivilegedProcessorTime        : {modelerProcess.PrivilegedProcessorTime}");
            StaticClass.Log($"TotalProcessorTime             : {modelerProcess.TotalProcessorTime}");
            StaticClass.Log($"UserProcessorTime              : {modelerProcess.UserProcessorTime}");
        }

        /// <summary>
        /// Check if the Modeler has deposited the OverallResult entry in the job data.xml file
        /// </summary>
        /// <param name="dataXmlFileName"></param>
        /// <returns></returns>
        public bool OverallResultEntryCheck(string dataXmlFileName)
        {
            int numOfRetries = 0;
            do
            {
                // Check for data.xml file to be ready
                if (StaticClass.CheckFileReady(dataXmlFileName))
                {
                    // Check if the OverallResult node exists
                    XmlDocument dataXmlDoc = new XmlDocument();
                    dataXmlDoc.Load(dataXmlFileName);
                    XmlNode OverallResult = dataXmlDoc.DocumentElement.SelectSingleNode("/Data/OverallResult/result");
                    if (OverallResult != null)
                    {
                        return true;
                    }

                    // Check for shutdown or pause
                    if (StaticClass.ShutDownPauseCheck("Overall Result Entry Check") == true)
                    {
                        return false;
                    }

                    Thread.Sleep(StaticClass.FILE_WAIT_DELAY);
                }
            }
            while (numOfRetries++ < StaticClass.NUM_RESULTS_ENTRY_RETRIES);

            StaticClass.Log(string.Format("File {0} did not have Overall Result entry even after {1} retries at {2:HH:mm:ss.fff}",
                dataXmlFileName, StaticClass.NUM_RESULTS_ENTRY_RETRIES, DateTime.Now));
            return false;
        }

        /// <summary>
        /// Run the Job Completion file and directory handling
        /// </summary>
        /// <param name="job"></param>
        /// <param name="monitorData"></param>
        public void RunJobFileProcessing(string job, StatusMonitorData monitorData)
        {
            string repositoryDirectory = StaticClass.IniData.RepositoryDir;
            string finishedDirectory = StaticClass.IniData.FinishedDir;
            string errorDirectory = StaticClass.IniData.ErrorDir;
            string processingBufferJobDir = StaticClass.IniData.ProcessingDir + @"\" + job;
            string dataXmlFileName = StaticClass.IniData.ProcessingDir + @"\" + job + @"\" + "data.xml";

            StaticClass.StatusDataEntry(job, JobStatus.COPYING_TO_ARCHIVE, JobType.TIME_START);

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
                // If the Finished directory does not exist, create it
                string finishedJobDirectoryName = finishedDirectory + @"\" + monitorData.JobSerialNumber;
                if (!Directory.Exists(finishedJobDirectoryName))
                {
                    Directory.CreateDirectory(finishedJobDirectoryName);
                }

                // Copy the Transfered files to the Finished directory 
                foreach (string file in monitorData.TransferedFileList)
                {
                    FileHandling.CopyFile(processingBufferJobDir + @"\" + file,
                                          finishedJobDirectoryName + @"\" + file);
                }

                // Move Processing Buffer Files to the Repository directory when passed
                FileHandling.CopyFolderContents(processingBufferJobDir, repositoryJobDirectoryName, true, true);
            }
            else // Send files to the Error Buffer and repository
            {
                // If the Error directory does not exist, create it
                string errorJobDirectoryName = errorDirectory + @"\" + monitorData.JobSerialNumber;
                if (!Directory.Exists(errorJobDirectoryName))
                {
                    Directory.CreateDirectory(errorJobDirectoryName);
                }

                // Copy the Transfered files to the Error directory 
                foreach (string file in monitorData.TransferedFileList)
                {
                    if (File.Exists(processingBufferJobDir + @"\" + file))
                    {
                        FileHandling.CopyFile(processingBufferJobDir + @"\" + file, 
                                              errorJobDirectoryName + @"\" + file);
                    }
                }

                // Move Processing Buffer Files to the Repository directory when failed
                FileHandling.CopyFolderContents(processingBufferJobDir, repositoryJobDirectoryName, true, true);
            }
        }

        /// <summary>
        /// Run a job from Input or Processing Buffers
        /// </summary>
        /// <param name="dirScanType"></param>
        /// <param name="jobXmlData"></param>
        public void RunJob(JobXmlData jobXmlData, DirectoryScanType dirScanType)
        {
            // Increment number of Jobs executing in only one place!
            StaticClass.NumberOfJobsExecuting++;

            // Create the Job Run common strings
            string job = jobXmlData.Job;
            string xmlJobDirectory = jobXmlData.JobDirectory;
            string processingBufferDirectory = StaticClass.IniData.ProcessingDir;
            string processingBufferJobDir = processingBufferDirectory + @"\" + job;

            // Set the job start time
            StaticClass.JobStartTime[job] = DateTime.Now;

            // Create new status monitor data and fill it in with the job xml data
            StatusMonitorData monitorData = new StatusMonitorData
            {
                Job = job,
                JobDirectory = xmlJobDirectory,
                StartTime = DateTime.Now,
                JobSerialNumber = jobXmlData.JobSerialNumber,
                TimeStamp = jobXmlData.TimeStamp,
                XmlFileName = jobXmlData.XmlFileName
            };

            // Add initial entry to status list
            StaticClass.StatusDataEntry(job, JobStatus.JOB_STARTED, JobType.TIME_RECEIVED);

            // Get the Job xml data
            monitorData = GetJobXmlData(jobXmlData, monitorData);

            // If this job comes from the Input directory, run the Input job check and start job
            if (dirScanType == DirectoryScanType.INPUT_BUFFER)
            {
                RunInputBufferScan(processingBufferJobDir, job, monitorData);
            }

            // If the shutdown flag is set, exit method
            if (StaticClass.ShutDownPauseCheck("Run Job") == true)
            {
                return;
            }

            // Add entry to status list
            StaticClass.StatusDataEntry(job, JobStatus.EXECUTING, JobType.TIME_START);

            StaticClass.Log(string.Format("Starting Job {0} with Modeler {1} on Port {2} with {3} CPU's at {4:HH:mm:ss.fff}",
                job, monitorData.Modeler, monitorData.JobPortNumber, StaticClass.IniData.CPUCores, DateTime.Now));

            // Execute Modeler using the command line generator
            string executable = StaticClass.IniData.ModelerRootDir + @"\" + monitorData.Modeler + @"\" + monitorData.Modeler + ".exe";
            string processingBuffer = processingBufferJobDir;
            int port = monitorData.JobPortNumber;
            int cpuCores = StaticClass.IniData.CPUCores;
            CommandLineGenerator cmdLineGenerator = new CommandLineGenerator(
                executable, processingBuffer, port, cpuCores);
            if (cmdLineGenerator == null)
            {
                StaticClass.Logger.LogError("JobRunThread cmdLineGenerator failed to instantiate");
            }
            Process modelerProcess = cmdLineGenerator.ExecuteCommand(job);

            // Register with the Processing File Watcher class and start its thread
            ProcessingFileWatcherThread processingFileWatcher = new ProcessingFileWatcherThread(processingBufferJobDir, monitorData);
            if (processingFileWatcher == null)
            {
                StaticClass.Logger.LogError("JobRunThread ProcessingFileWatch failed to instantiate");
            }
            processingFileWatcher.ThreadProc();

            // Start the TCP/IP Communications thread before checking for Processing job files
            TcpIpListenThread tcpIpThread = new TcpIpListenThread(monitorData);
            if (tcpIpThread == null)
            {
                StaticClass.Logger.LogError("ProcessingFileWatcherThread tcpIpThread failed to instantiate");
            }
            tcpIpThread.ThreadProc();

            // Add entry to status list
            StaticClass.StatusDataEntry(job, JobStatus.MONITORING_PROCESSING, JobType.TIME_START);

            // Wait 45 seconds for Modeler to get started before reading it's information
            Thread.Sleep(StaticClass.DISPLAY_PROCESS_DATA_WAIT);

            // Display the Modeler Process information
            DisplayProcessInfo(job, modelerProcess);

            // Wait for the Processing job scan complete or shut down
            do
            {
                if (StaticClass.ShutDownPauseCheck("Run Job") == true)
                {
                    return;
                }

                Thread.Yield();
            }
            while ((StaticClass.ProcessingJobScanComplete[job] == false) && (StaticClass.JobShutdownFlag[job] == false));

            // Wait to make sure the data.xml is done being handled
            Thread.Sleep(StaticClass.POST_PROCESS_WAIT);

            if (StaticClass.JobShutdownFlag[job] == false)
            {
                // Wait for the data.xml file to contain a result
                string dataXmlFileName = processingBufferJobDir + @"\" + "data.xml";
                if (OverallResultEntryCheck(dataXmlFileName) == true)
                {
                    StaticClass.Log(string.Format("Overall Results check confirmed for Job {0} at {1:HH:mm:ss.fff}",
                        job, DateTime.Now));
                }
                else
                {
                    StaticClass.Log(string.Format("Overall Results check failed for Job {0} at {1:HH:mm:ss.fff}",
                        job, DateTime.Now));
                }
            }

            // Make sure Modeler Process is stopped
            if (StaticClass.ProcessHandles[job].HasExited == false)
            {
                StaticClass.Log(string.Format("Shutting down Modeler Executable for Job {0} at {1:HH:mm:ss.fff}",
                    job, DateTime.Now));

                StaticClass.ProcessHandles[job].Kill();
                StaticClass.ProcessHandles.Remove(job);

                // Wait for Process to end
                Thread.Sleep(StaticClass.SHUTDOWN_PROCESS_WAIT);
            }

            // Run the Job Complete handler
            RunJobFileProcessing(job, monitorData);

            // Add entry to status list
            StaticClass.StatusDataEntry(job, JobStatus.COMPLETE, JobType.TIME_COMPLETE);

            // Show Job Complete message
            TimeSpan timeSpan = DateTime.Now - StaticClass.JobStartTime[job];
            StaticClass.Log(string.Format("Job {0} Complete taking {1:hh\\:mm\\:ss}. Decrementing Job count to {2} at {3:HH:mm:ss.fff}",
                job, timeSpan, StaticClass.NumberOfJobsExecuting - 1, DateTime.Now));

            // Decrement the number of Jobs executing in one place!
            StaticClass.NumberOfJobsExecuting--;
        }
    }
}
