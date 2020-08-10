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
        public string JobSerialNumber { get; set; }
        public string TimeStamp { get; set; }
        public string JobDirectory { get; set; }
        public string XmlFileName { get; set; }
        public string UnitNumber { get; set; }
        public string Modeler { get; set; }
        public int JobPortNumber { get; set; }
        public int NumFilesConsumed { get; set; }
        public int NumFilesProduced { get; set; }
        public int NumFilesToTransfer { get; set; }
        public DateTime StartTime { get; set; }
        public List<String> transferedFileList { get; set; }
    }
}
