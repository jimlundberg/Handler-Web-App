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
    }
}
