using System;
using System.Collections.Generic;
using System.Text;

namespace StatusModels
{
    public class StatusMonitorData
    {
        public string Job { get; set; }
        public string JobPrefix { get; set; }
        public string JobDirectory { get; set; }
        public string IniFileName { get; set; }
        public string XmlFileName { get; set; }
        public string UploadDir { get; set; }
        public string ProcessingDir { get; set; }
        public string RepositoryDir { get; set; }
        public string FinishedDir { get; set; }
        public string ErrorDir { get; set; }
        public string ModelerRootDir { get; set; }
        public int CPUCores { get; set; }
        public string Modeler { get; set; }
        public int MaxTimeLimit { get; set; }
        public int StartPort { get; set; }
        public string LogFile { get; set; }
        public string UnitNumber { get; set; }
        public int NumFilesConsumed { get; set; }
        public int NumFilesProduced { get; set; }
        public int NumFilesToTransfer { get; set; }
    }
}
