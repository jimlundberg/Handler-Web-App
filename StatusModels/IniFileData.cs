using System;

namespace StatusModels
{
    /// <summary>
    /// Ini File Data
    /// </summary>
    public class IniFileData
    {
        public string IniFileName { get; set; }
        public string InputDir { get; set; }
        public string ProcessingDir { get; set; }
        public string RepositoryDir { get; set; }
        public string FinishedDir { get; set; }
        public string ErrorDir { get; set; }
        public string ModelerRootDir { get; set; }
        public string StatusLogFile { get; set; }
        public string ProcessLogFile { get; set; }
        public int CPUCores { get; set; }
        public int ExecutionLimit { get; set; }
        public int MaxTimeLimit { get; set; }
        public int ScanTime { get; set; }
        public int StartPort { get; set; }
        public int LogFileHistory { get; set; }
        public int LogFileMaxSize { get; set; }
    }
}
