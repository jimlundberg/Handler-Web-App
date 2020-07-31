using System;

namespace StatusModels
{
    /// <summary>
    /// Ini File Data
    /// </summary>
    public class IniFileData
    {
        public String IniFileName { get; set; }
        public String InputDir { get; set; }
        public String ProcessingDir { get; set; }
        public String RepositoryDir { get; set; }
        public String FinishedDir { get; set; }
        public String ErrorDir { get; set; }
        public String ModelerRootDir { get; set; }
        public String LogFile { get; set; }
        public int CPUCores { get; set; }
        public int ExecutionLimit { get; set; }
        public int MaxTimeLimit { get; set; }
        public int ScanTime { get; set; }
        public int StartPort { get; set; }
        public int LogFileHistory { get; set; }
    }
}
