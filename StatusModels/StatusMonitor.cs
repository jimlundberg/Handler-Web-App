using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace StatusModels
{
    public class StatusMonitorData
    {
        public string Job { get; set; }
        public int JobIndex { get; set; }
        public String JobSerialNumber { get; set; }
        public String TimeStamp { get; set; }
        public String JobDirectory { get; set; }
        public String IniFileName { get; set; }
        public String InputDir { get; set; }
        public String UploadDir { get; set; }
        public String ProcessingDir { get; set; }
        public String RepositoryDir { get; set; }
        public String FinishedDir { get; set; }
        public String ErrorDir { get; set; }
        public String ModelerRootDir { get; set; }
        public String XmlFileName { get; set; }
        public String UnitNumber { get; set; }
        public String Modeler { get; set; }
        public int CPUCores { get; set; }
        public int ExecutionLimit { get; set; }
        public int ExecutionCount{ get; set; }
        public int MaxTimeLimit { get; set; }
        public int StartPort { get; set; }
        public String LogFile { get; set; }
        public int NumFilesConsumed { get; set; }
        public int NumFilesProduced { get; set; }
        public int NumFilesToTransfer { get; set; }
        public List<String> transferedFileList { get; set; }
    }
}
