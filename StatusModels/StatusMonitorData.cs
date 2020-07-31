using System;
using System.Collections.Generic;

namespace StatusModels
{
    /// <summary>
    /// Status Monitor Data
    /// </summary>
    public class StatusMonitorData
    {
        public string Job { get; set; }
        public int JobIndex { get; set; }
        public String JobSerialNumber { get; set; }
        public String TimeStamp { get; set; }
        public String JobDirectory { get; set; }
        public String XmlFileName { get; set; }
        public String UnitNumber { get; set; }
        public String Modeler { get; set; }
        public int JobPortNumber { get; set; }
        public int ExecutionCount { get; set; }
        public int NumFilesConsumed { get; set; }
        public int NumFilesProduced { get; set; }
        public int NumFilesToTransfer { get; set; }
        public List<String> transferedFileList { get; set; }
    }
}
