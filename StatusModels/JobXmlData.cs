namespace Status.Models
{
    /// <summary>
    /// Job Xml Data
    /// </summary>
    public class JobXmlData
    {
        public string Job { get; set; }
        public string JobDirectory { get; set; }
        public string JobSerialNumber { get; set; }
        public string TimeStamp { get; set; }
        public string XmlFileName { get; set; }
        public int NumberOfFilesFound { get; set; }
        public int NumberOfFilesNeeded { get; set; }
        public bool AreJobFilesReady { get; set; }
    }
}
