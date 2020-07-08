using System;
using System.Collections.Generic;
using System.Text;

namespace StatusModels
{
    public class StatusMonitorData
    {
        public string Job { get; set; }
        public string JobName { get; set; }
        public string TimeStamp { get; set; }
        public string JobDirectory { get; set; }
        public string IniFileName { get; set; }
        public string XmlFileName { get; set; }
        public string UploadDir { get; set; }
        public string ProcessingDir { get; set; }
        public string RepositoryDir { get; set; }
        public string FinishedDir { get; set; }
        public string ErrorDir { get; set; }
        public string ModelersDir { get; set; }
        public int CPUCores { get; set; }
        public string Modeler { get; set; }
        public int MaxTimeLimit { get; set; }
        public int StartPort { get; set; }
        public string LogFile { get; set; }
        public string UnitNumber { get; set; }
        public int NumFilesConsumed { get; set; }
        public int NumFilesProduced { get; set; }
        public int NumFilesToTransfer { get; set; }
        public string Transfered1{ get; set; }
        public string Transfered2 { get; set; }
        public string Transfered3 { get; set; }
        public string Transfered4 { get; set; }
        public string Transfered5 { get; set; }
    }
}
